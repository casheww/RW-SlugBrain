using SlugBrain.GameClasses;
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
            On.AImap.TileAccessibleToCreature_IntVector2_CreatureTemplate += AImap_TileAccessibleToCreature_IntVector2_CreatureTemplate;
        }
        public static void Disable()
        {
            On.AbstractCreature.ctor -= AbstractCreature_ctor;
            On.AbstractCreature.Realize -= AbstractCreature_Realize;
            On.AbstractCreature.InitiateAI -= AbstractCreature_InitiateAI;
            On.Player.checkInput -= Player_checkInput;
            On.Player.ObjectEaten -= Player_ObjectEaten;
            On.AImap.IsConnectionAllowedForCreature -= AImap_IsConnectionAllowedForCreature;
            On.AImap.TileAccessibleToCreature_IntVector2_CreatureTemplate -= AImap_TileAccessibleToCreature_IntVector2_CreatureTemplate;
        }

        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self,
            World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);

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

            self.input[0] = BrainPlugin.InputSpoofer.ModifyInputs(self.input[0]);
        }

        private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig(self, edible);

            if (self is SuperSlugcat super)
            {
                BrainPlugin.Log($"consumed {edible} - yummy yummy");
                super.AI.treatTracker.RemoveFood((edible as PhysicalObject).abstractPhysicalObject);
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

        private static bool AImap_TileAccessibleToCreature_IntVector2_CreatureTemplate(
            On.AImap.orig_TileAccessibleToCreature_IntVector2_CreatureTemplate orig, AImap self, IntVector2 pos, CreatureTemplate crit)
        {
            bool result = orig(self, pos, crit);

            if (crit.type == CreatureTemplate.Type.Slugcat && !isCheckingJump)
            {
                isCheckingJump = true;
                if (JumpModule.CheckIsJumpable(self, pos, crit)) result = true;
                isCheckingJump = false;
            }

            return result;
        }
        static bool isCheckingJump = false;     // prevent stack overflow


    }
}
