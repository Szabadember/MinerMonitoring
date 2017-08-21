import IMinedBlockDTO from "./IMinedBlockDTO"
import ITopMinerEntryDTO from "./ITopMinerEntryDTO"
import IPoolStatsDTO from "./IPoolStatsDTO";
import IPriceDTO from "./IPriceDTO";

export interface IBasicPoolStatsDTO {
    minedBlocks: IMinedBlockDTO[];
    topMiners: ITopMinerEntryDTO[];
    poolStats: IPoolStatsDTO;
    price: IPriceDTO;
}
export default IBasicPoolStatsDTO;
