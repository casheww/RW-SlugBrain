using UnityEngine;

namespace SlugBrain.GameClasses
{
    public class RoomRepresentation
    {
        public RoomRepresentation(AbstractRoom room)
        {
            this.room = room;
            food = 0;
            threats = 0;
        }

        public float Attractiveness(bool hungry)
        {
            float desire;

            if (hungry)
                desire = (food + threats == 0) ? 0 : (food / (float)(food + threats));
            else
                desire = Mathf.Clamp01(threats / (float)threatLimit);

            return desire;
        }

        public readonly AbstractRoom room;
        public int food;
        public int threats;
        public float distToShelter;

        private const int threatLimit = 2;

    }
}
