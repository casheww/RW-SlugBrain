using RWCustom;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    class SlugcatAI : ArtificialIntelligence, IUseARelationshipTracker
    {
        public SlugcatAI(AbstractCreature creature, World world) : base(creature, world)
        {
            slugcat = creature.realizedCreature as SuperSlugcat;
            slugcat.AI = this;

            // vanilla AI modules
            AddModule(new StandardPather(this, world, creature));
            AddModule(new StuckTracker(this, false, true));
            stuckTracker.AddSubModule(new StuckTracker.GetUnstuckPosCalculator(stuckTracker));
            AddModule(new Tracker(this, 10, 20, -1, 0.35f, 5, 5, 10));
            AddModule(new RelationshipTracker(this, tracker));
            AddModule(new ThreatTracker(this, 10));
            AddModule(new PreyTracker(this, 10, 1f, 3f, 15f, 0.95f));
            AddModule(new RainTracker(this));

            // custom AI modules
            hungerTracker = new HungerTracker(this);
            AddModule(hungerTracker);
            shelterFinder = new ShelterFinder(this);
            AddModule(shelterFinder);

            // comparer
            AddModule(new UtilityComparer(this));
            utilityComparer.AddComparedModule(threatTracker, null, 0.9f, 1.1f);
            utilityComparer.AddComparedModule(preyTracker, null, 0.9f, 1.1f);
            utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 1f, 1.1f);

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

            if (lastBehavior != behavior || counter == 120)
            {
                BrainPlugin.Log($"slugcat behavior : {urge} {behavior} {urgeStrength}");
                counter = 0;
            }
            else counter++;

            WorldCoordinate destination = creature.pos;

            switch (behavior)
            {
                case Behavior.FollowPath:
                    break;

                case Behavior.Flee:
                    WorldCoordinate fleeTo = threatTracker.FleeTo(creature.pos, 5, 30, urgeStrength > 0.3f);
                    destination = fleeTo;
                    break;

                case Behavior.Hunt:
                    destination = preyTracker.MostAttractivePrey.BestGuessForPosition();
                    break;

                case Behavior.EscapeRain:
                    WorldCoordinate denPos = shelterFinder.GetShelterTarget();
                    destination = denPos;
                    break;

                case Behavior.GetUnstuck:
                    destination = stuckTracker.getUnstuckPosCalculator.unstuckGoalPosition;
                    break;
            }

            creature.abstractAI.SetDestinationNoPathing(destination, false);
            Destination = destination;

            DrawDebugNodes();
        }
        int counter = 0;

        void DrawDebugNodes()
        {
            Room room = creature.Room.realizedRoom;
            if (room == null) return;

            if (shelterNode == null) shelterNode = new DebugNode(new Color(0.86f, 0.53f, 0.82f));
            shelterNode.UpdatePosition(room, shelterFinder.ExitTile);

            if (threatNode == null) threatNode = new DebugNode(new Color(1f, 0.07f, 0.07f));
            if (threatTracker.mostThreateningCreature != null)
                threatNode.UpdatePosition(room, threatTracker.mostThreateningCreature.BestGuessForPosition().Tile);

            if (preyNode == null) preyNode = new DebugNode(new Color(1f, 0.4f, 0.05f));
            if (preyTracker.MostAttractivePrey != null)
                preyNode.UpdatePosition(room, preyTracker.MostAttractivePrey.BestGuessForPosition().Tile);

            if (overallDestinationNode == null) overallDestinationNode = new DebugNode(new Color(0.3f, 0.8f, 0.4f));
            if (Destination.room == room.abstractRoom.index) overallDestinationNode.UpdatePosition(room, Destination.Tile);
            else overallDestinationNode.UpdatePosition(room, new IntVector2(1, 1));

        }

        AIModule IUseARelationshipTracker.ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        {
            switch (relationship.type)
            {
                case CreatureTemplate.Relationship.Type.Afraid:
                    return threatTracker;
                case CreatureTemplate.Relationship.Type.Eats:
                    return preyTracker;
                default:
                    return null; 
            }
        }

        CreatureTemplate.Relationship IUseARelationshipTracker.UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship.Type type = dRelation.currentRelationship.type;
            float intensity = dRelation.currentRelationship.intensity;

            if (type == CreatureTemplate.Relationship.Type.Eats)
            {
                if (hungerTracker.Utility() > 0.4f)
                {
                    intensity = Mathf.Clamp01(intensity + 0.1f);
                }
            }
            else if (dRelation.trackedByModuleWeigth > 0.65f)
            {
                intensity = Mathf.Clamp01(intensity + 0.05f);
            }

            return new CreatureTemplate.Relationship(type, intensity);
        }

        RelationshipTracker.TrackedCreatureState IUseARelationshipTracker.CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
        {
            return null;
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
        HungerTracker hungerTracker;

        public Behavior behavior;

        DebugNode shelterNode;
        DebugNode threatNode;
        DebugNode preyNode;
        DebugNode overallDestinationNode;

        public WorldCoordinate Destination { get; private set; }

    }
}
