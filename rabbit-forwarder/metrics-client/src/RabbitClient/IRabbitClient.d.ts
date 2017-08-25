import * as Rx from "rxjs/Rx";

export interface IRabbitClient {
    startConsumer(): Rx.Observable<Map<string, number>>;
}
export default IRabbitClient;