export interface IMinerStatsDTO {
    time: number;
    lastSeen: number;
    reportedHashrate: number;
    currentHashrate: number;
    validShares: number;
    invalidShares: number;
    staleShares: number;
    averageHashrate: number;
    activeWorkers: number;
    unpaid: number;
    unconfirmed: number;
}
export default IMinerStatsDTO;
