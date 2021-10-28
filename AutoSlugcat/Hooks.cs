using SlugBrain.GameClasses;
using System.Collections.Generic;
using RWCustom;

namespace SlugBrain
{
    static class Hooks
    {
        public static void Enable()
        {
            On.AbstractCreature.ctor += AbstractCreature_ctor;
            On.AbstractCreature.Realize += AbstractCreature_Realize;
            On.AbstractCreature.InitiateAI += AbstractCreature_InitiateAI;
            On.Player.checkInput += Player_checkInput;
            On.Player.ObjectEaten += Player_ObjectEaten;
            On.AImap.IsConnectionAllowedForCreature += AImap_IsConnectionAllowedForCreature;
        }
        public static void Disable()
        {
            On.AbstractCreature.ctor -= AbstractCreature_ctor;
            On.AbstractCreature.Realize -= AbstractCreature_Realize;
            On.AbstractCreature.InitiateAI -= AbstractCreature_InitiateAI;
            On.Player.checkInput -= Player_checkInput;
            On.Player.ObjectEaten -= Player_ObjectEaten;
            On.AImap.IsConnectionAllowedForCreature -= AImap_IsConnectionAllowedForCreature;
        }

        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self,
            World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID id)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, id);

            // give slugcat a new abstract AI, regardless of its template's AI flag
            if (creatureTemplate.type == CreatureTemplate.Type.Slugcat)
            {
                self.abstractAI = new AbstractCreatureAI(world, self);
            }
        }

        private static void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            if (self.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat)
            {
                // create a SuperSlugcat instead of a Player object
                self.realizedCreature = new SuperSlugcat(self, self.world);
                self.InitiateAI();

                foreach (AbstractPhysicalObject.AbstractObjectStick obj in self.stuckObjects)
                {
                    obj.A.Realize();
                    obj.B.Realize();
                }
            }
            else orig(self);
        }

        private static void AbstractCreature_InitiateAI(On.AbstractCreature.orig_InitiateAI orig, AbstractCreature self)
        {
            orig(self);

            // init the custom slugcat AI
            if (self.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat &&
                self.realizedCreature is SuperSlugcat)
            {
                self.abstractAI.RealAI = new SlugcatAI(self, self.world);
            }
        }

        private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);

            self.input[0] = BrainPlugin.InputSpoofer.ModifyInput(self.input[0]);
        }

        private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig(self, edible);

            if (self is SuperSlugcat slugcat && edible is PhysicalObject obj)
            {
                BrainPlugin.Log($"consumed {edible} - yummy yummy");
                slugcat.ai.treatTracker.RegisterFoodEaten(obj.abstractPhysicalObject);
            }
        }

        private static bool AImap_IsConnectionAllowedForCreature(On.AImap.orig_IsConnectionAllowedForCreature orig,
            AImap self, MovementConnection connection, CreatureTemplate crit)
        {
            bool result = orig(self, connection, crit);

            if (crit.type == CreatureTemplate.Type.Slugcat)
            {
                if (self.TileAccessibleToCreature(connection.DestTile, crit)) result = true;
            }

            return result;
        }
        
    }
}
