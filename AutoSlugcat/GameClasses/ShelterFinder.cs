using RWCustom;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    class ShelterFinder : AIModule
    {
        public ShelterFinder(ArtificialIntelligence AI, AbstractCreature creature) : base(AI)
        {
            this.creature = creature;
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);

            int i = GetExitToClosestShelter(out float dist);
            if (i > -1)
            {
                ExitToShelter = i;
                BrainPlugin.Log($"exit to closest shelter : {ExitToShelter}\n\t\ttotal distance to shelter : {dist}");
            }
            else
            {
                ExitToShelter = 0;
                BrainPlugin.Log($"couldn't find any shelters!");
            }
            
        }

        public WorldCoordinate GetShelterTarget()
        {
            if (creature.Room.shelter)
            {
                return new WorldCoordinate(creature.Room.index, 25, 15, -1);
            }
            else return new WorldCoordinate(creature.Room.index, -1, -1, ExitToShelter);
        }

        public override float Utility()
        {
            return base.Utility();
        }

        public int GetExitToClosestShelter(out float shortestDistToClosestShelter)
        {
            int exitClosestToAnyShelter = -1;
            shortestDistToClosestShelter = float.PositiveInfinity;

            for (int shelterInt = 0; shelterInt < creature.world.shelters.Length; shelterInt++)
            {
                int exitClosestToThisShelter = -1;
                float shortestDist = float.PositiveInfinity;

                for (int connIndex = 0; connIndex < creature.Room.connections.Length; connIndex++)
                {
                    WorldCoordinate coord = new WorldCoordinate(creature.Room.index, -1, -1, connIndex);
                    float dist = creature.world.overseersWorldAI.shelterFinder.DistanceToShelter(shelterInt, coord);

                    if (dist < shortestDist)
                    {
                        exitClosestToThisShelter = connIndex;
                        shortestDist = dist;
                    }
                }

                if (shortestDist < shortestDistToClosestShelter)
                {
                    exitClosestToAnyShelter = exitClosestToThisShelter;
                    shortestDistToClosestShelter = shortestDist;
                }
            }

            return exitClosestToAnyShelter;
        }


        AbstractCreature creature;

        int ExitToShelter
        {
            get => _exitToShelter;
            set
            {
                _exitToShelter = value;
                exitTile = creature.Room.realizedRoom.LocalCoordinateOfNode(value).Tile;

                if (BrainPlugin.debugAI && creature.Room.realizedRoom != null)
                {
                    if (debugNode == null) debugNode = new DebugNode(Color.green);
                    debugNode.UpdatePosition(creature.Room.realizedRoom, exitTile);
                }
            }
        }
        int _exitToShelter;

        IntVector2 exitTile;

        DebugNode debugNode = null;

    }
}
