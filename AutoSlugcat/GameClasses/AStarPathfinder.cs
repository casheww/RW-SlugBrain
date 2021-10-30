using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    public class AStarPathfinder : SlugcatAIModule
    {
        public AStarPathfinder(ArtificialIntelligence ai, World world, AbstractCreature creature) : base(ai)
        {
            _world = world;
            _creature = creature;
            state = State.NotReady;

            _openNodes = new List<IntVector2>();
            _nodeParentDictionary = new Dictionary<IntVector2, IntVector2>();
            _costToNode_G = new Dictionary<IntVector2, float>();
        }

        public override void UpdateRoomRepresentation(RoomRepresentation rRep) { }
        
        public void SetDestination(WorldCoordinate coords)
        {
            IntVector2 tileInRoom;

            if (Room.index == coords.room)
            {
                tileInRoom = coords.Tile;
            }
            else
            {
                // if the world coordinate doesn't point to the current room, 
                //  find the exit from this room that is closest to the target room. 

                if (Room.realizedRoom.shortcuts == null)
                {
                    BrainPlugin.Log("room shortcuts not ready for pathfinding", warning: true);
                    return;
                }
                
                int exit = GetExitIndexForRoom(Room, coords.room);
                tileInRoom = Room.realizedRoom.LocalCoordinateOfNode(exit).Tile;
                BrainPlugin.Log($"finding path to {coords} via exit {exit} at {tileInRoom}");
            }

            SetDestination(tileInRoom);
        }
        
        /// <param name="room">The starting room.</param>
        /// <param name="targetRoomIndex">The index of the room to search for.</param>
        /// <returns>The index of the exit in the starting room that is closest to the target room.</returns>
        public int GetExitIndexForRoom(AbstractRoom room, int targetRoomIndex)
        {
            int targetExit = room.ExitIndex(targetRoomIndex);

            while (targetExit == -1)
            {
                for (int exit = 0; exit < room.connections.Length; exit++)
                {
                    AbstractRoom roomThroughExit = _world.GetAbstractRoom(room.connections[exit]);

                    if (GetExitIndexForRoom(roomThroughExit, targetRoomIndex) != -1)
                    {
                        targetExit = exit;
                        break;
                    }
                }
            }

            return targetExit;
        }
        
        /// <summary>
        /// Sets the pathfinder up for the A* search to the tile in the room at the given tile coords.
        /// The search is then run by <see cref="Update"/> calls
        /// as <see cref="state"/> is set to <see cref="State"/>.CalculatingPath. 
        /// </summary>
        public void SetDestination(IntVector2 destination)
        {
            if (state == State.CalculatingPath)
            {
                bool onPath = false;
                foreach (IntVector2 v in Custom.eightDirectionsAndZero)
                {
                    if (_nodeParentDictionary.ContainsKey(_creature.pos.Tile + v) ||
                        _nodeParentDictionary.ContainsKey(_creature.pos.Tile + v * 2))
                    {
                        onPath = true;
                        break;
                    }
                }
                
                if (destination == _goal && (_start.FloatDist(_creature.pos.Tile) < 5f || onPath))
                {
                    //BrainPlugin.Log($"already finding that path ({_start} to {_goal})!\n\tcancelling new call",
                    //    warning: true);
                    return;
                }
            }

            state = State.CalculatingPath;
            _start = _creature.pos.Tile;
            _goal = destination;
            
            _openNodes.Clear();
            _openNodes.Add(_start);
            
            _costToNode_G.Clear();
            _costToNode_G.Add(_start, 0f);
            
            _nodeParentDictionary.Clear();
            
            BrainPlugin.Log($"setup for pathing from {_start} to {_goal}");
        }

        public override void Update()
        {
            for (int check = 0; check < checksPerUpdate; check++)
            {
                if (state != State.CalculatingPath) return;
            
                DoPathfinding();
            }
        }

        private void DoPathfinding()
        {
            _bestNode = GetBestNode(out _);
            _openNodes.Remove(_bestNode);           // node is no longer unexplored
            
            BrainPlugin.NodeManager.Draw("bestnode", Color.red, Room.realizedRoom, _bestNode, 5f);
            BrainPlugin.TextManager.Write("bestnode", _bestNode, Color.red);

            foreach (IntVector2 dir in Custom.fourDirections)
            {
                IntVector2 neighbour = _bestNode + dir;
                
                if (!CheckIsTileLegal(Room.realizedRoom, neighbour))
                    continue;
                
                BrainPlugin.TextManager.Write("pathfinder pathing", "working", PathingColor);

                AddOpenNode(MovementConnection.MovementType.Standard, neighbour, _bestNode);
                
                // check if done
                if (neighbour.FloatDist(_goal) < 2f)
                {
                    state = State.Done;
                    FinalPath = ConstructPath(neighbour, true);
                    BrainPlugin.Log($"pathfinder : done -> {FinalPath.Length}");
                    BrainPlugin.TextManager.Write("pathfinder pathing", $"done -> {FinalPath.Length}", PathingColor);
                }
            }
            
            // add tiles that can be reached from the current best node by jumping
            foreach (var jData in JumpCalculator.GetJumpableTiles(_creature.Room.realizedRoom, _bestNode))
            {
                BrainPlugin.NodeManager.Draw($"j{jData.to}", Color.cyan, Room.realizedRoom, jData.to, 4f, 20);
                AddOpenNode(jData.type, jData.to, _bestNode);
            }
        }

        private void AddOpenNode(MovementConnection.MovementType moveType, IntVector2 node, IntVector2 parent)
        {
            // don't add nodes that are already marked as open or have already been checked
            if (_openNodes.Contains(node) ||
                _nodeParentDictionary.ContainsValue(node) ||
                _nodeParentDictionary.ContainsKey(node))
                return;

            _openNodes.Add(node);
            _nodeParentDictionary.Add(node, parent);
            _costToNode_G[node] = GetCostToNode_G(parent) + GetMovementCost(moveType, parent, node);
        }
        
        private bool CheckIsTileLegal(Room room, IntVector2 tile)
        {
            if (!room.IsPositionInsideBoundries(tile)) return false;
            
            if (room.readyForAI)
            {
                AItile.Accessibility acc = room.aimap.getAItile(tile).acc;

                if (acc == AItile.Accessibility.Floor ||
                    acc == AItile.Accessibility.Climb ||
                    acc == AItile.Accessibility.Corridor)
                    return true;
            }

            return false;
        }

        private float GetMovementCost(MovementConnection.MovementType moveType, IntVector2 from, IntVector2 to)
        {
            float dist = from.FloatDist(to);

            switch (moveType)
            {
                default:
                    if (moveType == EnumExt_SlugMovements.StandingJump)
                        return 5f;
                    else if (dist < 2f)
                        return 1f;
                    break;
            }
            
            // TODO costs for different movements (like jumps)

            return float.PositiveInfinity;  
        }

        /// <summary>
        /// Gets the best node from the open set, <see cref="_openNodes"/>.<br/>
        /// Evaluation of nodes is done by the estimated cost to <see cref="_goal"/>,
        ///     calculated by <see cref="EstimateCostToGoalFrom_H"/>.
        /// </summary>
        private IntVector2 GetBestNode(out float bestScore)
        {
            IntVector2 best = new IntVector2(0, 0);
            bestScore = float.PositiveInfinity;

            foreach (IntVector2 node in _openNodes)
            {
                float score = EstimateCostToGoalFrom_H(node);

                // H score comparison with actual displacement as a tie-breaker
                if (score < bestScore ||
                    score == bestScore && node.FloatDist(_goal) < best.FloatDist(_goal))
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>
        /// The path cost from the start to the goal through the given node. 
        /// </summary>
        private float GetOverallCost_F(IntVector2 node) =>
            GetCostToNode_G(node) + EstimateCostToGoalFrom_H(node);

        /// <summary>
        /// The path cost from the start to the given node. 
        /// </summary>
        private float GetCostToNode_G(IntVector2 node)
        {
            if (_costToNode_G.TryGetValue(node, out float cost))
                return cost;
            
            return float.PositiveInfinity;
        }

        /// <summary>
        /// The estimated path cost from the given node to the goal. 
        /// </summary>
        private float EstimateCostToGoalFrom_H(IntVector2 node) =>
            Mathf.Abs(_goal.x - node.x) + Mathf.Abs(_goal.y - node.y);

        /// <summary>
        /// Walks backwards through <see cref="_nodeParentDictionary"/> to construct the path taken by the pathfinder. 
        /// </summary>
        private IntVector2[] ConstructPath(IntVector2 to, bool draw)
        {
            List<IntVector2> path = new List<IntVector2>() { to };
            IntVector2 current = to;

            while (_nodeParentDictionary.TryGetValue(current, out IntVector2 parent))
            {
                BrainPlugin.Log($"{current} has parent {parent}");
                current = parent;
                path.Add(current);
            }
            
            path.Reverse();

            if (draw)
            {
                foreach (IntVector2 n in path)
                {
                    BrainPlugin.NodeManager.Draw(n.ToString(), PathingColor, Room.realizedRoom, n, frames: 120);
                }
            }
            
            return path.ToArray();
        }
        
        public MovementConnection FollowPath(WorldCoordinate start)
        {
            MovementConnection noMovement = new MovementConnection(MovementConnection.MovementType.Standard, start, start, 0);
            
            if (FinalPath != null && FinalPath.Length > 0)
            {
                // find the path node that's closest to slugcat
                int closestNodeIndex = 0;
                float shortestDist = float.PositiveInfinity;

                for (int i = 0; i < FinalPath.Length; i++)
                {
                    float d = FinalPath[i].FloatDist(start.Tile);
                    if (d < shortestDist)
                    {
                        closestNodeIndex = i;
                        shortestDist = d;
                    }
                }
                
                // if too far away from closest node, re-evaluate path
                if (shortestDist > 5f && state != State.CalculatingPath)
                {
                    BrainPlugin.TextManager.Write("pathfinder", $"{shortestDist} away from path. Redoing!",
                        PathingColor, 80);
                    BrainPlugin.Log($"{shortestDist} away from path. Redoing!", warning: true);
                    SetDestination(_goal);
                    return noMovement;
                }
                
                // else if not close enough to 
                
                // ... else move to next node in path
                if (closestNodeIndex + 1 < FinalPath.Length)
                {
                    IntVector2 currentTile = FinalPath[closestNodeIndex];
                    IntVector2 nextTile = FinalPath[closestNodeIndex + 1];

                    // check if tiles are adjacent
                    if (Mathf.Abs(nextTile.y - currentTile.y) + Mathf.Abs(nextTile.x - currentTile.x) <= 1)
                    {
                        return new MovementConnection(MovementConnection.MovementType.Standard,
                            start,
                            new WorldCoordinate(start.room, nextTile.x, nextTile.y, -1),
                            (int)start.Tile.FloatDist(nextTile));
                    }
                    
                    // for non-adjacent tiles, check if jumpable instead
                    bool jumpable = JumpCalculator.CheckJumpability(_creature.Room.realizedRoom, currentTile, nextTile,
                        out MovementConnection.MovementType movementType);

                    if (jumpable)
                    {
                        return new MovementConnection(movementType, start,
                            new WorldCoordinate(start.room, nextTile.x, nextTile.y, -1),
                            (int)start.Tile.FloatDist(nextTile));
                    }
                }
            }
            
            BrainPlugin.TextManager.Write("pathfinder", $"no path from {start} to {_goal}. state?{state}",
                PathingColor);
            BrainPlugin.Log($"no path from {start} to {_goal}", error: true);
            return noMovement;
        }


        public enum State
        {
            NotReady,
            CalculatingPath,
            Done
        }
        
        
        private readonly World _world;
        private readonly AbstractCreature _creature;
        private AbstractRoom Room => _creature.Room;
        public State state;
        private const int checksPerUpdate = 1;
        
        private IntVector2 _start;
        private IntVector2 _goal;
        private IntVector2 _bestNode;

        private readonly List<IntVector2> _openNodes;
        private readonly Dictionary<IntVector2, IntVector2> _nodeParentDictionary;
        private readonly Dictionary<IntVector2, float> _costToNode_G;
        
        public IntVector2[] FinalPath { get; private set; }

        private static Color PathingColor =>
            DebugColors.GetColor(DebugColors.Subject.Destination);
        
    }
}
