using RWCustom;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    class SuperSlugcat : Player
    {
        public SuperSlugcat(AbstractCreature absCreature, World world) : base(absCreature, world) { }

        public override void Update(bool eu)
        {
            if (Consious) Act();

            base.Update(eu);
        }

        void Act()
        {
            AI.Update();
            FollowPath();
        }

        void FollowPath()
        {
            MovementConnection movement =
                (AI.pathFinder as StandardPather).FollowPath(room.GetWorldCoordinate(mainBodyChunk.pos), true);
            if (movement != null)
            {
                BrainPlugin.Log(movement, true);
                Move(movement);
            }
        }

        void Move(MovementConnection movement)
        {
            Vector2 dir = Custom.DirVec(movement.StartTile.ToVector2(), movement.DestTile.ToVector2());

            InputPackage input = new InputPackage(
                false,
                dir.x <= -1 ? -1 : (dir.x >= 1 ? 1 : 0),
                dir.y <= -1 ? -1 : (dir.y >= 1 ? 1 : 0),
                false,
                false, false, false, false
            );
            BrainPlugin.InputSpoofer.SetNewInputs(input);
        }


        public SlugcatAI AI;
    }
}
