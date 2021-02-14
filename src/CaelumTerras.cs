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

                FuzzyEntityPos spawn = player.SpawnPosition;
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
                                player.Entity.ServerPos.SetFrom(player.SpawnPosition.XYZ);
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

}
