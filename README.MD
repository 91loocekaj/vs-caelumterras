-----Changes to World Generation Process-----
- Structures will only spawn at heights above sea level to stop floating structures(Harmony patches NoAirRuin & NoAirHut). Structure spawn rate increased to accommodate
- Because water block id changed to air patches where made to change id to water to allow for proper ponds and waterfalls (Harmony Patches FloatingPonds & FloatingRiver)
- A generic function is hooked in to player join event that when paired with a 0 spawn radius, ensures a safe spawn point for the player

-----Locust Nest Changes-----
This causes the nest to only settle towards the bottom along with more decorations

-----Carrion AI Attributes-----
This AI contains all of the standard attributes inherited from the AiTaskBase class as well as
eatSound = A string represented the path of the desired sound to be made when eating
eatAnimation = A string which should be a code of an animation to use for eating
movespeed = A float which determines how fast the creature can move to a corpse

-----Aerial Flee Entity AI Attributes-----
This AI is a modified version of the vanilla class AiTaskFleeEntity. It adds no new attributes and is tweaked to allow flying creatures better use of vertical movement for escape

-----Ranged Attack AI-----
  burstCooldownMs = Integer of number of milliseconds between shots
  burstSize = Integer number of rounds per attack. (Note having burstCooldownMs below 600 reduces number of shots for some reason)
  projectileCount = Integer number of projectiles shot per round
  damagePlayerAtMs = Integer; When to start firing
  minDist = Float; Minimum horizontal distance the target must be at
  minVerDist = Float; Minimum vertical distance the target must be at
  maxDist = Float; Maximum horizontal distance the target can be from
  maxVertDist = Float; Maximum vertical distance the target can be at
  spreadAngle = Float; How big the spread/accuracy is
  shotVelocity =  Float; How fast the shot is
  bodyPitch = A bool that determines if the entity should move its pitch when aiming
  fireThrough = A bool of whether the entity should fire through obstacles
  projectiles = An array of potential projectiles to shoot. A projectile should look like this:
    {
    projectileEntityCode: String of the entity code for the projectile

    damage: A float for how much damage to do
                weight: A float of the weight of the projectile(Only affects knockback)
      dropChance: A float that determines if this projectile item gets picked up
      stack: A Json Item Stack the projectile contains
    }
