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
            AI = new SlugcatAI(player.abstractCreature, world);
            pather = new SlugcatPather(AI, world, player.abstractCreature);
        }

        Player player;
        SlugcatAI AI;
        SlugcatPather pather;

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
            Plugin.Log($"target room : {NextRoomName}", true);
            foreach (ShortcutData shortcut in room.shortcuts)
            {
                IntVector2 exit;
                if (shortcut.DestTile == null)
                {
                    exit = shortcut.StartTile;
                }
                else exit = shortcut.DestTile;

                AbstractRoom potentialRoom = room.WhichRoomDoesThisExitLeadTo(exit);
                if (potentialRoom is null) continue;

                Plugin.Log(potentialRoom.name);

                if (potentialRoom.name == NextRoomName)
                {
                    Plugin.Log($"shortcut to target room identified : {exit}", true);
                    pather.Reset(room);

                    IntVector2 start = room.GetTilePosition(player.mainBodyChunk.pos);
                    IntVector2 end = shortcut.StartTile;

                    if (debug) DebugRoomAccess(room, start, end);

                    IntVector2[] path = A_Star(room, start, end);

                    if (path == null) return;

                    Plugin.Log($"final path length: {path.Length}");
                    PathThroughRoomObtained = true;
                }
            }
        }

        IntVector2[] A_Star(Room room, IntVector2 start, IntVector2 end)
        {
            if (!room.aimap.TileAccessibleToCreature(end, slugTemplate)) return null;
            
            Vector2 dir = Custom.DirVec(start.ToVector2(), end.ToVector2());
            Plugin.Log($"mapping path from {start} to {end} (net direction {dir})");
            
            List<IntVector2> path = new List<IntVector2>() { start };

            bool success;
            int i = 0;
            do
            {
                IntVector2 potential = GetNeighbourInDirection(path[i], dir);

                bool accessable = room.aimap.TileAccessibleToCreature(potential, slugTemplate);
                if (accessable)
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
                    foreach (IntVector2 neighbour in GetAllNeighbours(path[i], dir))
                    {
                        if (room.aimap.TileAccessibleToCreature(neighbour, slugTemplate) &&
                                (path.Count > 1 && neighbour != path[i-1]))
                        {
                            // hm yes, recursion is a good idea...
                            IntVector2[] p = A_Star(room, neighbour, end);
                            Plugin.Log($"path segment with length {p.Length} was found");
                            if (shortestPath == null)
                            {
                                Plugin.Log(shortestPath.Length);
                                Plugin.Log(p.Length);
                                if (p.Length > shortestPath.Length)
                                {
                                    shortestPath = p;
                                }
                            }
                        }
                    }
                    // check accessable ground tiles within jump range

                    if (shortestPath == null)
                    {
                        Plugin.Log("issue with aimap");
                        return null;
                    }
                    else
                    {
                        path.AddRange(shortestPath);
                        Plugin.Log($"path segment with length {shortestPath.Length} was added to the path");
                    }
                }

                success = path[path.Count - 1] == end;
            } while (!success);

            return path.ToArray();
        }

        public static IntVector2 GetNeighbourInDirection(IntVector2 start, Vector2 dir)
        {
            return start + new IntVector2(Mathf.CeilToInt(dir.x), Mathf.CeilToInt(dir.y));
        }
        
        public static IntVector2[] GetAllNeighbours(IntVector2 start, Vector2 dir)
        {
            IntVector2[] neighbours = new IntVector2[8];
            for (int i = 0; i < 8; i++)
            {
                neighbours[i] = start + Custom.eightDirections[i];
            }
            return neighbours;
        }

        public void Move()
        {
            /*
            if (movementConnections.Count < 1) return;

            WorldCoordinate exit;
            if (movementConnections[0].destinationCoord == null)
            {
                exit = movementConnections[0].startCoord;
            }
            else exit = movementConnections[0].destinationCoord;
            Plugin.Log($"e {exit}");

            Plugin.Log($"start : {movementConnections[0].startCoord}");
            Plugin.Log($"dest  : {movementConnections[0].destinationCoord}\n");
            */
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

    }
}
