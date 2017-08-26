import * as Rx from "rxjs/Rx";
import * as process from "process";

import * as NodeScheduler from "node-schedule";
import IPoolStatsRepository from "./PoolStatsRepository/IPoolStatsRepository";
import PoolStatsRepository from "./PoolStatsRepository/PoolStatsRepository";

import ICarbonClient from "./CarbonClient/ICarbonClient";
import CarbonClient from "./CarbonClient/CarbonClient";

import IRabbitClient from "./RabbitClient/IRabbitClient";
import RabbitClient from "./RabbitClient/RabbitClient";

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

rabbitClient.startConsumer().flatMap(messageMap => carbonClient.pushMetrics(messageMap))
    .subscribe(
        () => {},
        (e) => console.error("Failed to forward metrics", e),
        () => console.warn("Forwarding ended")
    );

const j = NodeScheduler.scheduleJob(POOL_POLLING_SCHEDULE, () => {
    poolStatsRepo.getNetworkStats()
    .flatMap(dto => {
        const metricsMap = new Map<string, number>();
        metricsMap.set("pool.price.btc", dto.btc);
        metricsMap.set("pool.price.usd", dto.usd);
        metricsMap.set("pool.blocktime", dto.blockTime);
        metricsMap.set("pool.difficulty", dto.difficulty);
        metricsMap.set("pool.hashrate", dto.hashrate);
        return carbonClient.pushMetrics(metricsMap, dto.time);
    })
    .subscribe(
        () => {},
        (e) => console.error("Failed to write NetworkStats", e),
        () => console.log("Completed"),
    );

    poolStatsRepo.getMinerStats(WALLET_ADDRESS)
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
    .subscribe(
        () => {},
        (e) => console.error("Failed to write MinerStats", e),
        () => console.log("Completed"),
    );

    poolStatsRepo.getWorkerStats(WALLET_ADDRESS)
    .flatMap(dto => {
        let time = undefined;
        const obsArr: Rx.Observable<any>[] = [];
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
        return Rx.Observable.concat(obsArr);
    })
    .subscribe(
        () => {},
        (e) => console.error("Failed to write MinerStats", e),
        () => console.log("Completed"),
    );
});