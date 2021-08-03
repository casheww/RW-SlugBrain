using RWCustom;
using UnityEngine;

namespace SlugBrain
{
    struct MovementPackage
    {
        public MovementPackage(IntVector2 tile, Vector2 dir, Type type)
        {
            this.tile = tile;
            this.dir = dir;
            this.type = type;
        }
        
        public IntVector2 tile;
        public Vector2 dir;
        public Type type;

        public enum Type
        {
            Walk, PoleClimbV, PoleClimbH, Jump      // more to come
        }
    }
}
