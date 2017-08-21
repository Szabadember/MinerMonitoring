import IBasicPoolStatsDTO from "./model/IBasicPoolStatsDTO";
import IMinedBlockHistoryDTO from "./model/IMinedBlockHistoryDTO";
import INetworkStatsDTO from "./model/INetworkStatsDTO";
import IMinerStatsDTO from "./model/IMinerStatsDTO";
import IWorkerStatsDTO from "./model/IWorkerStatsDTO";
import * as Rx from "rxjs/Rx";

export interface IPoolStatsRepository {
    getBasicPoolStats(): Rx.Observable<IBasicPoolStatsDTO[]>;
    getBlockHistory(): Rx.Observable<IMinedBlockHistoryDTO[]>;
    getNetworkStats(): Rx.Observable<INetworkStatsDTO>;
    getMinerStats(miner: string): Rx.Observable<IMinerStatsDTO>;
    getWorkerStats(miner: string): Rx.Observable<IWorkerStatsDTO[]>;
}
export default IPoolStatsRepository;