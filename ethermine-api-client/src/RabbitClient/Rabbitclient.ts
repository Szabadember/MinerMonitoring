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
        const connObs = Rx.Observable.fromPromise(amqp.connect(this.url));
        const chObs = connObs.flatMap(conn => conn.createChannel());
        const consumeObs = chObs.flatMap(ch => {
            const queuePromise = ch.assertExchange(this.exchangeName, "topic", {durable: false})
            .then(arg => {
                const assertQueue = ch.assertQueue("", {exclusive: true});
                return assertQueue;
            })
            .then(arg => {
                const bindQueue = ch.bindQueue(this.queueName, this.exchangeName, "*.#");
                return bindQueue;
            });

            const queueObs = Rx.Observable.fromPromise(queuePromise);
            const consumeObs = this.consume(ch, this.queueName, { noAck: true });
            const resultObs = consumeObs.map(msg => new Map<string, number>([msg.fields.routing_key, msg.content.readDoubleLE(0)]));

            return queueObs.flatMap(x => resultObs);
        });

        return consumeObs;
    }

    private consume(ch: amqp.Channel, queue: string, options?: amqp.Options.Consume): Rx.Observable<amqp.Message> {
        return <Rx.Observable<amqp.Message>> Rx.Observable.create((observer: Rx.Observer<amqp.Message>) => {
          let tag: string;
          let close$ = Rx.Observable.fromEvent(ch, 'close');
          let closeSub = close$.subscribe(() => observer.complete());
    
          ch.consume(queue, (msg: amqp.Message) => {
            observer.next(msg);
          }, options).then(r => tag = r.consumerTag);
    
          return () => {
            closeSub.unsubscribe();
            ch.cancel(tag);
          }
        });
      }
}