using System;
using UnityEngine;

namespace SlugBrain
{
    static class DebugColors
    {
        public enum Subject
        {
            Position,
            MoveTo,
            Destination,
            Food,
            Threat,
            Shelter,
        }

        public static Color GetColor(Subject subject)
        {
            int i = (int)subject;
            return (0 <= i && i < _colors.Length) ? _colors[i] : Color.white;
        }

        public static Color GetColor(AIModule module)
        {
            if (module is GameClasses.TreatTracker)
            {
                return GetColor(Subject.Food);
            }
            if (module is RainTracker)
            {
                return GetColor(Subject.Shelter);
            }
            if (module is ThreatTracker)
            {
                return GetColor(Subject.Threat);
            }

            return Color.white;
        }

        private static readonly Color[] _colors = new Color[]
        {
            Color.grey,
            new Color(0.1f, 0.6f, 0.2f),
            new Color(0.3f, 0.9f, 0.35f),
            new Color(0.4f, 0.4f, 0.9f),
            new Color(1f, 0.07f, 0.07f),
            new Color(0.86f, 0.53f, 0.82f)
        };
    }
}
