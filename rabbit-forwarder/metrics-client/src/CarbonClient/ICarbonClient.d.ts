import * as Rx from "rxjs/Rx";

export interface ICarbonClient {
    pushMetric(topic: string, value: number, time?: number): Rx.Observable<string>;
    pushMetrics(metrics: Map<string, number>, time?: number): Rx.Observable<string>;
}
export default ICarbonClient;