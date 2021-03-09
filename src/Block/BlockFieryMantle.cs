using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System;

namespace CaelumTerras
{
    public class BlockFieryMantle : Block
    {
        private class FireLocation
        {
            public readonly BlockPos firePos;
            public readonly BlockFacing facing;

            public FireLocation(BlockPos firePos, BlockFacing facing)
            {
                this.firePos = firePos;
                this.facing = facing;
            }
        }

        private readonly int temperature = 1200;


        private readonly int tempLossPerMeter = 100;

        private Block blockFire;

        public BlockFieryMantle() : base()
        {
            if (Attributes != null)
            {
                temperature = Attributes["temperature"].AsInt(1200);
                tempLossPerMeter = Attributes["tempLossPerMeter"].AsInt(100);
            }
        }

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            base.OnServerGameTick(world, pos, extra);

            if (blockFire == null)
            {
                blockFire = world.GetBlock(new AssetLocation("fire"));
            }
            FireLocation fireLocation = (FireLocation)extra;
            world.BlockAccessor.SetBlock(blockFire.BlockId, fireLocation.firePos);
            BlockEntityFire befire = world.BlockAccessor.GetBlockEntity(fireLocation.firePos) as BlockEntityFire;
            if (befire != null)
            {
                befire.Init(fireLocation.facing, null);
            }
        }


        private FireLocation SearchAreaForAirNextToCombustibleBlock(IWorldAccessor world, BlockPos lavaPos)
        {
            FireLocation combustibleBlockPos = SearchRadiusForAirNextToCombustibleBlock(world, lavaPos, 1, 2);
            if (combustibleBlockPos == null)
            {
                combustibleBlockPos = SearchRadiusForAirNextToCombustibleBlock(world, lavaPos, 2, 3);
                if (combustibleBlockPos == null)
                {
                    combustibleBlockPos = SearchRadiusForAirNextToCombustibleBlock(world, lavaPos, 3, 2);
                }
            }
            return combustibleBlockPos;
        }


        private FireLocation SearchRadiusForAirNextToCombustibleBlock(IWorldAccessor world, BlockPos lavaPos, int y, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    BlockPos airBlockPos = lavaPos.AddCopy(x, y, z);

                    BlockFacing facing = IsNextToCombustibleBlock(world, lavaPos, airBlockPos);
                    if (facing != null)
                    {
                        return new FireLocation(airBlockPos, facing);
                    }
                }
            }
            return null;
        }


        private BlockFacing IsNextToCombustibleBlock(IWorldAccessor world, BlockPos lavaPos, BlockPos airBlockPos)
        {
            Block airBlock = world.BlockAccessor.GetBlock(airBlockPos);
            if (airBlock.BlockId == 0)
            {
                foreach (BlockFacing facing in BlockFacing.ALLFACES)
                {
                    if (facing != BlockFacing.DOWN)
                    {
                        BlockPos combustibleBlockPos = airBlockPos.Copy().Add(facing);
                        Block combustibleBlock = world.BlockAccessor.GetBlock(combustibleBlockPos);
                        if (combustibleBlock.CombustibleProps != null && combustibleBlock.CombustibleProps.BurnTemperature <= GetTemperatureAtLocation(lavaPos, airBlockPos))
                        {
                            return facing;
                        }
                    }
                }
            }
            return null;
        }


        private int GetTemperatureAtLocation(BlockPos lavaPos, BlockPos airBlockPos)
        {
            int distance = lavaPos.ManhattenDistance(airBlockPos);
            return temperature - (distance * tempLossPerMeter);
        }

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            
                FireLocation fireLocation = SearchAreaForAirNextToCombustibleBlock(world, pos);
                if (fireLocation != null)
                {
                    extra = fireLocation;
                    return true;
                }
            

            extra = null;
            return false;
        }


        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            if (world.Side == EnumAppSide.Server)
            {
                entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Fire, Source = EnumDamageSource.Block, SourceBlock = this, SourcePos = pos.ToVec3d() }, 3f);
            }
        }


        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            if (world.Side == EnumAppSide.Server)
            {
                entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Fire, Source = EnumDamageSource.Block, SourceBlock = this, SourcePos = pos.ToVec3d() }, 3f);
                entity.Ignite();
                if (!entity.Alive) entity.Die(EnumDespawnReason.Combusted);
            }
        }

        public override bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc)
        {
            return false;
        }
    }

}
