using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using HarmonyLib;
using System.Reflection;
using System;

namespace CaelumTerras
{
    public class CaelumTerras : ModSystem
    {
        private Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterEntity("EntityLocustDrone", typeof(EntityLocustDrone));

            
            api.RegisterBlockClass("BlockFieryMantle", typeof(BlockFieryMantle));            
            api.RegisterBlockClass("BlockWastelandNest", typeof(BlockWastelandNest));
            
            

            AiTaskRegistry.Register<AiTaskAerialFleeEntity>("fleeentity");
            AiTaskRegistry.Register<AiTaskRangedAttack>("rangedattack");
            AiTaskRegistry.Register<AiTaskSeekCarrion>("seekcarrion");

            harmony = new Harmony("com.jakecool19.skylands.structures");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            api.Event.PlayerCreate += (player) => {

                FuzzyEntityPos spawn = player.GetSpawnPosition(false);
                BlockPos pos = spawn.XYZ.AsBlockPos;

                if (api.World.BlockAccessor.GetTerrainMapheightAt(pos) + 1 <= api.World.SeaLevel)
                {
                    for (int dx = -200; dx <= 200; dx++)
                    {
                        for (int dz = -200; dx <= 200; dx++)
                        {
                            if (api.World.BlockAccessor.GetTerrainMapheightAt(pos.Add(dx, 0, dz)) + 1 > api.World.SeaLevel)
                            {
                                player.SetSpawnPosition(new PlayerSpawnPos(pos.X, api.World.BlockAccessor.GetTerrainMapheightAt(pos) + 2, pos.Z));
                                player.Entity.ServerPos.SetFrom(player.GetSpawnPosition(false).XYZ);
                                player.SendPositionToClient();
                                return;
                            }
                        }
                    }
                }
            };
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }
    }

    [HarmonyPatch(typeof(WorldGenStructure), "TryGenerateRuinAtSurface")]
    public class NoAirRuins
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        static bool Prefix(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, ref bool __result)
        {
            if (blockAccessor.GetTerrainMapheightAt(pos) + 1 <= worldForCollectibleResolve.SeaLevel)
            {
                //System.Diagnostics.Debug.WriteLine("Failed to place structure");
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(WorldGenStructure), "TryGenerateAtSurface")]
    public class NoAirHuts
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        static bool Prefix(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, ref bool __result)
        {
            if (blockAccessor.GetTerrainMapheightAt(pos) + 1 <= worldForCollectibleResolve.SeaLevel)
            {
                //System.Diagnostics.Debug.WriteLine("Failed to place hut");
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GenPonds), "initWorldGen")]
    public class FloatingPonds
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPostfix]
        static void Postfix(GenPonds __instance, ICoreServerAPI ___api)
        {
            __instance.GlobalConfig.waterBlockId = ___api.World.BlockAccessor.GetBlock(new AssetLocation("game:water-still-7")).BlockId;
        }
    }

    [HarmonyPatch(typeof(GenRivulets), "initWorldGen")]
    public class FloatingRivers
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPostfix]
        static void Postfix(GenRivulets __instance, ICoreServerAPI ___api)
        {
            __instance.GlobalConfig.waterBlockId = ___api.World.BlockAccessor.GetBlock(new AssetLocation("game:water-still-7")).BlockId;
        }
    }

    /*[HarmonyPatch(typeof(WeatherSimulationSnowAccum))]
    [HarmonyPatch("UpdateSnowLayer")]
    public class SnowPeek
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        static bool Prefix(SnowAccumSnapshot sumsnapshot, bool ignoreOldAccum, IServerMapChunk mc, Vec2i chunkPos, IWorldChunk[] chunksCol, ref UpdateSnowLayerChunk __result,
            WeatherSystemBase ___ws, int ___chunksize, int ___regionsize, int[][] ___randomShuffles, ICoreServerAPI ___sapi)
        {
            __result = null;
            UpdateSnowLayerChunk updateChunk = new UpdateSnowLayerChunk();
            var layers = ___ws.GeneralConfig.SnowLayerBlocks;

            int chunkX = chunkPos.X;
            int chunkZ = chunkPos.Y;

            int regionX = (chunkX * ___chunksize) / ___regionsize;
            int regionZ = (chunkZ * ___chunksize) / ___regionsize;

            int regionBasePosX = regionX * ___regionsize;
            int regionBasePosZ = regionZ * ___regionsize;

            BlockPos pos = new BlockPos();
            BlockPos placePos = new BlockPos();
            float aboveSeaLevelHeight = ___sapi.World.BlockAccessor.MapSizeY - ___sapi.World.SeaLevel;

            int[] posIndices = ___randomShuffles[___sapi.World.Rand.Next(___randomShuffles.Length)];

            int prevChunkY = -99999;
            IWorldChunk chunk = null;

            for (int i = 0; i < posIndices.Length; i++)
            {
                int posIndex = posIndices[i];
                int posY = mc.RainHeightMap[posIndex];
                int chunkY = posY / ___chunksize;

                pos.Set(
                    chunkX * ___chunksize + posIndex % ___chunksize,
                    posY,
                    chunkZ * ___chunksize + posIndex / ___chunksize
                );

                if (prevChunkY != chunkY)
                {
                    //this is the problem
                    if (chunksCol != null && chunkY < chunksCol.Length)
                    {
                        chunk = chunksCol?[chunkY] ?? ___sapi.WorldManager.GetChunk(chunkX, chunkY, chunkZ);
                        prevChunkY = chunkY;
                    }
                    else if (chunksCol != null)
                    { System.Diagnostics.Debug.WriteLine(string.Format("Chunk Y: {0} and Chunk Columms length is {1}", chunkY, chunksCol.Length)); return false; }
                }
                if (chunk == null) return false;

                float relx = (pos.X - regionBasePosX) / (float)___regionsize;
                float rely = GameMath.Clamp((pos.Y - ___sapi.World.SeaLevel) / aboveSeaLevelHeight, 0, 1);
                float relz = (pos.Z - regionBasePosZ) / (float)___regionsize;


                // What needs to be done here?
                // 1. Get desired snow cover level

                // 2. Get current snow cover level
                //    - Get topmmost block. Is it snow?
                //      - Yes. Use it as reference pos and stuff
                //      - No. Must have no snow, increment pos.Y by 1

                // 3. Compare and place block accordingly
                // Idea: New method Block.UpdateSnowLayer() returns a new block instance if a block change is needed


                // What needs to be done here, take 2
                // We have 3 possible cases per-block
                // 1: We find upside solid block. That means it has no snow on top
                // 2: We find snow. That means below is a solid block. 
                // 3: We find some other block: That means we should try to find its snow-covered variant

                // We have the following input data
                // 1. Snow accumulation changes since the last update (usually an in-game hour or 2)
                // 2. A precise snow level value from the position (if not set, load from snowlayer block type) (set to zero if the snowlayer is removed)
                // 3. The current block at position, which is either
                //    - A snow layer: Override with internal level + accum changes
                //    - A solid block: Plase snow on top based on internal level + accum changes
                //    - A snow variantable block: Call the method with the new level


                Block block = chunk.GetLocalBlockAtBlockPos(___sapi.World, pos);

                float hereAccum = 0;

                Vec2i vec = new Vec2i(pos.X, pos.Z);
                if (!ignoreOldAccum && !mc.SnowAccum.TryGetValue(vec, out hereAccum))
                {
                    hereAccum = block.GetSnowLevel(pos);
                }

                float nowAccum = hereAccum + sumsnapshot.GetAvgSnowAccumByRegionCorner(relx, rely, relz);

                mc.SnowAccum[vec] = GameMath.Clamp(nowAccum, -1, ___ws.GeneralConfig.SnowLayerBlocks.Count + 0.5f);

                float hereShouldLevel = nowAccum - GameMath.MurmurHash3Mod(pos.X, 0, pos.Z, 100) / 300f;
                float shouldIndexf = GameMath.Clamp((hereShouldLevel - 1.1f), -1, ___ws.GeneralConfig.SnowLayerBlocks.Count - 1);
                int shouldIndex = shouldIndexf < 0 ? -1 : (int)shouldIndexf;

                placePos.Set(pos.X, Math.Min(pos.Y + 1, ___sapi.World.BlockAccessor.MapSizeY - 1), pos.Z);
                chunkY = placePos.Y / ___chunksize;

                if (prevChunkY != chunkY)
                {
                    chunk = chunksCol?[chunkY] ?? ___sapi.WorldManager.GetChunk(chunkX, chunkY, chunkZ);
                    prevChunkY = chunkY;
                }
                if (chunk == null) return false;

                Block upBlock = chunk.GetLocalBlockAtBlockPos(___sapi.World, placePos);



                // Case 1: We have a block that can become snow covered (or more snow covered)
                placePos.Set(pos);
                Block newblock = block.GetSnowCoveredVariant(placePos, hereShouldLevel);
                if (newblock != null)
                {
                    if (block.Id != newblock.Id && upBlock.Replaceable > 6000)
                    {
                        updateChunk.SetBlocks[placePos.Copy()] = new BlockIdAndSnowLevel(newblock, hereShouldLevel);
                    }
                }
                // Case 2: We have a solid block that can have snow on top
                else if ((block.SnowCoverage == null && block.SideSolid[BlockFacing.UP.Index]) || block.SnowCoverage == true)
                {
                    placePos.Set(pos.X, pos.Y + 1, pos.Z);

                    if (upBlock.Id != 0)
                    {
                        newblock = upBlock.GetSnowCoveredVariant(placePos, hereShouldLevel);
                        if (newblock != null && upBlock.Id != newblock.Id)
                        {
                            updateChunk.SetBlocks[placePos.Copy()] = new BlockIdAndSnowLevel(newblock, hereShouldLevel);
                        }

                        continue;
                    }

                    if (shouldIndex >= 0)
                    {
                        Block toPlaceBlock = layers.GetKeyAtIndex(shouldIndex);
                        updateChunk.SetBlocks[placePos.Copy()] = new BlockIdAndSnowLevel(toPlaceBlock, hereShouldLevel);
                    }
                }
            }

            __result = updateChunk;
            return false;
        }
    }*/

}
