using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using System;
using Vintagestory.API.Common.Entities;

namespace CaelumTerras
{
    public class AiTaskSeekCarrion : AiTaskBase
    {
        AssetLocation eatSound;

        
        IAnimalFoodSource targetPoi;

        float moveSpeed = 0.02f;
        long stuckatMs = 0;
        bool nowStuck = false;

        float eatTime = 1f;

        float eatTimeNow = 0;
        bool soundPlayed = false;
        bool doConsumePortion = true;
        bool eatAnimStarted = false;
        
        float quantityEaten;

        AnimationMetaData eatAnimMeta;

        Dictionary<IAnimalFoodSource, CFailedAttempt> failedSeekTargets = new Dictionary<IAnimalFoodSource, CFailedAttempt>();

        float extraTargetDist;
        long lastPOISearchTotalMs;

        public AiTaskSeekCarrion(EntityAgent entity) : base(entity)
        {
            entity.WatchedAttributes.SetBool("doesEat", true);
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);
            if (taskConfig["eatSound"] != null)
            {
                string eatsoundstring = taskConfig["eatSound"].AsString(null);
                if (eatsoundstring != null) eatSound = new AssetLocation(eatsoundstring).WithPathPrefix("sounds/");
            }

            if (taskConfig["movespeed"] != null)
            {
                moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
            }

            if (taskConfig["eatAnimation"].Exists)
            {
                eatAnimMeta = new AnimationMetaData()
                {
                    Code = taskConfig["eatAnimation"].AsString()?.ToLowerInvariant(),
                    Animation = taskConfig["eatAnimation"].AsString()?.ToLowerInvariant(),
                    AnimationSpeed = taskConfig["eatAnimationSpeed"].AsFloat(1f)
                }.Init();
            }
        }

        public override bool ShouldExecute()
        {
            ITreeAttribute hunger = entity.WatchedAttributes.GetOrAddTreeAttribute("hunger");

            if (hunger.GetFloat("saturation") >= (entity.Properties?.Attributes?["maxSaturation"].AsFloat(20f) ?? 20f)) return false;
            // Don't search more often than every 3 seconds
            if (lastPOISearchTotalMs + 3000 > entity.World.ElapsedMilliseconds) return false;
            if (cooldownUntilMs > entity.World.ElapsedMilliseconds) return false;
            if (cooldownUntilTotalHours > entity.World.Calendar.TotalHours) return false;
            if (whenInEmotionState != null && !entity.HasEmotionState(whenInEmotionState)) return false;
            if (whenNotInEmotionState != null && entity.HasEmotionState(whenNotInEmotionState)) return false;


            targetPoi = null;
            extraTargetDist = 0;
            lastPOISearchTotalMs = entity.World.ElapsedMilliseconds;

            entity.World.Api.ModLoader.GetModSystem<EntityPartitioning>().WalkEntities(entity.ServerPos.XYZ, 10, (e) =>
            {
                if (!e.Alive)
                {
                    targetPoi = new Carrion(e);
                    return false;
                }

                return true;
            });           

            return targetPoi != null;
        }



        public float MinDistanceToTarget()
        {
            return Math.Max(extraTargetDist + 0.6f, (entity.CollisionBox.X2 - entity.CollisionBox.X1) / 2 + 0.05f);
        }

        public override void StartExecute()
        {
            base.StartExecute();
            stuckatMs = -9999;
            nowStuck = false;
            soundPlayed = false;
            eatTimeNow = 0;
            pathTraverser.NavigateTo(targetPoi.Position, moveSpeed, MinDistanceToTarget() - 0.1f, OnGoalReached, OnStuck, false, 1000);
            eatAnimStarted = false;
        }

        public override bool ContinueExecute(float dt)
        {
            Vec3d pos = targetPoi.Position;

            pathTraverser.CurrentTarget.X = pos.X;
            pathTraverser.CurrentTarget.Y = pos.Y;
            pathTraverser.CurrentTarget.Z = pos.Z;

            Cuboidd targetBox = entity.CollisionBox.ToDouble().Translate(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
            double distance = targetBox.ShortestDistanceFrom(pos);

            float minDist = MinDistanceToTarget();

            if (distance <= minDist)
            {
                pathTraverser.Stop();

                if (targetPoi.IsSuitableFor(entity) != true) return false;

                if (eatAnimMeta != null && !eatAnimStarted)
                {
                    if (animMeta != null)
                    {
                        entity.AnimManager.StopAnimation(animMeta.Code);
                    }

                    entity.AnimManager.StartAnimation(eatAnimMeta);

                    eatAnimStarted = true;
                }

                eatTimeNow += dt;


                if (eatTimeNow > eatTime * 0.75f && !soundPlayed)
                {
                    soundPlayed = true;
                    if (eatSound != null) entity.World.PlaySoundAt(eatSound, entity, null, true, 16, 1);
                }


                if (eatTimeNow >= eatTime)
                {
                    ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
                    if (tree == null) entity.WatchedAttributes["hunger"] = tree = new TreeAttribute();

                    if (doConsumePortion)
                    {
                        float sat = targetPoi.ConsumeOnePortion() * entity.Stats.GetBlended("digestion");
                        quantityEaten += sat;
                        tree.SetFloat("saturation", sat + tree.GetFloat("saturation", 0));
                        entity.WatchedAttributes.SetDouble("lastMealEatenTotalHours", entity.World.Calendar.TotalHours);
                        entity.WatchedAttributes.MarkPathDirty("hunger");
                    }
                    else quantityEaten = 1;

                    failedSeekTargets.Remove(targetPoi);

                    return false;
                }
            }
            else
            {
                if (!pathTraverser.Active)
                {
                    float rndx = (float)entity.World.Rand.NextDouble() * 0.3f - 0.15f;
                    float rndz = (float)entity.World.Rand.NextDouble() * 0.3f - 0.15f;
                    if (!pathTraverser.NavigateTo(targetPoi.Position.AddCopy(rndx, 0, rndz), moveSpeed, MinDistanceToTarget() - 0.15f, OnGoalReached, OnStuck, false, 500))
                    {
                        return false;
                    }
                }
            }


            if (nowStuck && entity.World.ElapsedMilliseconds > stuckatMs + eatTime * 1000)
            {
                return false;
            }


            return true;
        }


        float GetSaturation()
        {
            ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (tree == null) entity.WatchedAttributes["hunger"] = tree = new TreeAttribute();

            return tree.GetFloat("saturation");
        }


        public override void FinishExecute(bool cancelled)
        {
            base.FinishExecute(cancelled);
            pathTraverser.Stop();

            if (eatAnimMeta != null)
            {
                entity.AnimManager.StopAnimation(eatAnimMeta.Code);
            }

            if (cancelled)
            {
                cooldownUntilTotalHours = 0;
            }

            if (quantityEaten < 1)
            {
                cooldownUntilTotalHours = 0;
            }
            else
            {
                quantityEaten = 0;
            }
        }



        private void OnStuck()
        {
            stuckatMs = entity.World.ElapsedMilliseconds;
            nowStuck = true;

            CFailedAttempt attempt = null;
            failedSeekTargets.TryGetValue(targetPoi, out attempt);
            if (attempt == null)
            {
                failedSeekTargets[targetPoi] = attempt = new CFailedAttempt();
            }

            attempt.Count++;
            attempt.LastTryMs = world.ElapsedMilliseconds;

        }

        private void OnGoalReached()
        {
            pathTraverser.Active = true;
            failedSeekTargets.Remove(targetPoi);
        }


    }

    public class CFailedAttempt
    {
        public long LastTryMs;
        public int Count;
    }

    public class Carrion : IAnimalFoodSource
    {
        public Entity corpse;
        Vec3d pos = new Vec3d();

        public Carrion(Entity corpse)
        {
            this.corpse = corpse;
        }

        public Vec3d Position
        {
            get
            {
                pos.Set(corpse.Pos.X, corpse.Pos.Y, corpse.Pos.Z);
                return pos;
            }
        }

        public string Type => "food";

        public float ConsumeOnePortion()
        {
            ITreeAttribute meats;

            if ((meats = corpse.WatchedAttributes.GetTreeAttribute("harvestableInv")) != null)
            {
                DummyInventory harvest = new DummyInventory(corpse.Api);
                harvest.FromTreeAttributes(meats);
                harvest.DropAll(Position);
            }
            corpse.GetBehavior<EntityBehaviorDeadDecay>()?.DecayNow();
            return 1f;
        }

        public bool IsSuitableFor(Entity entity)
        {
            if (corpse != null && !corpse.Alive) return true;

            return false;
        }
    }
}
