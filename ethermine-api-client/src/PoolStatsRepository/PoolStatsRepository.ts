import IPoolStatsRepository from "./IPoolStatsRepository";
import IBasicPoolStatsDTO from "./model/IBasicPoolStatsDTO";
import IMinedBlockHistoryDTO from "./model/IMinedBlockHistoryDTO";
import INetworkStatsDTO from "./model/INetworkStatsDTO";
import IMinerStatsDTO from "./model/IMinerStatsDTO";
import IWorkerStatsDTO from "./model/IWorkerStatsDTO";
import * as Rx from "rxjs/Rx";
import * as http from "https";
import { URL } from "url";

export default class PoolStatsRepository implements IPoolStatsRepository {

    constructor(private host: string) {}

    public getBasicPoolStats(): Rx.Observable<IBasicPoolStatsDTO[]> {
        const path = "/poolStats";
        const request = this.getRequest(path);
        return request;
    }

    public getBlockHistory(): Rx.Observable<IMinedBlockHistoryDTO[]> {
        const path = "/blocks/history";
        const request = this.getRequest(path);
        return request;
    }

    public getNetworkStats(): Rx.Observable<INetworkStatsDTO> {
        const path = "/networkStats";
        const request = this.getRequest(path);
        return request;
    }

    public getMinerStats(miner: string): Rx.Observable<IMinerStatsDTO> {
        const path = "/miner/:miner/currentStats".replace(":miner", miner);
        const request = this.getRequest(path);
        return request;
    }

    public getWorkerStats(miner: string): Rx.Observable<IWorkerStatsDTO[]> {
        const path = "/miner/:miner/workers".replace(":miner", miner);
        const request = this.getRequest(path);
        return request;
    }

    private getRequest(path: string) {
        const request: Rx.Observable<string> = Rx.Observable.create((o: Rx.Observer<string>) => {
            try {
                const url = new URL(path, this.host);
                http.get(url, (response) => {
                    response.on('data', (data: string) => {
                        o.next(data);
                    });
                    response.on('end', function() {
                        o.complete();
                    });
                    if(response.statusCode !== 200) {
                        o.error(response.statusCode);
                    }
                });
            }
            catch (err) {
                o.error(err);
            }
        });

        const requestWithFullBody = request.reduce((acc, val) => acc += val);
        const parsedJSON = requestWithFullBody.map(x => {
            const obj = JSON.parse(x);
            if (obj.status !== "OK") {
                throw new Error(obj.status);
            }
            return obj.data;
        });

        return parsedJSON;
    }
}
