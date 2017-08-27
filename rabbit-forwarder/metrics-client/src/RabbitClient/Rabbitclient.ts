import IRabbitClient from "./IRabbitClient";
import * as Rx from "rxjs/Rx";
import * as amqp from "amqplib";

export default class RabbitClient implements IRabbitClient {
    constructor(
        private url: string,
        private exchangeName: string,
        private queueName: string,
    ) {}

    public startConsumer(): Rx.Observable<Map<string, number>> {
        const obs: Rx.Observable<amqp.Message> = Rx.Observable.create((observer: Rx.Observer<amqp.Message>) => {
            amqp.connect(this.url)
            .then(conn => {
                conn.on("error", err => observer.error(err));
                return conn.createChannel();
            })
            .then(ch => {
                ch.on("error", err => observer.error(err));
                return ch.assertExchange(this.exchangeName, "topic", {durable: false})
                .then(arg => {
                    const assertQueue = ch.assertQueue(this.queueName, {exclusive: true});
                    return assertQueue;
                })
                .then(arg => {
                    const bindQueue = ch.bindQueue(this.queueName, this.exchangeName, "*.#");
                    return bindQueue;
                })
                .then(() => {
                    let tag: string;
                    let close$ = Rx.Observable.fromEvent(ch, 'close');
                    let closeSub = close$.subscribe(() => observer.complete());
                    try {
                        ch.consume(this.queueName, (msg: amqp.Message) => {
                            observer.next(msg);
                        }, { noAck: true }).then(r => tag = r.consumerTag);
                    } catch (error) {
                        observer.error(error);
                    }
        
                    return () => {
                        closeSub.unsubscribe();
                        ch.cancel(tag);
                    }
                })
                .catch(err => observer.error(err));
            })
            .catch(err => observer.error(err));
        });

        return obs.map(msg => {
            const value = parseFloat(msg.content.toString());
            return new Map<string, number>([[msg.fields.routingKey, value]])
        });
    }
}
