import * as Rx from "rxjs/Rx";

export interface ICarbonClient {
    pushMetric(topic: string, value: number): Rx.Observable<void>;
    pushMetrics(metrics: Map<string, number>): Rx.Observable<void>;
}
export default ICarbonClient;