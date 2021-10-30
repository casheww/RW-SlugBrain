using RWCustom;

namespace SlugBrain.GameClasses
{
    public static partial class JumpCalculator
    {
        public readonly struct JumpData
        {
            public JumpData(IntVector2 from, IntVector2 to, MovementConnection.MovementType type)
            {
                this.from = from;
                this.to = to;
                this.type = type;
            }
            
            public readonly IntVector2 from;
            public readonly IntVector2 to;
            public readonly MovementConnection.MovementType type;
        }
    }
}