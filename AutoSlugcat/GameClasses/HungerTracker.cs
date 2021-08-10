using UnityEngine;

namespace SlugBrain.GameClasses
{
    class HungerTracker : AIModule
    {
        public HungerTracker(ArtificialIntelligence AI) : base(AI) { }

        public override float Utility()
        {
            if (AI.creature.realizedCreature == null ||
                !(AI.creature.realizedCreature is Player player)) return 0f;

            if (player.CurrentFood < player.slugcatStats.foodToHibernate)
            {
                return Mathf.Clamp01(0.9f * AI.rainTracker.Utility());
            }

            float foodScore = 1 - (player.CurrentFood / player.MaxFoodInStomach);
            return 0.5f * foodScore;
        }

    }
}
