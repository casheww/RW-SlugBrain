using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;

namespace AutoSlugcat
{
    public class PathManager
    {
        public PathManager(string queuePath, bool debugRooms)
        {
            Plugin.Log($"queue path : {queuePath}");
            LoadPath(queuePath);

            debug = debugRooms;
            PathThroughRoomObtained = false;
        }
        bool debug;

        void LoadPath(string queuePath)
        {
            TargetPos = 1;

            Regex notNotRoomRegex = new Regex(@"^[^//\s]+");

            string raw = File.ReadAllText(queuePath);
            foreach (string line in raw.Split('\n'))
            {
                string roomName = line.Trim();
                if (notNotRoomRegex.IsMatch(roomName))
                {
                    Plugin.Log($"queuing {roomName}");
                    queue.Add(roomName);
                }
            }
            Plugin.Log("");
        }

        public void SetupAI(Player player, World world)
        {
            this.player = player;
            //AI = new SlugcatAI(player.abstractCreature, world);
            //pather = new SlugcatPather(AI, world, player.abstractCreature);
        }

        Player player;

        public string Advance()
        {
            TargetPos++;
            PathThroughRoomObtained = false;

            if (FinishedQueue)
            {
                Plugin.Log("\nyou have reached your destination");
            }

            return CurrentRoomName;
        }

        public void DeterminePathThroughRoom(Room room)
        {
            IntVector2 start = room.GetTilePosition(player.mainBodyChunk.pos);
            bool endFound = GetRoomExit(room, out IntVector2 end);

            if (!endFound)
            {
                Plugin.Log($"\nERROR : {room.abstractRoom.name} has no connection to {NextRoomName}\n");
                return;
            }
            if (debug) DebugRoomAccess(room, start, end);

            Dictionary<IntVector2, MovementType> path = new Dictionary<IntVector2, MovementType>();

            IntVector2[] nodes = A_Star(room, start, end);

            if (nodes == null) return;

            Plugin.Log($"final path length: {path.Count}");
            PathThroughRoomObtained = true;
        }

        bool GetRoomExit(Room room, out IntVector2 end)
        {
            Plugin.Log($"target room : {NextRoomName}", true);
            foreach (ShortcutData shortcut in room.shortcuts)
            {
                IntVector2 potentialExit;
                if (shortcut.DestTile == null)
                {
                    potentialExit = shortcut.StartTile;
                }
                else potentialExit = shortcut.DestTile;

                AbstractRoom potentialRoom = room.WhichRoomDoesThisExitLeadTo(potentialExit);
                if (potentialRoom is null) continue;

                if (potentialRoom.name == NextRoomName)
                {
                    Plugin.Log($"shortcut to target room identified : {potentialExit}", true);
                    end = potentialExit;
                    return true;
                }
            }

            end = new IntVector2(0, 0);
            return false;
        }

        IntVector2[] A_Star(Room room, IntVector2 start, IntVector2 end)
        {
            return A_Star(room, start, end, Vector2.zero);
        }

        IntVector2[] A_Star(Room room, IntVector2 start, IntVector2 end, Vector2 overallRoomDir)
        {
            if (!room.aimap.TileAccessibleToCreature(start, slugTemplate)) return null;
            
            Vector2 dir = Custom.DirVec(start.ToVector2(), end.ToVector2());
            Plugin.Log($"mapping path from {start} to {end} (net direction {dir})");
            
            List<IntVector2> path = new List<IntVector2>() { start };

            bool success;
            int i = 0;
            do
            {
                if (overallRoomDir != Vector2.zero)
                {
                    // a previous call of this method hit an obstacle. overallRoomDir is the entrance-exit dir
                    if (TryTurnToExit(room, path[i], end, dir, out IntVector2[] pathSection))
                    {
                        path.AddRange(pathSection);
                        Plugin.Log($"after a collision, a turn towards the exit could be made (len:{pathSection.Length})");
                    }
                }
                else
                {
                    IntVector2 potential = GetNeighbourInDirection(path[i], dir);
                    bool accessible = room.aimap.TileAccessibleToCreature(potential, slugTemplate);

                    if (accessible)
                    {
                        Plugin.Log($"best option was accessable : {potential}");
                        dir = Custom.DirVec(potential.ToVector2(), end.ToVector2());
                        path.Add(potential);
                        i++;
                    }
                    else
                    {
                        Plugin.Log("best option was not accessable");
                        IntVector2[] shortestPath = null;

                        foreach (KeyValuePair<IntVector2, IntVector2> neighbour in GetAllNeighbours(path[i]))
                        {
                            if (room.aimap.TileAccessibleToCreature(neighbour.Value, slugTemplate) &&
                                path.Count > 1 && neighbour.Value != path[i - 1])         // disallow backtracking
                            {
                                IntVector2 neighborsCoherentNeighbour = neighbour.Value + neighbour.Key;
                                IntVector2[] p = A_Star(room, neighbour.Value, neighborsCoherentNeighbour);
                                Plugin.Log($"path segment with length {p.Length} was found");

                                if (shortestPath != null || shortestPath.Length > p.Length)
                                {
                                    shortestPath = p;
                                }
                            }
                        }

                        if (shortestPath == null)
                        {
                            Plugin.Log("issue with aimap? path through room for given exists not found");
                            return null;
                        }
                        else
                        {
                            path.AddRange(shortestPath);
                            Plugin.Log($"path segment with length {shortestPath.Length} was added to the path");
                        }
                    }
                }

                success = Math.Abs(Custom.ManhattanDistance(path[path.Count - 1], end)) < 3;
            } while (!success);

            return path.ToArray();
        }

        bool TryTurnToExit(Room room, IntVector2 current, IntVector2 end, Vector2 dir, out IntVector2[] path)
        {
            IntVector2 potential = GetNeighbourInDirection(current, dir);
            if (room.aimap.TileAccessibleToCreature(potential, slugTemplate))
            {
                path = A_Star(room, current, end);
                return true;
            }
            path = null;
            return false;
        }

        public static IntVector2 GetNeighbourInDirection(IntVector2 start, Vector2 dir)
        {
            return start + new IntVector2(Mathf.CeilToInt(dir.x), Mathf.CeilToInt(dir.y));
        }
        
        public static Dictionary<IntVector2, IntVector2> GetAllNeighbours(IntVector2 start)
        {
            Dictionary<IntVector2, IntVector2> neighbours = new Dictionary<IntVector2, IntVector2>();
            for (int i = 0; i < 8; i++)
            {
                neighbours.Add(Custom.eightDirections[i], start + Custom.eightDirections[i]);
            }
            return neighbours;
        }

        public void Move()
        {
        }

        static void DebugRoomAccess(Room room, IntVector2 start, IntVector2 end)
        {
            string output = "";

            for (int y = room.Height; y >= 0; y--)
            {
                for (int x = 0; x < room.Width; x++)
                {
                    if (x == start.x && y == start.y)
                    {
                        output += "S";
                    }
                    else if (x == end.x && y == end.y)
                    {
                        output += "E";
                    }
                    else if (room.aimap.TileAccessibleToCreature(x, y, slugTemplate))
                    {
                        output += ".";
                    }
                    else
                    {
                        output += "X";
                    }
                }
                output += "\n";
            }

            File.WriteAllText("./Mods/AutoSlugcatStuff/debug.txt", output);
        }

        public bool PathThroughRoomObtained { get; private set; }

        List<string> queue = new List<string>();

        public int TargetPos { get; private set; }

        public string CurrentRoomName { get => player.room.abstractRoom.name; }
        public string NextRoomName { get =>
                (0 <= TargetPos && TargetPos < queue.Count) ? queue[TargetPos] : "INVALID" ; }

        public bool FinishedQueue { get => NextRoomName == "INVALID"; }

        static CreatureTemplate slugTemplate = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.PinkLizard);


        public enum MovementType
        {
            Walk,
            Jump
        }

    }
}
