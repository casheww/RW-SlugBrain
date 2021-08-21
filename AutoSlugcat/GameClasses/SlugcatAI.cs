using RWCustom;
using System.Collections.Generic;
using System.Linq;
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
            jumpModule = new JumpModule(this);      // not a real AI module

            // comparer
            AddModule(new UtilityComparer(this));
            utilityComparer.AddComparedModule(threatTracker, null, 0.9f, 1.1f);
            utilityComparer.AddComparedModule(treatTracker, null, 0.85f, 1.1f);
            utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 1f, 1.1f);

            submergedLastUpdate = false;
            behavior = Behavior.Idle;
            roomsVisited = new List<RoomRepresentation>();

            BrainPlugin.Log("SlugcatAI ctor done");
        }

        public override void Update()
        {
            // affects visibility of disco mode pathfinder on next room transition
            if (Input.GetKeyDown(KeyCode.Home)) pathFinder.visualize = !pathFinder.visualize;

            base.Update();

            if (creature.Room.realizedRoom == null || creature.realizedCreature == null) return;

            foreach (Room room in creature.world.activeRooms)
            {
                UpdateRoomRepresentation(room);
            }
            BrainPlugin.TextManager.Write("room memory", roomsVisited.Count);
            
            DrawDebugNodes();

            // switch pathfinder when entering/exiting water. Water pathfinder just seems to float... perhaps I'm missing some method call?
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
                destination = Explore();
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
            creature.abstractAI.SetDestination(dest);
            Destination = dest;
        }

        public void UpdateRoomRepresentation(Room room)
        {
            RoomRepresentation thisRoomRep = null;

            // check if this room has been visited before
            foreach (RoomRepresentation rRep in roomsVisited)
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
                roomsVisited.Add(thisRoomRep);
            }

            thisRoomRep.food = treatTracker.FoodsInRoom(room.abstractRoom, true).Count;
            thisRoomRep.threats =
                threatTracker.threatCreatures.Where(t => t.creature.representedCreature.Room == room.abstractRoom).ToArray().Length;

        }

        public WorldCoordinate Explore()
        {
            RoomRepresentation mostDesirable = null;
            float highestDesirability = float.MinValue;
            int exitToBestRoom = -1;

            foreach (RoomRepresentation rRep in roomsVisited)
            {
                if (rRep.room == creature.Room) continue;

                // use only connected rooms
                int exitToRoom = creature.Room.ExitIndex(rRep.room.index);
                if (exitToRoom > -1)
                {
                    BrainPlugin.Log(rRep.DesireToGoBack);
                    if (rRep.DesireToGoBack > highestDesirability)
                    {
                        mostDesirable = rRep;
                        highestDesirability = rRep.DesireToGoBack;
                        exitToBestRoom = exitToRoom;
                    }
                }
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


            if (threatNode == null) threatNode = new DebugNode(DebugColors.GetColor(DebugColors.Subject.Threat));
            if (threatTracker.mostThreateningCreature != null)
                threatNode.SetPosition(room, threatTracker.mostThreateningCreature.BestGuessForPosition().Tile);

            if (overallDestinationNode == null) overallDestinationNode =
                    new DebugNode(DebugColors.GetColor(DebugColors.Subject.Destination), true);
            if (Destination.room == room.abstractRoom.index)
            {
                IntVector2 t = DestinationTile;
                overallDestinationNode.SetPosition(room, t);
            }
            else
            {
                overallDestinationNode.SetPosition(room, new IntVector2(1, 1));
            }
        }

        void DoTextDebugs(AIModule highestModule)
        {
            BrainPlugin.TextManager.Write("pos   ", creature.pos);
            BrainPlugin.TextManager.Write("dest  ", Destination);
            BrainPlugin.TextManager.Write("mvmnt ", slugcat.LastMovement);
            BrainPlugin.TextManager.Write("active pathfinder", pathFinder);

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


        SuperSlugcat slugcat;
        public ShelterFinder shelterFinder;
        public TreatTracker treatTracker;
        public PathFinder standardPathFinder;
        public PathFinder swimmingPathFinder;
        public JumpModule jumpModule;

        bool submergedLastUpdate;

        public Behavior behavior;

        public List<RoomRepresentation> roomsVisited;

        DebugNode threatNode;
        DebugNode overallDestinationNode;

        public WorldCoordinate Destination { get; private set; }

        public IntVector2 DestinationTile =>
            Destination.TileDefined ? Destination.Tile : creature.Room.realizedRoom.LocalCoordinateOfNode(Destination.abstractNode).Tile;

    }
}
