import ICarbonClient from "./ICarbonClient";
import * as net from "net";
import * as Rx from "rxjs/Rx";

export default class CarbonClient implements ICarbonClient {
    
    constructor(private host: string, private port: number) {}

    public pushMetric(topic: string, value: number, time?: number): Rx.Observable<void> {
        const metricsMap = new Map<string, number>();
        metricsMap.set(topic, value);
        return this.pushMetrics(metricsMap, time);
    }

    public pushMetrics(metrics: Map<string, number>, time?: number): Rx.Observable<void> {
        const nowSeconds = Math.round(Date.now() / 1000.0);
        const timeStamp = (typeof time === "undefined" || time === null) ? nowSeconds : time;
        const packetArr: string[] = [];
        metrics.forEach((value, topic) => {
            const packet = topic + " " + value + " " + timeStamp + "\r\n";
            packetArr.push(packet);
        });

        const request = Rx.Observable.from(packetArr).flatMap(x => this.sendPacket(x));
        return request; 
    }

    private sendPacket(packet: string): Rx.Observable<void> {
        const request: Rx.Observable<void> = Rx.Observable.create((o: Rx.Observer<void>) => {
            const client = new net.Socket();
            try {
                client.connect(this.port, this.host, () => {
                    client.write(packet, () => {
                        client.destroy();
                        o.complete();
                    });
                });
            } catch (err) {
                client.destroy();
                o.error(err);
            }
        });
        return request;
    }
}