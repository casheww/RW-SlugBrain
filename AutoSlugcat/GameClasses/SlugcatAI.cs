using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    class SlugcatAI : ArtificialIntelligence, IUseARelationshipTracker
    {
        public SlugcatAI(AbstractCreature creature, World world) : base(creature, world)
        {
            _slugcat = creature.realizedCreature as SuperSlugcat;
            _slugcat.ai = this;

            // vanilla AI modules
            //AddModule(new StuckTracker(this, false, true));
            //stuckTracker.AddSubModule(new StuckTracker.GetUnstuckPosCalculator(stuckTracker));
            AddModule(new Tracker(this, 10, 20, -1, 0.35f, 5, 5, 10));
            AddModule(new RelationshipTracker(this, tracker));
            AddModule(new ThreatTracker(this, 10));
            AddModule(new RainTracker(this));

            // custom AI modules
            aStarPathFinder = new AStarPathfinder(this, world, creature);
            AddModule(aStarPathFinder);
            treatTracker = new TreatTracker(this, 8, 1f, 360f);
            AddModule(treatTracker);
            shelterFinder = new ShelterFinder(this);
            AddModule(shelterFinder);
            
            jumpModule = new JumpModule(this);      // not a real AI module

            // comparer
            AddModule(new UtilityComparer(this));
            utilityComparer.AddComparedModule(threatTracker, null, 0.9f, 1.1f);
            utilityComparer.AddComparedModule(treatTracker, null, 0.85f, 1.1f);
            utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
            //utilityComparer.AddComparedModule(stuckTracker, null, 1f, 1.1f);

            behavior = Behavior.Idle;
            _roomMemory = new List<RoomRepresentation>();

            BrainPlugin.Log("SlugcatAI ctor done");
        }

        public override void Update()
        {
            base.Update();

            if (creature.Room.realizedRoom == null || creature.realizedCreature == null) return;

            foreach (Room room in creature.world.activeRooms)
            {
                UpdateRoomRepresentation(room);
            }
            BrainPlugin.TextManager.Write("room memory", _roomMemory.Count);
            
            DrawDebugNodes();

            AIModule urge = utilityComparer.HighestUtilityModule();
            float urgeStrength = utilityComparer.HighestUtility();

            DoTextDebugs(urge);

            if (urge.Utility() == 0) behavior = Behavior.Idle;
            else if (urge is ThreatTracker) behavior = Behavior.Flee;
            else if (urge is RainTracker) behavior = Behavior.EscapeRain;
            else if (urge is TreatTracker) behavior = Behavior.FindFood;
            else if (urge is StuckTracker) behavior = Behavior.GetUnstuck;

            
            WorldCoordinate destination = creature.pos;

            // get destination from relevant module
            switch (behavior)
            {
                case Behavior.Idle:
                    destination = new WorldCoordinate(-1, -1, -1, -1);
                    break;

                case Behavior.Flee:
                    WorldCoordinate fleeTo = threatTracker.FleeTo(creature.pos, 5, 30, urgeStrength > 0.3f);
                    destination = fleeTo;
                    break;

                case Behavior.FindFood:
                    destination = treatTracker.GetMostAttractiveFoodDestination(out _);
                    break;

                case Behavior.EscapeRain:
                    destination = shelterFinder.GetShelterTarget();
                    break;

                case Behavior.GetUnstuck:
                    destination = stuckTracker.getUnstuckPosCalculator.unstuckGoalPosition;
                    break;
            }

            // explore if the module has no destination in mind
            if (destination.room == -1)     // screaming
            {
                destination = Explore(out float desire, out bool randomRoom);
                BrainPlugin.TextManager.Write("exploring", $"{destination} : {desire} : random?{randomRoom}", new Color(0.7f, 0.3f, 0.4f), 50);
            }

            if (destination.room == -1)     // more screaming
            {
                BrainPlugin.TextManager.Write("uh oh", "can't find rooms to explore what aaaaaaa ?!?!?!?!", Color.red, 40);
                return;
            }

            // is this working?
            if (Destination != destination)
            {
                BrainPlugin.Log($"slugcat behavior : {urge} {behavior} {urgeStrength}\n" +
                    $"destination : {destination}");
            }

            SetDestination(destination);
        }

        public new void SetDestination(WorldCoordinate dest)
        {
            base.SetDestination(dest);
            aStarPathFinder.SetDestination(dest);
            Destination = dest;
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);

            _randomExitToExplore = null;
        }

        public void UpdateRoomRepresentation(Room room)
        {
            RoomRepresentation thisRoomRep = null;

            // check if this room has been visited before
            foreach (RoomRepresentation rRep in _roomMemory)
            {
                if (rRep.room == room.abstractRoom)
                {
                    thisRoomRep = rRep;
                    break;
                }
            }

            if (thisRoomRep == null)
            {
                thisRoomRep = new RoomRepresentation(room.abstractRoom);
                _roomMemory.Add(thisRoomRep);
            }

            foreach (AIModule module in modules)
            {
                if (module is SlugcatAIModule slugModule)
                {
                    slugModule.UpdateRoomRepresentation(thisRoomRep);
                }
            }

        }

        public WorldCoordinate Explore(out float desire, out bool randomRoom)
        {
            RoomRepresentation closestToShelter = null;
            float shelterDistance = float.MaxValue;

            foreach (RoomRepresentation rRep in _roomMemory)
            {
                if (rRep.distToShelter < shelterDistance)
                {
                    closestToShelter = rRep;
                    shelterDistance = rRep.distToShelter;
                }
            }

            RoomRepresentation mostDesirable = null;
            desire = float.MinValue;
            int exitToBestRoom = -1;

            // find most attractive room in memory
            foreach (RoomRepresentation rRep in _roomMemory)
            {
                if (rRep.room == creature.Room) continue;

                // use only connected rooms
                int exitToRoom = creature.Room.ExitIndex(rRep.room.index);
                if (exitToRoom > -1)
                {
                    float attractiveness = rRep.Attractiveness(treatTracker.Utility() > 0);

                    if (closestToShelter == rRep) attractiveness *= 1f + rainTracker.Utility();
                    if (creature.world.GetAbstractRoom(lastRoom) == rRep.room) attractiveness *= 0.75f;

                    if (attractiveness > desire)
                    {
                        mostDesirable = rRep;
                        desire = attractiveness;
                        exitToBestRoom = exitToRoom;
                    }
                }
            }

            // if no options are very attractive, pick a random connected room
            if (desire < 0.2f)
            {
                if (_randomExitToExplore == null)
                {
                    int conn = Mathf.RoundToInt(Random.Range(0, creature.Room.connections.Length));
                    exitToBestRoom = creature.Room.ExitIndex(creature.Room.connections[conn]);
                    _randomExitToExplore = exitToBestRoom;
                }
                else exitToBestRoom = _randomExitToExplore.Value;
                
                randomRoom = true;
            }
            else
            {
                _randomExitToExplore = null;
                randomRoom = false;
            }

            if (mostDesirable != null)
                return creature.Room.realizedRoom.LocalCoordinateOfNode(exitToBestRoom);
            else
                return new WorldCoordinate(-1, -1, -1, -1);

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
                else dist = 500f;
            }
            else dist = Custom.WorldCoordFloatDist(from, to);

            return dist;
        }


        void DrawDebugNodes()
        {
            Room room = creature.Room.realizedRoom;
            if (room == null) return;

            treatTracker.DrawDebugNodes();
            shelterFinder.DrawDebugNode();

            if (threatTracker.mostThreateningCreature != null)
                BrainPlugin.NodeManager.Draw("threat",
                    DebugColors.GetColor(DebugColors.Subject.Threat),
                    room, threatTracker.mostThreateningCreature.BestGuessForPosition().Tile);
            
            if (Destination.room == room.abstractRoom.index)
            {
                BrainPlugin.NodeManager.Draw("destination",
                    DebugColors.GetColor(DebugColors.Subject.Destination),
                    room, DestinationTile);
            }
            else
            {
                BrainPlugin.NodeManager.Draw("destination",
                    DebugColors.GetColor(DebugColors.Subject.Destination),
                    room, new IntVector2(1, 1));
            }
        }

        void DoTextDebugs(AIModule highestModule)
        {
            BrainPlugin.TextManager.Write("pos   ", creature.pos);
            BrainPlugin.TextManager.Write("dest  ", Destination);
            BrainPlugin.TextManager.Write("mvmnt ", _slugcat.LastMovement);

            Color moduleColor = DebugColors.GetColor(highestModule);
            BrainPlugin.TextManager.Write("top module", $"{highestModule} {highestModule.Utility()}", moduleColor);
        }


        public enum Behavior
        {
            Idle,
            Flee,
            FindFood,
            EscapeRain,
            GetUnstuck
        }


        private SuperSlugcat _slugcat;
        public ShelterFinder shelterFinder;
        public TreatTracker treatTracker;
        public AStarPathfinder aStarPathFinder;
        public JumpModule jumpModule;
        
        public Behavior behavior;

        private readonly List<RoomRepresentation> _roomMemory;
        private int? _randomExitToExplore;

        public WorldCoordinate Destination { get; private set; }

        public IntVector2 DestinationTile =>
            Destination.TileDefined ? Destination.Tile : creature.Room.realizedRoom.LocalCoordinateOfNode(Destination.abstractNode).Tile;

    }
}
