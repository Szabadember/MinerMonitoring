namespace CoreClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Text;
    using System.Threading.Tasks;

    using CoreClientExtensions;
    using Entities;
    using Newtonsoft.Json;

    public class ClaymoreClient
    {
        private const string CLAYMORE_STATS_MESSAGE = "{\"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat1\"}\n";
        private int retryCount;
        private string host;
        private int port;
        
        public ClaymoreClient(string host, int port, int? retryCount)
        {
            this.host = host;
            this.port = port;
            this.retryCount = retryCount.HasValue ? retryCount.Value : 3;
        }

        public IObservable<MiningStats> requestStats()
        {
            return Observable.Create<string>(async (observer) => {
                try
                {
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync(this.host, this.port);
                        using (var stream = client.GetStream())
                        using (var reader = new BinaryReader(stream))
                        {
                            var request = System.Text.Encoding.ASCII.GetBytes(CLAYMORE_STATS_MESSAGE);
                            await stream.WriteAsync(request, 0, request.Length);
                            
                            while (client.Connected) {
                                byte[] readBuffer = reader.ReadBytes(1024);
                                var line = Encoding.ASCII.GetString(readBuffer);

                                if (string.Empty.Equals(line))
                                {
                                    break;
                                }

                                observer.OnNext(line);
                            }

                            observer.OnCompleted();
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                return Disposable.Empty;
            })
            .Aggregate(string.Empty, (acc, currentValue) => acc += currentValue)
            .Select((str) => JsonConvert.DeserializeObject<ClaymoreMessageDTO>(str))
            .Select((dto) => this.MapDTO(dto))
            .RetryWithBackoffStrategy(this.retryCount);
        }

        private MiningStats MapDTO(ClaymoreMessageDTO dto) {
            var entity = new MiningStats();

            entity.MinerVersion = dto.Result[0];
            entity.UptimeMinutes = int.Parse(dto.Result[1]);

            var primaryCoinHashrateAndShares = dto.Result[2].Split(";");
            var primaryHashratesPerGPU = dto.Result[3].Split(";");

            var secondaryCoinHashrateAndShares = dto.Result[4].Split(";");
            var secondaryHashratesPerGPU = dto.Result[5].Split(";");

            var temperaturesAndFanSpeedsPerGPU = dto.Result[6].Split(";");
            var poolAddresses = dto.Result[7].Split(";");
            var invalidsAndSwitches = dto.Result[8].Split(";");

            var primaryCoin = new CoinStats();
            primaryCoin.PoolAddress = poolAddresses[0];
            primaryCoin.Hashrate = long.Parse(primaryCoinHashrateAndShares[0]);
            primaryCoin.Shares = int.Parse(primaryCoinHashrateAndShares[1]);
            primaryCoin.RejectedShares = int.Parse(primaryCoinHashrateAndShares[2]);
            primaryCoin.InvalidShares = int.Parse(invalidsAndSwitches[0]);
            primaryCoin.PoolSwitches = int.Parse(invalidsAndSwitches[1]);
            entity.PrimaryCoin = primaryCoin;
            entity.SecondaryCoin = null;

            if (poolAddresses.Length == 2)
            {
                var secondaryCoin = new CoinStats();
                secondaryCoin.PoolAddress = poolAddresses[1];
                secondaryCoin.Hashrate = long.Parse(secondaryCoinHashrateAndShares[0]);
                secondaryCoin.Shares = int.Parse(secondaryCoinHashrateAndShares[1]);
                secondaryCoin.RejectedShares = int.Parse(secondaryCoinHashrateAndShares[2]);
                secondaryCoin.InvalidShares = int.Parse(invalidsAndSwitches[2]);
                secondaryCoin.PoolSwitches = int.Parse(invalidsAndSwitches[3]);
                entity.SecondaryCoin = secondaryCoin;
            }

            var gpuList = new List<GPUStats>();
            entity.GPUStats = gpuList;

            for (var i = 0; i < primaryHashratesPerGPU.Length; i++)
            {
                var gpuStat = new GPUStats();
                var primaryHashrate = long.Parse(primaryHashratesPerGPU[i]);
                var rawSecondaryHashrate = secondaryHashratesPerGPU[i];
                var secondaryHashrate = rawSecondaryHashrate == "off" ? (long?)null : long.Parse(rawSecondaryHashrate);
                var temperature = int.Parse(temperaturesAndFanSpeedsPerGPU[i * 2]);
                var fanspeed = int.Parse(temperaturesAndFanSpeedsPerGPU[i * 2 + 1]);

                gpuStat.PrimaryHashrate = primaryHashrate;
                gpuStat.SecondaryHashrate = secondaryHashrate;
                gpuStat.Temperature = temperature;
                gpuStat.FanSpeed = fanspeed;

                gpuList.Add(gpuStat);
            }

            return entity;
        }
    }
}
