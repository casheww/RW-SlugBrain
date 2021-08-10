using RWCustom;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    class SuperSlugcat : Player
    {
        public SuperSlugcat(AbstractCreature absCreature, World world) : base(absCreature, world)
        {
            lastInput = new InputPackage();
            sleeping = false;
        }

        public override void Update(bool eu)
        {
            if (Consious) Act();

            base.Update(eu);
        }

        void Act()
        {
            AI.Update();
            FollowPath();
        }

        void FollowPath()
        {
            WorldCoordinate headCoords = room.GetWorldCoordinate(mainBodyChunk.pos);
            WorldCoordinate bodyCoords = room.GetWorldCoordinate(bodyChunks[1].pos);
            MovementConnection movement = (AI.pathFinder as StandardPather).FollowPath(headCoords, true);

            if (movement == null) movement = (AI.pathFinder as StandardPather).FollowPath(bodyCoords, true);

            if (movement == null) movement =
                    new MovementConnection(MovementConnection.MovementType.Standard, headCoords, headCoords, 0);

            Move(movement);
        }

        void Move(MovementConnection movement)
        {
            BrainPlugin.Log(movement);

            if (debugNode == null) debugNode = new DebugNode(Color.white);
            debugNode.UpdatePosition(room, movement.DestTile);

            Vector2 dir = Custom.DirVec(movement.StartTile.ToVector2(), movement.DestTile.ToVector2());
            BrainPlugin.Log(dir);

            bool jmp = movement.type == MovementConnection.MovementType.ReachUp ||
                       movement.type == MovementConnection.MovementType.DoubleReachUp ||
                       movement.type == MovementConnection.MovementType.ReachOverGap;

            int x;
            int y;

            BrainPlugin.Log(room.GetTile(movement.DestTile).Terrain);
            if (room.GetTile(movement.DestTile).Terrain == Room.Tile.TerrainType.Slope)
            {
                Room.SlopeDirection slope = room.IdentifySlope(movement.DestTile);

                if (lastInput.x != 0) x = lastInput.x;
                else
                {
                    float _x = Custom.DirVec(movement.StartTile.ToVector2(), AI.Destination.Tile.ToVector2()).x;
                    x = _x < 0 ? -1 : (_x > 0 ? 1 : 0);
                }

                if (slope == Room.SlopeDirection.UpLeft || slope == Room.SlopeDirection.UpRight) y = 1;
                else y = 0;
            }
            else
            {
                x = dir.x <= -1 ? -1 : (dir.x >= 1 ? 1 : 0);
                y = dir.y <= -1 ? -1 : (dir.y >= 1 || jmp ? 1 : 0);
            }
            
            InputPackage input = new InputPackage(
                false,
                x, y,
                jmp,
                false, false, false, false
            );

            BrainPlugin.InputSpoofer.SetNewInputs(input);
            lastInput = input;
        }


        public SlugcatAI AI;
        DebugNode debugNode = null;

        InputPackage lastInput;
        public bool sleeping;

    }
}
