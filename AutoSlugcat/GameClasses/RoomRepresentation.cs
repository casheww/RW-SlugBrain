using UnityEngine;

namespace SlugBrain.GameClasses
{
    class RoomRepresentation
    {
        public RoomRepresentation(AbstractRoom room)
        {
            this.room = room;
            food = 0;
            threats = 0;
        }

        public readonly AbstractRoom room;
        public int food;
        public int threats;

        public float DesireToGoBack => Mathf.Lerp(0f, 1f, (food + threats == 0) ? 0 : food / (float)(food + threats));

    }
}
