export interface IWorkerStatsDTO {
    worker: string;
    time: number;
    lastSeen: number;
    reportedHashrate: number;
    currentHashrate: number;
    averageHashrate: number;
    validShares: number;
    invalidShares: number;
    staleShares: number;
}
export default IWorkerStatsDTO;