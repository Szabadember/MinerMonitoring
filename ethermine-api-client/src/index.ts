import * as NodeScheduler from "node-schedule"
import IPoolStatsRepository from "./PoolStatsRepository/IPoolStatsRepository"
import PoolStatsRepository from "./PoolStatsRepository/PoolStatsRepository"

const j = NodeScheduler.scheduleJob('*/1 * * * *', () => {
    const repo: IPoolStatsRepository = new PoolStatsRepository("https://api.ethermine.org");

    repo.getBasicPoolStats().subscribe(
        (v) => console.log("Response:", v),
        (e) => console.error("Error occured:", e),
        () => console.log("Completed"),
    );
});