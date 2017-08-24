import * as NodeScheduler from "node-schedule";
import IPoolStatsRepository from "./PoolStatsRepository/IPoolStatsRepository";
import PoolStatsRepository from "./PoolStatsRepository/PoolStatsRepository";

import ICarbonClient from "./CarbonClient/ICarbonClient";
import CarbonClient from "./CarbonClient/CarbonClient";

import IRabbitClient from "./RabbitClient/IRabbitClient";
import RabbitClient from "./RabbitClient/RabbitClient";

const carbon: ICarbonClient = new CarbonClient("127.0.0.1", 2003);
const metrics = new Map<string, number>();
const rabbit = new RabbitClient("amqp://minerrig1:Qgjk5234@localhost:5672", "exchange", "queue");

rabbit.startConsumer().subscribe(
    (msg) => {
        let vals = "";
        msg.forEach((value, key) => {
            vals += key + ":" + value + ", ";
        });
        console.log("Next Message:", vals);
    },
    (e) => console.log("rabbit error:", e),
    () => console.log("rabbit complete")
);

metrics.set("test1.qq", 555);
metrics.set("test2.qq", 666);
metrics.set("test3.qq", 777);
metrics.set("test4.qq", 888);

carbon.pushMetrics(metrics).subscribe(
    () => console.log("tetsmetric next"),
    (e) => console.log("testmetric errir:", e),
    () => console.log("testmetric success")
)

const j = NodeScheduler.scheduleJob('*/1 * * * *', () => {
    const repo: IPoolStatsRepository = new PoolStatsRepository("https://api.ethermine.org");

    repo.getBasicPoolStats().subscribe(
        (v) => console.log("Response:", v),
        (e) => console.error("Error occured:", e),
        () => console.log("Completed"),
    );
});