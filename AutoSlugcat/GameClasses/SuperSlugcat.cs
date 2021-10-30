using System;
using RWCustom;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    public class SuperSlugcat : Player
    {
        public SuperSlugcat(AbstractCreature absCreature, World world) : base(absCreature, world)
        {
            wantToSleep = false;
            _stationaryCounter = 0;
            _preferedChunkIndex = 0;
            _leftShelterThisCycle = false;
            _tryingToGrabFood = false;
            _movementExecutionCounter = 0;

            GrabRange = bodyChunks[0].rad / 20f + 2f;

            SlugTemplate = Template;
            
            PushPriorityText();
        }
        
        private static void PushPriorityText()
        {
            BrainPlugin.TextManager.Write("SLUGBRAIN", "enabled", Color.red);
            BrainPlugin.TextManager.Write("pos   ", "");
            BrainPlugin.TextManager.Write("dest  ", "");
            BrainPlugin.TextManager.Write("mvmnt ", "");
            BrainPlugin.TextManager.Write("input ", "");
        }

        public override void Update(bool eu)
        {
            if (Consious) Act();

            DebugTerrain();

            base.Update(eu);
        }

        private void Act()
        {
            ai.Update();

            if (!_leftShelterThisCycle && !room.abstractRoom.shelter) _leftShelterThisCycle = true;

            // want to sleep if rain is coming
            if (ai.rainTracker.Utility() > 0.7f)
            {
                wantToSleep = true;
            }

            // hibernation or desire to leave shelter
            if (room.abstractRoom.shelter)
            {
                if (wantToSleep)
                {
                    BrainPlugin.InputSpoofer.PushInputPackages(Hibernate());
                    return;
                }

                BrainPlugin.Log("trying to leave shelter");
                ai.SetDestination(new WorldCoordinate(room.abstractRoom.index, -1, -1, 0));
            }

            if (CurrentFood < MaxFoodInStomach)
            {
                // eat anything already in hands
                foreach (Grasp g in grasps)
                {
                    if (g != null && g.grabbed is IPlayerEdible _e && Safe)
                    {
                        BrainPlugin.Log($"eating {_e}");
                        BrainPlugin.InputSpoofer.PushInputPackages(GrabOrEat());
                        return;
                    }
                }

                // grab nearby food
                if (ai.treatTracker.CheckFoodProximity(coord, out TreatTracker.FoodRepresentation fRep)
                    && !_tryingToGrabFood)
                {
                    BrainPlugin.Log($"grabbing {fRep.RealizedObject}");
                    BrainPlugin.InputSpoofer.PushInputPackages(GrabOrEat());
                    _tryingToGrabFood = true;
                    return;
                }
                if (_tryingToGrabFood)
                {
                    BrainPlugin.InputSpoofer.PushInputPackages(new [] {new InputPackage()});
                    _tryingToGrabFood = false;
                }
            }

            FollowPath();
        }

        private void FollowPath()
        {
            // prefered and backup start positions are used because one chunk may not be able to reach a valid path while the other can
            WorldCoordinate preferedStart = room.GetWorldCoordinate(bodyChunks[_preferedChunkIndex].pos);
            WorldCoordinate backupStart = room.GetWorldCoordinate(bodyChunks[NotPreferedChunkIndex].pos);

            if (_lastPreferedStart == preferedStart) _stationaryCounter++;
            else _stationaryCounter = 0;

            bool gettingUnstuck = false;

            if (_stationaryCounter > 40 && !wantToSleep)
            {
                _preferedChunkIndex = NotPreferedChunkIndex;
                _stationaryCounter = 0;
                BrainPlugin.Log($"swapped prefered start chunk index to {_preferedChunkIndex}");
                gettingUnstuck = true;
            }

            // prevent accepting new movements from the pathfinder when still executing the last movement
            if (_movementExecutionCounter > 0)
                _movementExecutionCounter--;
            else
                FollowPath(preferedStart, backupStart, gettingUnstuck);

            _lastPreferedStart = preferedStart;
        }

        private void FollowPath(WorldCoordinate preferedStart, WorldCoordinate backupStart, bool gettingUnstuck)
        {
            MovementConnection movement;

            movement = ai.aStarPathFinder.FollowPath(preferedStart);

            if (movement == null) movement = ai.aStarPathFinder.FollowPath(backupStart);
            
            if (movement == null) movement =
                    new MovementConnection(MovementConnection.MovementType.Standard, preferedStart, preferedStart, 0);

            Move(movement, gettingUnstuck);
        }

        private void Move(MovementConnection movement, bool unstick = false)
        {
            BrainPlugin.TextManager.Write("mvmnt ", $"{movement.type} : {movement.StartTile} -> {movement.DestTile}");
            
            LastMovement = movement;

            BrainPlugin.NodeManager.Draw("moveDest",
                DebugColors.GetColor(DebugColors.Subject.MoveTo),
                room, movement.DestTile);

            BrainPlugin.NodeManager.Draw("currentTile",
                DebugColors.GetColor(DebugColors.Subject.Position),
                room, movement.StartTile);

            Vector2 dir = Custom.DirVec(movement.StartTile.ToVector2(), movement.DestTile.ToVector2());
            //Vector2 destDir = Custom.DirVec(movement.StartTile.ToVector2(), AI.Destination.Tile.ToVector2());

            int x;
            int y;

            Room.Tile startTile = room.GetTile(movement.StartTile);
            Room.Tile destTile = room.GetTile(movement.DestTile);

            if (movement.type == EnumExt_SlugMovements.StandingJump)
            {
                x = dir.x < -0.6f ? -1 : (dir.x > 0.6f ? 1 : 0);
                y = 1;
                BrainPlugin.Log("standing jump", warning: true);
                BrainPlugin.InputSpoofer.PushInputPackages(DoStandingJump(x, y));
                return;
            }
            
            if (destTile.Terrain == Room.Tile.TerrainType.Slope)
            {
                Room.SlopeDirection slope = room.IdentifySlope(movement.DestTile);
                BrainPlugin.Log($"slope {slope}");

                if (PrevInput.x != 0) x = PrevInput.x;
                else x = dir.x < -0.5f ? -1 : (dir.x > 0.5f ? 1 : 0);

                if (slope == Room.SlopeDirection.UpLeft || slope == Room.SlopeDirection.UpRight) y = 1;
                else y = 0;
                
                BrainPlugin.InputSpoofer.PushInputPackages(Walk(x, y));
                return;
            }
            
            CheckBeamStatus(movement.StartTile, out bool vBeam, out bool hBeam);
            bool onBeam = bodyMode == BodyModeIndex.ClimbingOnBeam;

            if (onBeam && vBeam && Mathf.Abs(dir.x) > 0)
            {
                BrainPlugin.Log("jumping off vertical beam");
                x = dir.x < -0.6f ? -1 : (dir.x > 0.6f ? 1 : 0);
                y = 0;
                BrainPlugin.InputSpoofer.PushInputPackages(DoStandingJump(x, y));
                return;
            }
            
            x = Math.Sign(dir.x);
            y = Math.Sign(dir.y);
            
            if (movement.type == MovementConnection.MovementType.ReachUp ||
                movement.type == MovementConnection.MovementType.DoubleReachUp ||
                movement.type == MovementConnection.MovementType.ReachOverGap ||
                unstick)
            {
                BrainPlugin.InputSpoofer.PushInputPackages(DoStandingJump(x, y));
                return;
            }
            
            BrainPlugin.InputSpoofer.PushInputPackages(Walk(x, y));
        }

        private bool CheckBeamStatus(IntVector2 start, out bool vBeam, out bool hBeam)
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

        private InputPackage[] Hibernate()
        {
            if (!room.shelterDoor.IsClosing)
            {
                int x = abstractCreature.pos.x;
                if (x == 25)
                {
                    return new [] { new InputPackage() };
                }
                FollowPath();
            }
            else return new [] { new InputPackage() };

            return new InputPackage[] { };
        }

        private InputPackage[] GrabOrEat()
        {
            return new [] { new InputPackage { pckp = true } };
        }

        private InputPackage[] DoStandingJump(int x, int y)
        {
            if (!JumpCalculator.CheckJumpStartIsValid(room,
                abstractCreature.pos.Tile - new IntVector2(0, NotPreferedChunkIndex),       // chunk 0 is raised
                EnumExt_SlugMovements.StandingJump))
            {
                BrainPlugin.Log("can't jump from here", warning: true);
                return new [] {new InputPackage()};
            }

            int jumpFrames = 7;
            _movementExecutionCounter = jumpFrames + 3;
            
            InputPackage[] inputs = new InputPackage[jumpFrames];
            for (int i = 0; i < jumpFrames; i++)
                inputs[i] = new InputPackage { x = x, y = y, jmp = true };

            return inputs;
        }

        private InputPackage[] Walk(int x, int y)
        {
            return new [] { new InputPackage { x = x, y = y} };
        }
        
        
        private void DebugTerrain()
        {
            if (_tileDebugLabel == null)
            {
                _tileDebugLabel = new FLabel("font", "-")
                {
                    color = new Color(0.4f, 0.6f, 0.8f),
                    alignment = FLabelAlignment.Left
                };
                Futile.stage.AddChild(_tileDebugLabel);
            }

            if (BrainPlugin.debugTerrainAndSlopes)
            {
                Vector2 mPos = new Vector2((int)Input.mousePosition.x, (int)Input.mousePosition.y);
                IntVector2 tPos = room.GetTilePosition(mPos + room.game.cameras[0].pos);

                _tileDebugLabel.x = Input.mousePosition.x;
                _tileDebugLabel.y = Input.mousePosition.y;

                _tileDebugLabel.text = $"{mPos}\n" +
                    $"{tPos}\n" +
                    $"terrain:{room.GetTile(tPos).Terrain}\n" +
                    $"slope:{room.IdentifySlope(tPos)}\n" +
                    $"aitile:{ai.pathFinder.AITileAtWorldCoordinate(new WorldCoordinate(room.abstractRoom.index, tPos.x, tPos.y, -1)).acc}";
            }
            else 
            {
                _tileDebugLabel.text = "";
            }
        }


        public SlugcatAI ai;
        private FLabel _tileDebugLabel;

        public bool wantToSleep;
        private int _stationaryCounter;
        private WorldCoordinate _lastPreferedStart;
        private bool _leftShelterThisCycle;
        private int _movementExecutionCounter;

        private bool _tryingToGrabFood;
        
        public bool Safe
        {
            get
            {
                return ai.threatTracker.Utility() < 0.7f &&
                    (ai.rainTracker.Utility() < 0.8f || room.abstractRoom.shelter);
            }
        }

        private static InputPackage PrevInput => BrainPlugin.InputSpoofer.PreviousInput;

        private int _preferedChunkIndex;
        private int NotPreferedChunkIndex => _preferedChunkIndex == 0 ? 1 : 0;

        public MovementConnection LastMovement { get; private set; }

        public float GrabRange { get; private set; }

        public static CreatureTemplate SlugTemplate { get; private set; }

    }
}
