using System;
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
            stationaryCounter = 0;
            preferedChunkIndex = 0;
        }

        public override void Update(bool eu)
        {
            if (Consious) Act();

            base.Update(eu);
        }

        void Act()
        {
            AI.Update();

            if (room.abstractRoom.shelter)
            {
                Hibernate();
                return;
            }
            
            FollowPath();
        }

        void FollowPath()
        {
            WorldCoordinate preferedStart = room.GetWorldCoordinate(bodyChunks[preferedChunkIndex].pos);
            WorldCoordinate backupStart = room.GetWorldCoordinate(bodyChunks[NotPreferedChunkIndex].pos);

            if (lastPreferedStart == preferedStart) stationaryCounter++;
            else stationaryCounter = 0;

            if (stationaryCounter > 40 && !sleeping)
            {
                preferedChunkIndex = NotPreferedChunkIndex;
                stationaryCounter = 0;
                BrainPlugin.Log($"swapped prefered start chunk index to {preferedChunkIndex}");
            }

            FollowPath(preferedStart, backupStart);

            lastPreferedStart = preferedStart;
        }

        void FollowPath(WorldCoordinate preferedStart, WorldCoordinate backupStart)
        {
            MovementConnection movement;

            if (AI.pathFinder is StandardPather standardPather)
            {
                movement = standardPather.FollowPath(preferedStart, true);

                if (movement == null) movement = standardPather.FollowPath(backupStart, true);
            }
            else if (AI.pathFinder is FishPather fishPather)
            {
                movement = fishPather.FollowPath(preferedStart, true);

                if (movement == null) movement = fishPather.FollowPath(backupStart, true);
            }
            else return;

            if (movement == null) movement =
                    new MovementConnection(MovementConnection.MovementType.Standard, preferedStart, preferedStart, 0);

            Move(movement);
        }

        void Move(MovementConnection movement)
        {
            if (destNode == null) destNode = new DebugNode(Color.white);
            destNode.UpdatePosition(room, movement.DestTile);

            if (currentNode == null) currentNode = new DebugNode(new Color(0.1f, 0.6f, 0.2f));
            currentNode.UpdatePosition(room, movement.StartTile);

            Vector2 dir = Custom.DirVec(movement.StartTile.ToVector2(), movement.DestTile.ToVector2());
            Vector2 destDir = Custom.DirVec(movement.StartTile.ToVector2(), AI.Destination.Tile.ToVector2());

            bool jmp = movement.type == MovementConnection.MovementType.ReachUp ||
                       movement.type == MovementConnection.MovementType.DoubleReachUp ||
                       movement.type == MovementConnection.MovementType.ReachOverGap;

            int x;
            int y;

            Room.Tile startTile = room.GetTile(movement.StartTile);
            Room.Tile destTile = room.GetTile(movement.DestTile);

            if (destTile.Terrain == Room.Tile.TerrainType.Slope)
            {
                Room.SlopeDirection slope = room.IdentifySlope(movement.DestTile);
                BrainPlugin.Log($"slope {slope}");

                if (lastInput.x != 0) x = lastInput.x;
                else x = Math.Sign(destDir.x); 

                if (slope == Room.SlopeDirection.UpLeft || slope == Room.SlopeDirection.UpRight) y = 1;
                else y = 0;
            }
            else if (startTile.verticalBeam && Mathf.Abs(dir.x) > 0)
            {
                BrainPlugin.Log("jumping off vertical beam");

                x = Math.Sign(dir.x);
                y = 0;
                jmp = true;
            }
            else
            {
                x = Math.Sign(dir.x);
                y = jmp ? 1 : Math.Sign(dir.y);
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

        void Hibernate()
        {
            if (!room.shelterDoor.IsClosing)
            {
                int x = abstractCreature.pos.x;
                if (x == 25)
                {
                    BrainPlugin.InputSpoofer.SetNewInputs(new InputPackage());
                }
                else FollowPath();
            }
        }


        public SlugcatAI AI;
        DebugNode destNode;
        DebugNode currentNode;

        InputPackage lastInput;
        public bool sleeping;
        int stationaryCounter;
        WorldCoordinate lastPreferedStart;

        int preferedChunkIndex;
        int NotPreferedChunkIndex => preferedChunkIndex == 0 ? 1 : 0;

    }
}
