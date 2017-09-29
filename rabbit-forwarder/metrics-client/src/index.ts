require("console-stamp")(console, "isoDateTime");

import * as Rx from "rxjs/Rx";
import * as process from "process";

import * as NodeScheduler from "node-schedule";
import IPoolStatsRepository from "./PoolStatsRepository/IPoolStatsRepository";
import PoolStatsRepository from "./PoolStatsRepository/PoolStatsRepository";

import ICarbonClient from "./CarbonClient/ICarbonClient";
import CarbonClient from "./CarbonClient/CarbonClient";

import IRabbitClient from "./RabbitClient/IRabbitClient";
import RabbitClient from "./RabbitClient/RabbitClient";

const RETRY_COUNT = process.env.RETRY_COUNT ? parseInt(<string>process.env.RETRY_COUNT) : 5;
const RETRY_DELAY_MS = process.env.RETRY_DELAY_MS ? parseInt(<string>process.env.RETRY_DELAY_MS) : 500;
const CARBON_HOST = <string>process.env.CARBON_HOST;
const CARBON_PORT = process.env.CARBON_PORT ? parseInt(<string>process.env.CARBON_PORT) : 2003;
const RABBIT_URL = <string>process.env.RABBIT_URL;
const RABBIT_EXCHANGE_NAME = <string>process.env.RABBIT_EXCHANGE_NAME;
const RABBIT_QUEUE_NAME = <string>process.env.RABBIT_QUEUE_NAME;
const POOL_API_HOST = <string>process.env.POOL_API_HOST;
const WALLET_ADDRESS = <string>process.env.WALLET_ADDRESS;
const POOL_POLLING_SCHEDULE = process.env.POOL_POLLING_SCHEDULE ? <string>process.env.POOL_POLLING_SCHEDULE : "*/5 * * * *";

const carbonClient: ICarbonClient = new CarbonClient(CARBON_HOST, CARBON_PORT);
const rabbitClient = new RabbitClient(RABBIT_URL, RABBIT_EXCHANGE_NAME, RABBIT_QUEUE_NAME);
const poolStatsRepo: IPoolStatsRepository = new PoolStatsRepository(POOL_API_HOST);

const retryPoll = (obs: Rx.Observable<string>) => {
    return obs.retryWhen(errors => {
        return errors.scan((errorCount, err) => {
            if(errorCount >= RETRY_COUNT) {
                throw err;
            }
            console.error("NetworkStats failure, retry " + (errorCount + 1) + "/" + RETRY_COUNT);
            return (errorCount + 1);
        }, 0).delay(RETRY_DELAY_MS);
    })
};

rabbitClient.startConsumer()
    .flatMap(messageMap => carbonClient.pushMetrics(messageMap))
    .retryWhen(errors => {
        console.error(`Failed to forward metrics, retrying in ${RETRY_DELAY_MS} ms`);
        return errors.delay(RETRY_DELAY_MS);
    })
    .repeatWhen(o => {
        console.info(`RabbitMQ connection gracefully closed, restarting in ${RETRY_DELAY_MS} ms`);
        return o.delay(RETRY_DELAY_MS)
    })
    .subscribe(
        (msg) => console.info("RabbitMQ message forwarded successfully", msg),
        (e) => console.error("Failed to forward metrics from RabbitMQ", e),
        () => console.warn("RabbitMQ forwarding ended")
    );

NodeScheduler.scheduleJob(POOL_POLLING_SCHEDULE, () => {
    const netWorkStatsObs = poolStatsRepo.getNetworkStats()
        .flatMap(dto => {
            const metricsMap = new Map<string, number>();
            metricsMap.set("pool.price.btc", dto.btc);
            metricsMap.set("pool.price.usd", dto.usd);
            metricsMap.set("pool.blocktime", dto.blockTime);
            metricsMap.set("pool.difficulty", dto.difficulty);
            metricsMap.set("pool.hashrate", dto.hashrate);
            return carbonClient.pushMetrics(metricsMap, dto.time);
        });

    retryPoll(netWorkStatsObs)
    .subscribe(
        (msg) => console.info("Carbon saved a package", msg),
        (e) => console.error("Failed to write NetworkStats", e),
        () => console.log("NetworkStats saved"),
    );

    const minerStatsObs = poolStatsRepo.getMinerStats(WALLET_ADDRESS)
        .flatMap(dto => {
            const metricsMap = new Map<string, number>();
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".activeWorkers", dto.activeWorkers);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".averageHashrate", dto.averageHashrate);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".currentHashrate", dto.currentHashrate);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".invalidShares", dto.invalidShares);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".lastSeen", dto.lastSeen);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".reportedHashrate", dto.reportedHashrate);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".staleShares", dto.staleShares);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".unconfirmed", dto.unconfirmed);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".unpaid", dto.unpaid);
            metricsMap.set("pool.miners." + WALLET_ADDRESS + ".validShares", dto.validShares);
            return carbonClient.pushMetrics(metricsMap, dto.time);
        })

    retryPoll(minerStatsObs)
    .subscribe(
        (msg) => console.info("Carbon saved a package", msg),
        (e) => console.error("Failed to write MinerStats", e),
        () => console.log("MinerStats saved"),
    );

    const workerStatsObs = poolStatsRepo.getWorkerStats(WALLET_ADDRESS)
        .flatMap(dto => {
            let time = undefined;
            const obsArr: Rx.Observable<string>[] = [];
            dto.forEach(worker =>{
                const metricsMap = new Map<string, number>();
                metricsMap.set("pool.miners." + WALLET_ADDRESS + ".workers." + worker.worker + ".averageHashrate", worker.averageHashrate);
                metricsMap.set("pool.miners." + WALLET_ADDRESS + ".workers." + worker.worker + ".currentHashrate", worker.currentHashrate);
                metricsMap.set("pool.miners." + WALLET_ADDRESS + ".workers." + worker.worker + ".invalidShares", worker.invalidShares);
                metricsMap.set("pool.miners." + WALLET_ADDRESS + ".workers." + worker.worker + ".lastSeen", worker.lastSeen);
                metricsMap.set("pool.miners." + WALLET_ADDRESS + ".workers." + worker.worker + ".reportedHashrate", worker.reportedHashrate);
                metricsMap.set("pool.miners." + WALLET_ADDRESS + ".workers." + worker.worker + ".staleShares", worker.staleShares);
                metricsMap.set("pool.miners." + WALLET_ADDRESS + ".workers." + worker.worker + ".validShares", worker.validShares);
                const obs = carbonClient.pushMetrics(metricsMap, worker.time);
                obsArr.push(obs);
            });
            return Rx.Observable.concat(...obsArr);
        })

    retryPoll(workerStatsObs)
    .subscribe(
        (msg) => console.info("Carbon saved a package", msg),
        (e) => console.error("Failed to write WorkerStats", e),
        () => console.log("WorkerStats saved"),
    );
});