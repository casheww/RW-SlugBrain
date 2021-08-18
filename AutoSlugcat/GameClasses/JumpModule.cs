using RWCustom;

namespace SlugBrain.GameClasses
{
    class JumpModule
    {
        public JumpModule(ArtificialIntelligence AI)
        {
            this.AI = AI;
        }

        public static bool CheckIsJumpable(AImap aimap, IntVector2 dest)
            => CheckIsJumpable(aimap, dest, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat));

        public static bool CheckIsJumpable(AImap aimap, IntVector2 dest, CreatureTemplate crit)
        {
            for (int i = 1; i <= jumpHeight; i++)
            {
                Room.Tile.TerrainType terrain = aimap.room.GetTile(dest).Terrain;
                if (aimap.TileAccessibleToCreature(dest.x, dest.y - i, crit) &&
                    terrain != Room.Tile.TerrainType.Solid && terrain != Room.Tile.TerrainType.Slope) return true;
            }

            return false;
        }

        public bool CheckJumpAndGrabbable(AImap aimap, IntVector2 dest)
        {
            for (int i = 0; i < Custom.fourDirections.Length; i++)
            {
                if (CheckIsJumpable(aimap, dest + Custom.fourDirections[i])) return true;
                if (CheckIsJumpable(aimap, dest + Custom.fourDirections[i] * 2)) return true;
            }
            for (int i = 0; i < Custom.diagonals.Length; i++)
            {
                if (CheckIsJumpable(aimap, dest + Custom.diagonals[i])) return true;
            }

            return false;
        }


        ArtificialIntelligence AI;
        IntVector2 Position => AI.creature.pos.Tile;

        const float jumpHeight = 2;

    }
}
