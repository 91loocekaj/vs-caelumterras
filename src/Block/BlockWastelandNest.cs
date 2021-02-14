using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CaelumTerras
{
    public class BlockWastelandNest:BlockLocustNest
    {
        public override void OnLoaded(ICoreAPI api)
        {
            DecoBlocksFloor = new Block[]
            {
                api.World.GetBlock(new AssetLocation("locustnest-metalspike-none")),
                api.World.GetBlock(new AssetLocation("locustnest-metalspike-tiny")),
                api.World.GetBlock(new AssetLocation("locustnest-metalspike-small")),
                api.World.GetBlock(new AssetLocation("locustnest-metalspike-medium")),
                api.World.GetBlock(new AssetLocation("locustnest-metalspike-large")),
                api.World.GetBlock(new AssetLocation("bonyremains-cowskull-up")),
                api.World.GetBlock(new AssetLocation("bonyremains-ribcage"))
            };
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            for (int i = 0; i < 11; i++)
            {
                if (i > 0) pos.Y--;
                if (pos.Y < 2) return false;

                Block floor = blockAccessor.GetBlock(pos.DownCopy());
                if (blockAccessor.GetBlock(pos).Id == 0 && floor.SideSolid[BlockFacing.DOWN.Index])
                {
                    blockAccessor.SetBlock(blockAccessor.GetBlock(new AssetLocation("game:locustnest-cage")).Id, pos);
                    if (blockAccessor.GetBlockEntity(pos) == null) blockAccessor.SpawnBlockEntity("LocustNest", pos);

                    BlockPos tmppos = new BlockPos();
                    int tries = 55 + worldGenRand.NextInt(55);
                    while (tries-- > 0)
                    {
                        int offX = worldGenRand.NextInt(15) - 7;
                        int offY = worldGenRand.NextInt(15) - 7;
                        int offZ = worldGenRand.NextInt(15) - 7;

                        if (worldGenRand.NextDouble() < 0.4)
                        {
                            tryPlaceDecoDown(tmppos.Set(pos.X + offX, pos.Y + offY, pos.Z + offZ), blockAccessor, worldGenRand);
                        }


                    }

                    return true;
                }
            }

            return false;
        }

        private void tryPlaceDecoDown(BlockPos blockPos, IBlockAccessor blockAccessor, LCGRandom worldGenRand)
        {
            if (blockAccessor.GetBlockId(blockPos) != 0) return;

            int tries = 7;
            while (tries-- > 0)
            {
                blockPos.Y--;
                Block block = blockAccessor.GetBlock(blockPos);
                if (block.IsLiquid()) return;
                if (block.SideSolid[BlockFacing.DOWN.Index] && !(block is BlockFieryMantle))
                {
                    blockPos.Y++;
                    blockAccessor.SetBlock(DecoBlocksFloor[worldGenRand.NextInt(DecoBlocksFloor.Length)].BlockId, blockPos);
                    return;
                }
            }
        }
    }

}
