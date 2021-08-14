﻿using RWCustom;
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
            standardPathFinder = new StandardPather(this, world, creature);
            swimmingPathFinder = new FishPather(this, world, creature);
            AddModule(standardPathFinder);
            AddModule(new StuckTracker(this, false, true));
            stuckTracker.AddSubModule(new StuckTracker.GetUnstuckPosCalculator(stuckTracker));
            AddModule(new Tracker(this, 10, 20, -1, 0.35f, 5, 5, 10));
            AddModule(new RelationshipTracker(this, tracker));
            AddModule(new ThreatTracker(this, 10));
            AddModule(new RainTracker(this));

            // custom AI modules
            treatTracker = new TreatTracker(this, 8, 1f, 360f);
            AddModule(treatTracker);
            shelterFinder = new ShelterFinder(this);
            AddModule(shelterFinder);

            // comparer
            AddModule(new UtilityComparer(this));
            utilityComparer.AddComparedModule(threatTracker, null, 0.9f, 1.1f);
            utilityComparer.AddComparedModule(treatTracker, null, 0.85f, 1.1f);
            utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 1f, 1.1f);

            submergedLastUpdate = false;

            behavior = Behavior.FollowPath;

            BrainPlugin.Log("SlugcatAI ctor done");
        }

        public override void Update()
        {
            base.Update();

            if (creature.Room.realizedRoom == null || creature.realizedCreature == null) return;

            DrawDebugNodes();


            bool submerged = creature.Room.realizedRoom.PointSubmerged(creature.realizedCreature.bodyChunks[1].pos);
            if (submerged != submergedLastUpdate)
            {
                if (submerged)
                {
                    modules.Remove(standardPathFinder);
                    AddModule(swimmingPathFinder);
                }
                else
                {
                    modules.Remove(swimmingPathFinder);
                    AddModule(standardPathFinder);
                }

                pathFinder.Reset(creature.Room.realizedRoom);
                BrainPlugin.Log($"path finder set to {pathFinder}");
            }
            submergedLastUpdate = submerged;


            AIModule urge = utilityComparer.HighestUtilityModule();
            float urgeStrength = utilityComparer.HighestUtility();

            SetAIDebugLabel(urge);

            if (urge is ThreatTracker) behavior = Behavior.Flee;
            else if (urge is RainTracker) behavior = Behavior.EscapeRain;
            else if (urge is TreatTracker) behavior = Behavior.Hunt;
            else if (urge is StuckTracker) behavior = Behavior.GetUnstuck;

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
                    destination = treatTracker.GetMostAttractiveFoodDestination(out _);
                    break;

                case Behavior.EscapeRain:
                    WorldCoordinate denPos = shelterFinder.GetShelterTarget();
                    destination = denPos;
                    break;

                case Behavior.GetUnstuck:
                    destination = stuckTracker.getUnstuckPosCalculator.unstuckGoalPosition;
                    break;
            }

            if (destination.room == -1) return;

            if (Destination != destination)
            {
                BrainPlugin.Log($"slugcat behavior : {urge} {behavior} {urgeStrength}\n" +
                    $"destination : {destination}");
            }

            SetDestination(destination);
        }

        void DrawDebugNodes()
        {
            Room room = creature.Room.realizedRoom;
            if (room == null) return;

            treatTracker.DrawDebugNodes();


            if (shelterNode == null) shelterNode = new DebugNode(new Color(0.86f, 0.53f, 0.82f));
            shelterNode.UpdatePosition(room, shelterFinder.ExitTile);

            if (threatNode == null) threatNode = new DebugNode(new Color(1f, 0.07f, 0.07f));
            if (threatTracker.mostThreateningCreature != null)
                threatNode.UpdatePosition(room, threatTracker.mostThreateningCreature.BestGuessForPosition().Tile);

            if (overallDestinationNode == null) overallDestinationNode = new DebugNode(new Color(0.3f, 0.8f, 0.4f));
            if (Destination.room == room.abstractRoom.index)
            {
                IntVector2 t = DestinationTile;
                overallDestinationNode.UpdatePosition(room, t);
            }
            else overallDestinationNode.UpdatePosition(room, new IntVector2(1, room.TileHeight - 1));

        }

        void SetAIDebugLabel(AIModule urge)
        {
            if (AIdebugLabel == null)
            {
                AIdebugLabel = new FLabel("font", "-")
                {
                    color = new Color(0.5f, 0.7f, 0.8f),
                    scale = 1.2f
                };
                Futile.stage.AddChild(AIdebugLabel);
            }

            string destHint = Destination.room == creature.Room.index ? $" ({DestinationTile})" : "";

            AIdebugLabel.text = $"top module : {urge} {urge.Utility()}\n" +
                $"active path finder : {pathFinder}\n" +
                $"dest : {Destination} {destHint}";

            Vector2 pos = creature.realizedCreature.mainBodyChunk.pos + creature.Room.realizedRoom.game.cameras[0].pos;
            AIdebugLabel.x = pos.x;
            AIdebugLabel.y = pos.y;

        }

        public new void SetDestination(WorldCoordinate dest)
        {
            base.SetDestination(dest);
            creature.abstractAI.SetDestination(dest);
            Destination = dest;
        }

        AIModule IUseARelationshipTracker.ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        {
            switch (relationship.type)
            {
                case CreatureTemplate.Relationship.Type.Afraid:
                    return threatTracker;
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
                if (treatTracker.Utility() > 0.4f)
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
            // TODO
            return null;
        }


        public float EstimateTileDistance(WorldCoordinate from, WorldCoordinate to)
        {
            float dist;

            if (from.room != to.room)
            {
                AbstractRoom abstractFrom = creature.world.GetAbstractRoom(from.room);
                int exitToRoom = abstractFrom.ExitIndex(to.room);

                if (exitToRoom > -1)   // check rooms are connected
                {
                    dist = Custom.WorldCoordFloatDist(from, abstractFrom.realizedRoom.LocalCoordinateOfNode(exitToRoom));
                }
                else dist = float.PositiveInfinity;
            }
            else dist = Custom.WorldCoordFloatDist(from, to);

            return dist;
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
        public ShelterFinder shelterFinder;
        public TreatTracker treatTracker;
        public PathFinder standardPathFinder;
        public PathFinder swimmingPathFinder;

        bool submergedLastUpdate;

        public Behavior behavior;

        DebugNode shelterNode;
        DebugNode threatNode;
        DebugNode overallDestinationNode;
        FLabel AIdebugLabel;

        public WorldCoordinate Destination { get; private set; }

        public IntVector2 DestinationTile =>
            Destination.TileDefined ? Destination.Tile : creature.Room.realizedRoom.LocalCoordinateOfNode(Destination.abstractNode).Tile;

    }
}
