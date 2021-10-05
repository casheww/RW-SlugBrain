using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    class JumpModule
    {
        public JumpModule(ArtificialIntelligence ai)
        {
            _ai = ai;
        }

        public static JumpType CheckJumpability(Room room, IntVector2 start, IntVector2 dest)
        {
            JumpType easiestJumpType = JumpType.Impossible;

            //float jumpFac = 1f;         // minimum possible - Mathf.Lerp(1, 1.15, adrenaline)
            //float runSpeedFac = 1f;     // survivor default

            for (int i = 0; i < _validJumpTypes.Length; i++)
            {
                JumpType jType = _validJumpTypes[i];

                float ux = _jumpVelocities[jType].x;
                float uy = _jumpVelocities[jType].y;

                if (CheckTileIsOnJumpPath(room, start, dest, ux, uy))
                    easiestJumpType = jType;
                else
                    break;
            }

            return easiestJumpType;
        }

        public static IntVector2[] GetJumpableTiles(Room room, IntVector2 start, AItile startAItile)
        {
            List<IntVector2> tiles = new List<IntVector2>();

            /*
             * iterate through jump types
             *      check if jump type is possible from current tile
             *          if so, then cast the jump path with that jump type's starting velocities
             */

            foreach (JumpType jump in _validJumpTypes)
            {
                if (CheckJumpStartIsValid(room, start, startAItile, jump))
                {
                    float ux = _jumpVelocities[jump].x;
                    float uy = _jumpVelocities[jump].y;
                    
                    tiles.AddRange(CastJumpPath(room, start, ux, uy));
                }
            }
            
            return tiles.ToArray();
        }

        private static bool CheckTileIsOnJumpPath(Room room, IntVector2 start, IntVector2 dest, float velX, float velY)
        {
            bool canReachDest = false;

            for (int i = 0; i < Custom.eightDirectionsAndZero.Length; i++)
            {
                if (CastJumpPath(room, start, velX, velY).Contains(dest + Custom.eightDirectionsAndZero[i]))
                {
                    canReachDest = true;
                    break;
                }
            }

            return canReachDest;
        }

        private static List<IntVector2> CastJumpPath(Room room, IntVector2 start, float velX, float velY)
        {
            List<IntVector2> path = new List<IntVector2>();
            Vector2 current = start.ToVector2() * 20f;

            float gravity = room.gravity * 0.9f;     // 90% because of funny game physics

            bool pathBlocked = false;
            do
            {
                velY -= gravity * updatesPerCastStep;
                current.x += velX * updatesPerCastStep;
                current.y += velY * updatesPerCastStep;

                IntVector2 currentTile = new IntVector2(Mathf.FloorToInt(current.x / 20f), Mathf.FloorToInt(current.y / 20f));
                
                if (CheckCanIMoveOntoTile(room, currentTile) && !path.Contains(currentTile))
                {
                    path.Add(currentTile);
                }
                else
                {
                    pathBlocked = true;
                    //BrainPlugin.NodeManager.Draw($"jumpblock{currentTile}", new Color(0.4f, 0, 0), room, currentTile, 5f);
                }
                
            } while (!pathBlocked);

            return path;
        }

        public static bool CheckJumpable(AImap aiMap, IntVector2 dest, CreatureTemplate crit)
        {
            Room.Tile.TerrainType terrain = aiMap.room.GetTile(dest).Terrain;
            if (terrain == Room.Tile.TerrainType.Solid || terrain == Room.Tile.TerrainType.Slope)
                return false;

            for (int i = 1; i <= jumpHeight; i++)
            {
                if (aiMap.TileAccessibleToCreature(dest.x, dest.y - i, crit))
                    return true;
            }

            return false;
        }

        public bool CheckJumpAndGrabbable(AImap aiMap, IntVector2 dest)
        {
            for (int i = 0; i < Custom.fourDirections.Length; i++)
            {
                if (CheckJumpable(aiMap, dest + Custom.fourDirections[i], SuperSlugcat.SlugTemplate)) return true;
            }
            for (int i = 0; i < Custom.diagonals.Length; i++)
            {
                if (CheckJumpable(aiMap, dest + Custom.diagonals[i], SuperSlugcat.SlugTemplate)) return true;
            }

            return false;
        }

        private static bool CheckJumpStartIsValid(Room room, IntVector2 start, AItile aiTile, JumpType jump)
        {
            if (!room.IsPositionInsideBoundries(start)) return false;
            
            switch (jump)
            {
                case JumpType.Standard:
                    return aiTile.acc == AItile.Accessibility.Floor || aiTile.acc == AItile.Accessibility.Climb;
                
                case JumpType.Leap:
                    return aiTile.acc == AItile.Accessibility.Floor;
                
                // TODO
            }

            return false;
        }
        
        private static bool CheckCanIMoveOntoTile(Room room, IntVector2 pos)
        {
            return room.GetTile(pos).Terrain == Room.Tile.TerrainType.Air;
        }


        private readonly ArtificialIntelligence _ai;
        private IntVector2 Position => _ai.creature.pos.Tile;
        private SuperSlugcat Slugcat => _ai.creature.realizedCreature as SuperSlugcat;

        private const float jumpHeight = 2;
        private const int updatesPerCastStep = 3;


        public enum JumpType
        {
            Standard,
            Leap,
            SlidePounce,

            Impossible
        }

        private static readonly JumpType[] _validJumpTypes = new JumpType[]
        {
            JumpType.Standard,
            JumpType.Leap,
            JumpType.SlidePounce,       // TODO
        };

        private static readonly Dictionary<JumpType, IntVector2> _jumpVelocities = new Dictionary<JumpType, IntVector2>()
        {
            { JumpType.Standard, new IntVector2(4, 4) },
            { JumpType.Leap, new IntVector2(9, 3) },
            { JumpType.SlidePounce, new IntVector2(0, 0) }      // TODO
        };
    }
}
