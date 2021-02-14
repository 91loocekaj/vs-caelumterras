using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace CaelumTerras
{
    public class EntityLocustDrone: EntityAgent
    {
        ILoadedSound hoverSound;
        Vec3f soundpos;
        bool killSound
        {
            get { return WatchedAttributes.GetBool("killSound"); }
            set { WatchedAttributes.SetBool("killSound", value); WatchedAttributes.MarkPathDirty("killSound"); }
        }

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);

            if (!Alive) this.Properties.Habitat = EnumHabitat.Land;

            AnimManager.StartAnimation("fly");

            AssetLocation loc = new AssetLocation("ailments:sounds/entity/droneidle.ogg");

            if (hoverSound == null && World.Side == EnumAppSide.Client && Alive)
            {
                hoverSound = ((IClientWorldAccessor)World).LoadSound(new SoundParams()
                {
                    Location = loc,
                    ShouldLoop = true,
                    Position = soundpos = Pos.XYZ.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 3f
                });

                hoverSound?.Start();
            }
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);

            if (soundpos != null)
            {
                soundpos.X = (float)Pos.X;
                soundpos.Y = (float)Pos.Y;
                soundpos.Z = (float)Pos.Z;
                hoverSound?.SetPosition(soundpos);
            }

            if (killSound && Api.Side == EnumAppSide.Client)
            {
                hoverSound?.Stop();
                hoverSound?.Dispose();
                killSound = false;
            }
        }

        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            hoverSound?.Stop();
            hoverSound?.Dispose();

            base.OnEntityDespawn(despawn);
        }

        public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
        {
            this.Properties.Habitat = EnumHabitat.Land;

            killSound = true;

            base.Die(reason, damageSourceForDeath);
        }

        public override void Revive()
        {
            this.Properties.Habitat = EnumHabitat.Air;
            AnimManager.StartAnimation("fly");

            if (hoverSound == null && World.Side == EnumAppSide.Client)
            {
                hoverSound = ((IClientWorldAccessor)World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("ailments:sounds/entity/droneidle.ogg"),
                    ShouldLoop = true,
                    Position = soundpos = Pos.XYZ.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.25f
                });

                hoverSound.Start();
            }

            base.Revive();
        }

    }
}
