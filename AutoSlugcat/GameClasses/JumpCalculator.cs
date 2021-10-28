using RWCustom;
using System.Collections.Generic;
using System.Linq;
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

            foreach (JumpType jump in _validJumpTypes)
            {
                float maxX = _jumpXVelocities[(int)jump];
                
                if (CheckJumpStartIsValid(room, start, room.aimap.getClampedAItile(start.x, start.y), jump))
                {
                    tiles.AddRange(CastJumpPath(room, pixelStart, jump, maxX, 0));
                    tiles.AddRange(CastJumpPath(room, pixelStart, jump, -maxX, 0));
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

            float[] accel = _jumpYAccelerations[(int) jType];

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
                    path.Add(new JumpData(ConvertPixelsToTile(pixelStart), currentTile, jType));
                }
                else pathBlocked = true;

            } while (!pathBlocked);

            return path;
        }
        
        private static List<JumpData> CastJumpPath(Room room, IntVector2 start, JumpType jType) =>
            CastJumpPath(room, ConvertTileToPixels(start), jType,
                _jumpXVelocities[(int)jType], 0);

        public static bool CheckJumpability(Room room, IntVector2 start, IntVector2 end,
            out MovementConnection.MovementType movementType)
        {
            foreach (JumpType jType in _validJumpTypes)
                foreach (JumpData jData in CastJumpPath(room, start, jType))
                    if (jData.to == end)
                    {
                        movementType = GetMovementType(jType);
                        return true;
                    }

            movementType = MovementConnection.MovementType.OffScreenUnallowed;
            return false;
        }

        private static bool CheckJumpStartIsValid(Room room, IntVector2 start, AItile aiTile, JumpType jump)
        {
            if (!room.IsPositionInsideBoundries(start)) return false;
            
            switch (jump)
            {
                case JumpType.StandardJump:
                    return aiTile.acc == AItile.Accessibility.Floor || aiTile.acc == AItile.Accessibility.Climb;
            }

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
        
        public enum JumpType
        {
            StandardJump,
            //Pounce,
            //SlidePounce
        }

        private static MovementConnection.MovementType GetMovementType(JumpType jump)
        {
            switch (jump)
            {
                case JumpType.StandardJump:
                    return EnumExt_SlugBrainJumps.StandardJump;
                default:
                    return MovementConnection.MovementType.Standard;
            }
        }

        private static readonly JumpType[] _validJumpTypes = new JumpType[]
        {
            JumpType.StandardJump
        };

        private static readonly float[][] _jumpYAccelerations = new float[][]
        {
            new [] { 0f, 5.50f, 3.77f, 1.36f, 0.66f, -0.01f, -0.69f, -1.36f, -1.35f }       // StandardJump
        };

        private static readonly float[] _jumpXVelocities = new float[]
        {
            4.0f        // StandardJump
        };

    }
}
