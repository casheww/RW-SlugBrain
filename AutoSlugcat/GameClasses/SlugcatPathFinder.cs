using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SlugBrain.GameClasses
{
    class SlugcatPathFinder : StandardPather
    {
        public SlugcatPathFinder(ArtificialIntelligence AI, World world, AbstractCreature creature)
            : base (AI, world, creature)
        {
            stepsPerFrame = 20;
        }
    }
}
