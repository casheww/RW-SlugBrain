namespace SlugBrain.GameClasses
{
    public static partial class JumpCalculator
    {
        public struct JumpType
        {
            public JumpType(MovementConnection.MovementType type, float[] yAccels, float xVel)
            {
                this.type = type;
                yAccelerations = yAccels;
                xVelocity = xVel;
            }
        
            public readonly MovementConnection.MovementType type;
            public readonly float[] yAccelerations;
            public readonly float xVelocity;
        }
    }
}
