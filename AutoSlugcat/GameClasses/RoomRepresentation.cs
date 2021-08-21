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

        public float DesireToGoBack(bool hungry)
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

        const int threatLimit = 2;

    }
}
