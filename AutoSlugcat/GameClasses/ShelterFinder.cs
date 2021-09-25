
namespace SlugBrain.GameClasses
{
    class ShelterFinder : SlugcatAIModule
    {
        public ShelterFinder(ArtificialIntelligence ai) : base(ai) { }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);

            int i = GetExitToClosestShelter(AI.creature.Room, out float dist);
            if (i > -1)
            {
                _exitToShelter = i;
                DistanceToShelter = dist;
                BrainPlugin.Log($"exit to closest shelter : {_exitToShelter}\n\t\ttotal distance to shelter : {dist}");
            }
            else
            {
                _exitToShelter = 0;
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
            else
            {
                ExitToShelterCoords = AI.creature.Room.realizedRoom.LocalCoordinateOfNode(_exitToShelter);
                return ExitToShelterCoords;
            }
        }

        public int GetExitToClosestShelter(AbstractRoom room, out float shortestDistToClosestShelter)
        {
            int exitClosestToAnyShelter = -1;
            shortestDistToClosestShelter = float.PositiveInfinity;

            for (int shelterInt = 0; shelterInt < AI.creature.world.shelters.Length; shelterInt++)
            {
                int exitClosestToThisShelter = -1;
                float shortestDist = float.PositiveInfinity;

                for (int connIndex = 0; connIndex < room.connections.Length; connIndex++)
                {
                    WorldCoordinate coord = new WorldCoordinate(room.index, -1, -1, connIndex);
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

        public void DrawDebugNode()
        {
            if (AI.creature.Room.index == ExitToShelterCoords.room)
            {
                BrainPlugin.NodeManager.Draw("shelter",
                    DebugColors.GetColor(DebugColors.Subject.Shelter),
                    AI.creature.Room.realizedRoom, ExitToShelterCoords.Tile);

                BrainPlugin.Log($"DFSKJKFSDJJKFSD : {ExitToShelterCoords.Tile}");
            }
        }

        public override void UpdateRoomRepresentation(RoomRepresentation rRep)
        {
            GetExitToClosestShelter(rRep.room, out float dist);
            rRep.distToShelter = dist;
        }


        private int _exitToShelter;
        public WorldCoordinate ExitToShelterCoords { get; private set; }
        public float DistanceToShelter { get; private set; }
        
        
    }
}
