using RWCustom;

namespace SlugBrain.GameClasses
{
    class ShelterFinder : AIModule
    {
        public ShelterFinder(ArtificialIntelligence AI) : base(AI) { }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);

            int i = GetExitToClosestShelter(out float dist);
            if (i > -1)
            {
                exitToShelter = i;
                BrainPlugin.Log($"exit to closest shelter : {exitToShelter}\n\t\ttotal distance to shelter : {dist}");
            }
            else
            {
                exitToShelter = 0;
                BrainPlugin.Log($"couldn't find any shelters!");
            }
            
        }

        public WorldCoordinate GetShelterTarget()
        {
            if (AI.creature.Room.shelter)
            {
                // the common safe point of vanilla shelters...
                // there may be some special cases yet to be accounted for
                return new WorldCoordinate(AI.creature.Room.index, 25, 15, -1);
            }
            // focus the exit node that takes us closest to the nearest shelter
            else return new WorldCoordinate(AI.creature.Room.index, -1, -1, exitToShelter);
        }

        public int GetExitToClosestShelter(out float shortestDistToClosestShelter)
        {
            int exitClosestToAnyShelter = -1;
            shortestDistToClosestShelter = float.PositiveInfinity;

            for (int shelterInt = 0; shelterInt < AI.creature.world.shelters.Length; shelterInt++)
            {
                int exitClosestToThisShelter = -1;
                float shortestDist = float.PositiveInfinity;

                for (int connIndex = 0; connIndex < AI.creature.Room.connections.Length; connIndex++)
                {
                    WorldCoordinate coord = new WorldCoordinate(AI.creature.Room.index, -1, -1, connIndex);
                    float dist = AI.creature.world.overseersWorldAI.shelterFinder.DistanceToShelter(shelterInt, coord);

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


        int exitToShelter;

        public IntVector2 ExitTile => AI.creature.Room.realizedRoom.LocalCoordinateOfNode(exitToShelter).Tile;

    }
}
