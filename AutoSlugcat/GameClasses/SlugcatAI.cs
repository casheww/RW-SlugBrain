
namespace SlugBrain.GameClasses
{
    class SlugcatAI : ArtificialIntelligence
    {
        public SlugcatAI(AbstractCreature creature, World world) : base(creature, world)
        {
            slugcat = creature.realizedCreature as SuperSlugcat;
            slugcat.AI = this;

            AddModule(new StandardPather(this, world, creature));
            AddModule(new ThreatTracker(this, 10));
            AddModule(new PreyTracker(this, 10, 1f, 3f, 15f, 0.95f));
            AddModule(new RainTracker(this));
            AddModule(new StuckTracker(this, true, true));
            stuckTracker.AddSubModule(new StuckTracker.GetUnstuckPosCalculator(stuckTracker));

            shelterFinder = new ShelterFinder(this, creature);
            AddModule(shelterFinder);

            AddModule(new UtilityComparer(this));
            utilityComparer.AddComparedModule(threatTracker, null, 0f, 1.1f);
            utilityComparer.AddComparedModule(preyTracker, null, 0f, 1.1f);
            utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 0f, 1.1f);

            behavior = Behavior.FollowPath;

            BrainPlugin.Log("SlugcatAI ctor done");
        }

        public override void Update()
        {
            base.Update();

            AIModule urge = utilityComparer.HighestUtilityModule();
            float urgeStrength = utilityComparer.HighestUtility();

            Behavior lastBehavior = behavior;

            if (urge is ThreatTracker) behavior = Behavior.Flee;
            else if (urge is RainTracker) behavior = Behavior.EscapeRain;
            else if (urge is PreyTracker) behavior = Behavior.Hunt;
            else if (urge is StuckTracker) behavior = Behavior.GetUnstuck;

            if (lastBehavior != behavior) BrainPlugin.Log($"slugcat behavior changed to {behavior}");

            switch (behavior)
            {
                case Behavior.FollowPath:
                    break;

                case Behavior.Flee:
                    WorldCoordinate fleeTo = threatTracker.FleeTo(creature.pos, 5, 30, urgeStrength > 0.3f);
                    creature.abstractAI.SetDestination(fleeTo);
                    break;

                case Behavior.Hunt:
                    creature.abstractAI.SetDestination(preyTracker.MostAttractivePrey.BestGuessForPosition());
                    break;

                case Behavior.EscapeRain:
                    WorldCoordinate denPos = shelterFinder.GetShelterTarget();
                    creature.abstractAI.SetDestination(denPos);
                    break;

                case Behavior.GetUnstuck:
                    creature.abstractAI.SetDestination(stuckTracker.getUnstuckPosCalculator.unstuckGoalPosition);
                    break;
            }
        }


        public enum Behavior
        {
            FollowPath,
            Flee,
            Hunt,
            EscapeRain,
            GetUnstuck
        }


        SuperSlugcat slugcat;
        ShelterFinder shelterFinder;

        public Behavior behavior;

    }
}
