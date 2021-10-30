using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    public static partial class JumpCalculator
    {

        public static JumpData[] GetJumpableTiles(Room room, IntVector2 start)
        {
            List<JumpData> tiles = new List<JumpData>();

            /*
             * iterate through jump types
             *      check if jump type is possible from current tile
             *          if so, then cast the jump path with that jump type's starting velocities
             */

            Vector2 pixelStart = ConvertTileToPixels(start);

            foreach (JumpType jType in _validJumps)
            {
                float maxX = jType.xVelocity;
                
                if (CheckJumpStartIsValid(room, start, jType.type))
                {
                    tiles.AddRange(CastJumpPath(room, pixelStart, jType, maxX, 0));
                    tiles.AddRange(CastJumpPath(room, pixelStart, jType, -maxX, 0));
                }
            }
            
            return tiles.ToArray();
        }

        private static List<JumpData> CastJumpPath(Room room, Vector2 pixelStart, JumpType jType, float velX, float velY)
        {
            List<JumpData> path = new List<JumpData>();

            Vector2 current = pixelStart;
            int frame = 0;
            //float gravity = room.gravity * 0.9f;     // 90% because of funny game physics --- temporarily ignoring room.gravity
            float[] accel = jType.yAccelerations;

            bool pathBlocked = false;
            do
            {
                frame++;
                // use registered acceleration value if possible, but repeat last value if not registered
                velY += (frame < accel.Length ? accel[frame] : accel[accel.Length - 1]) * updatesPerCastStep;

                current.x += velX * updatesPerCastStep;
                current.y += velY * updatesPerCastStep;

                IntVector2 currentTile = ConvertPixelsToTile(current);
                
                if (CheckCanIMoveOntoTile(room, currentTile))
                {
                    path.Add(new JumpData(ConvertPixelsToTile(pixelStart), currentTile, jType.type));
                }
                else pathBlocked = true;

            } while (!pathBlocked);

            return path;
        }
        
        private static List<JumpData> CastJumpPath(Room room, IntVector2 start, JumpType jType) =>
            CastJumpPath(room, ConvertTileToPixels(start), jType, jType.xVelocity, 0);

        public static bool CheckJumpability(Room room, IntVector2 start, IntVector2 end,
            out MovementConnection.MovementType movementType)
        {
            foreach (JumpType jType in _validJumps)
                foreach (JumpData jData in CastJumpPath(room, start, jType))
                    if (jData.to == end)
                    {
                        movementType = jType.type;
                        return true;
                    }

            movementType = MovementConnection.MovementType.OffScreenUnallowed;
            return false;
        }

        public static bool CheckJumpStartIsValid(Room room, IntVector2 start, MovementConnection.MovementType jump)
        {
            if (!room.IsPositionInsideBoundries(start)) return false;

            AItile aiTile = room.aimap.getClampedAItile(start.x, start.y);
            Room.Tile ground = room.GetTile(start - new IntVector2(0, 1));
            
            if (jump == EnumExt_SlugMovements.StandingJump)
                return aiTile.acc == AItile.Accessibility.Floor ||
                       aiTile.acc == AItile.Accessibility.Climb ||
                       ground.Terrain == Room.Tile.TerrainType.Slope;
            
            // TODO add jump types here

            return false;
        }
        
        private static bool CheckCanIMoveOntoTile(Room room, IntVector2 pos)
        {
            return room.GetTile(pos).Terrain != Room.Tile.TerrainType.Solid;
        }

        public static bool CheckIsBasicJumpable(Room room, IntVector2 end)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = 0; y >= -5; y--)
                    if (room.GetTile(end + new IntVector2(x, y)).Terrain == Room.Tile.TerrainType.Floor)
                        return true;

            return false;
        }
        
        public static Vector2 ConvertTileToPixels(IntVector2 tile) =>
            tile.ToVector2() * 20f;

        public static IntVector2 ConvertPixelsToTile(Vector2 pixels) =>
            IntVector2.FromVector2(pixels / 20f);


        private const int updatesPerCastStep = 1;
        
        /// <summary>
        /// Given jump velocities are maximums, so this gives the step size used between casts the
        /// multiple casts between negative of the maximum and the maximum velocity.
        /// </summary>
        private const float velocityResolution = 1f;

        
        // =====

        private static readonly JumpType[] _validJumps = new []
        {
            new JumpType(EnumExt_SlugMovements.StandingJump,
                new [] { 0f, 5.50f, 3.77f, 1.36f, 0.66f, -0.01f, -0.69f, -1.36f, -1.35f },
                4.0f)
        };

    }
}
