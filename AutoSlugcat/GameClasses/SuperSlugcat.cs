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
            tryingToGrabFood = false;

            GrabRange = bodyChunks[0].rad / 20f + 2f;
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

            if (AI.rainTracker.Utility() > 0.7f)
            {
                wantToSleep = true;
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
                // eat anything already in hands
                foreach (Grasp g in grasps)
                {
                    if (g != null && g.grabbed is IPlayerEdible _e && Safe)
                    {
                        BrainPlugin.Log($"eating {_e}");
                        GrabOrEat();
                        return;
                    }
                }

                // grab nearby food
                if (AI.treatTracker.CheckFoodProximity(coord, out TreatTracker.FoodRepresentation fRep)
                    && !tryingToGrabFood)
                {
                    BrainPlugin.Log($"grabbing {fRep.RealizedObject}");
                    GrabOrEat();
                    tryingToGrabFood = true;
                    return;
                }
                else if (tryingToGrabFood)
                {
                    BrainPlugin.InputSpoofer.ClearInputExceptBuffer();
                    tryingToGrabFood = false;
                }
            }

            FollowPath();
        }

        void FollowPath()
        {
            // prefered and backup start positions are used because one chunk may not be able to reach a valid path while the other can
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
            LastMovement = movement;

            if (destNode == null) destNode = new DebugNode(Color.white);
            destNode.UpdatePosition(room, movement.DestTile);

            if (currentNode == null) currentNode = new DebugNode(new Color(0.1f, 0.6f, 0.2f));
            currentNode.UpdatePosition(room, movement.StartTile);

            Vector2 dir = Custom.DirVec(movement.StartTile.ToVector2(), movement.DestTile.ToVector2());
            //Vector2 destDir = Custom.DirVec(movement.StartTile.ToVector2(), AI.Destination.Tile.ToVector2());

            int x;
            int y;
            bool holdJmp = false;

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
            else
            {
                bool onBeam = CheckBeamStatus(movement.StartTile, out bool vBeam, out bool hBeam);

                if (onBeam && vBeam && Mathf.Abs(dir.x) > 0)
                {
                    BrainPlugin.Log("jumping off vertical beam");
                    x = Math.Sign(dir.x);
                    y = 0;
                    holdJmp = true;
                }
                else
                {
                    x = Math.Sign(dir.x);
                    y = holdJmp ? 1 : Math.Sign(dir.y);
                }
            }
            
            InputPackage input = new InputPackage(
                false,
                x, y,
                false,
                false, false, false, false
            );

            BrainPlugin.InputSpoofer.PushNewInput(input);
            lastInput = input;

            if (movement.type == MovementConnection.MovementType.ReachUp ||
                movement.type == MovementConnection.MovementType.DoubleReachUp ||
                movement.type == MovementConnection.MovementType.ReachOverGap ||
                unstick ||
                holdJmp)
            {
                BrainPlugin.InputSpoofer.Jump(20);
            }
        }

        bool CheckBeamStatus(IntVector2 start, out bool vBeam, out bool hBeam)
        {
            Room.Tile startTile = room.GetTile(start);
            hBeam = startTile.horizontalBeam;


            vBeam = false;
            for (int y = -2; y < 2; y++)
            {
                Room.Tile tile = room.GetTile(start + new IntVector2(0, y));
                if (tile.verticalBeam)
                {
                    vBeam = true;
                    break;
                }
            }

            return vBeam || hBeam;
        }

        void Hibernate()
        {
            if (!room.shelterDoor.IsClosing)
            {
                int x = abstractCreature.pos.x;
                if (x == 25)
                {
                    BrainPlugin.InputSpoofer.PushNewInput(new InputPackage());
                }
                else FollowPath();
            }
            else BrainPlugin.InputSpoofer.PushNewInput(new InputPackage());
        }

        void GrabOrEat()
        {
            BrainPlugin.InputSpoofer.PushNewInput(new InputPackage() { pckp = true });
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
                Vector2 mPos = new Vector2((int)Input.mousePosition.x, (int)Input.mousePosition.y);
                IntVector2 tPos = room.GetTilePosition(mPos + room.game.cameras[0].pos);

                tileDebugLabel.x = Input.mousePosition.x;
                tileDebugLabel.y = Input.mousePosition.y;

                tileDebugLabel.text = $"{mPos}\n" +
                    $"{tPos}\n" +
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

        bool tryingToGrabFood;
        
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

        public MovementConnection LastMovement { get; private set; }

        public float GrabRange { get; private set; }

    }
}
