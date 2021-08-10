using SlugBrain.GameClasses;

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
        }
        public static void Disable()
        {
            On.AbstractCreature.ctor -= AbstractCreature_ctor;
            On.AbstractCreature.Realize -= AbstractCreature_Realize;
            On.AbstractCreature.InitiateAI -= AbstractCreature_InitiateAI;
            On.Player.checkInput -= Player_checkInput;
        }

        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self,
            World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);

            if (creatureTemplate.type == CreatureTemplate.Type.Slugcat)
            {
                self.abstractAI = new AbstractCreatureAI(world, self);
            }
        }

        private static void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            if (self.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat)
            {
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

            if (self.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat &&
                self.realizedCreature is SuperSlugcat)
            {
                self.abstractAI.RealAI = new SlugcatAI(self, self.world);
            }
        }

        private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);

            BrainPlugin.InputSpoofer.ModifyInputs(ref self.input[0]);
        }

    }
}
