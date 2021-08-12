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
            wantToSleep = false;
            stationaryCounter = 0;
            preferedChunkIndex = 0;
            leftShelterThisCycle = false;
        }

        public override void Update(bool eu)
        {
            if (Consious) Act();

            DebugTerrain();

            base.Update(eu);
        }

        void Act()
        {
            AI.Update();

            if (!leftShelterThisCycle && !room.abstractRoom.shelter) leftShelterThisCycle = true;

            if (AI.rainTracker.Utility() > 0.6f)    // || (leftShelterThisCycle && room.abstractRoom.shelter)
            {
                wantToSleep = true;
                BrainPlugin.Log("sleepy time soon");
            }

            if (room.abstractRoom.shelter)
            {
                if (wantToSleep)
                {
                    Hibernate();
                    return;
                }
                else
                {
                    BrainPlugin.Log("trying to leave shelter");
                    AI.SetDestination(new WorldCoordinate(room.abstractRoom.index, -1, -1, 0));
                }
                
            }

            if (CurrentFood < MaxFoodInStomach)
            {
                foreach (Grasp g in grasps)
                {
                    if (g != null && g.grabbed is IPlayerEdible _e && Safe)
                    {
                        BrainPlugin.Log($"eating {_e}");
                        GrabOrEat();
                        return;
                    }
                }

                WorldCoordinate nearestEdibleCoords =
                    AI.treatTracker.GetNearestEdibleLocation(out PhysicalObject edible);
                if (edible != null)
                {
                    if (Custom.DistLess(mainBodyChunk.pos, edible.bodyChunks[0].pos, 40f))
                    {
                        BrainPlugin.Log($"grabbing {edible}");
                        GrabOrEat();
                    }
                }
                
            }

            FollowPath();
        }

        void FollowPath()
        {
            WorldCoordinate preferedStart = room.GetWorldCoordinate(bodyChunks[preferedChunkIndex].pos);
            WorldCoordinate backupStart = room.GetWorldCoordinate(bodyChunks[NotPreferedChunkIndex].pos);

            if (lastPreferedStart == preferedStart) stationaryCounter++;
            else stationaryCounter = 0;

            bool gettingUnstuck = false;

            if (stationaryCounter > 40 && !wantToSleep)
            {
                preferedChunkIndex = NotPreferedChunkIndex;
                stationaryCounter = 0;
                BrainPlugin.Log($"swapped prefered start chunk index to {preferedChunkIndex}");
                gettingUnstuck = true;
            }

            FollowPath(preferedStart, backupStart, gettingUnstuck);

            lastPreferedStart = preferedStart;
        }

        void FollowPath(WorldCoordinate preferedStart, WorldCoordinate backupStart, bool gettingUnstuck)
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

            Move(movement, gettingUnstuck);
        }

        void Move(MovementConnection movement, bool unstick = false)
        {
            if (destNode == null) destNode = new DebugNode(Color.white);
            destNode.UpdatePosition(room, movement.DestTile);

            if (currentNode == null) currentNode = new DebugNode(new Color(0.1f, 0.6f, 0.2f));
            currentNode.UpdatePosition(room, movement.StartTile);

            Vector2 dir = Custom.DirVec(movement.StartTile.ToVector2(), movement.DestTile.ToVector2());
            //Vector2 destDir = Custom.DirVec(movement.StartTile.ToVector2(), AI.Destination.Tile.ToVector2());

            bool jmp = movement.type == MovementConnection.MovementType.ReachUp ||
                       movement.type == MovementConnection.MovementType.DoubleReachUp ||
                       movement.type == MovementConnection.MovementType.ReachOverGap ||
                       unstick;

            int x;
            int y;

            Room.Tile startTile = room.GetTile(movement.StartTile);
            Room.Tile destTile = room.GetTile(movement.DestTile);

            if (destTile.Terrain == Room.Tile.TerrainType.Slope)
            {
                Room.SlopeDirection slope = room.IdentifySlope(movement.DestTile);
                BrainPlugin.Log($"slope {slope}");

                if (lastInput.x != 0) x = lastInput.x;
                else x = dir.x < 0 ? -1 : (dir.x > 0 ? 1 : 0); 

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
            else BrainPlugin.InputSpoofer.SetNewInputs(new InputPackage());
        }

        void GrabOrEat()
        {
            BrainPlugin.InputSpoofer.SetNewInputs(new InputPackage() { pckp = true });
        }


        void DebugTerrain()
        {
            if (tileDebugLabel == null)
            {
                tileDebugLabel = new FLabel("font", "-")
                {
                    color = new Color(0.4f, 0.6f, 0.8f),
                    alignment = FLabelAlignment.Left
                };
                Futile.stage.AddChild(tileDebugLabel);
            }

            if (BrainPlugin.debugTerrainAndSlopes)
            {
                IntVector2 tPos = room.GetTilePosition(
                    new Vector2((int)Input.mousePosition.x, (int)Input.mousePosition.y) + room.game.cameras[0].pos);

                tileDebugLabel.x = Input.mousePosition.x;
                tileDebugLabel.y = Input.mousePosition.y;

                tileDebugLabel.text = $"{tPos}\n" +
                    $"terrain:{room.GetTile(tPos).Terrain}\n" +
                    $"slope:{room.IdentifySlope(tPos)}";
            }
            else
            {
                tileDebugLabel.text = "";
            }
        }


        public SlugcatAI AI;
        DebugNode destNode;
        DebugNode currentNode;
        FLabel tileDebugLabel;

        InputPackage lastInput;
        public bool wantToSleep;
        int stationaryCounter;
        WorldCoordinate lastPreferedStart;
        bool leftShelterThisCycle;
        
        public bool Safe
        {
            get
            {
                return AI.threatTracker.Utility() < 0.7f &&
                    (AI.rainTracker.Utility() < 0.8f || room.abstractRoom.shelter);
            }
        }

        int preferedChunkIndex;
        int NotPreferedChunkIndex => preferedChunkIndex == 0 ? 1 : 0;

    }
}
