using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    /// <summary>
    /// AIModule for tracking treats (food edibles)
    /// </summary>
    class TreatTracker : AIModule       // not to be confused with the threat trackers :)))
    {
        public TreatTracker(ArtificialIntelligence AI, int maxFoodCount, float persistance, float discourageDist)
            : base(AI)
        {
            foods = new List<FoodRepresentation>();
            this.maxFoodCount = maxFoodCount;
            this.persistance = persistance;

            this.discourageDist = Mathf.Clamp(discourageDist, 20f, 500f);
        }

        public override float Utility()
        {
            if (AI.creature.realizedCreature == null ||
                !(AI.creature.realizedCreature is Player player)) return 0f;

            if (player.CurrentFood < player.slugcatStats.foodToHibernate)
            {
                // could use rain tracker utility to mod this, but for now...
                return 0.9f;
            }

            float foodScore = 1 - (player.CurrentFood / player.MaxFoodInStomach);
            return Mathf.Clamp(foodScore, 0f, 0.55f);
        }

        public override void NewRoom(Room room)
        {
            base.NewRoom(room);
            Refresh();
        }

        public override void Update()
        {
            base.Update();

            if (refreshTimer >= 40)
            {
                Refresh();
                refreshTimer = 0;
            }
            else refreshTimer++;

        }
        int refreshTimer = 0;

        public void Refresh()
        {
            // remove undesirable foods
            List<FoodRepresentation> toRemove = new List<FoodRepresentation>();
            foreach (FoodRepresentation fRep in foods)
            {
                if (fRep.RealizedObject == null || fRep.Attractiveness <= 0)
                    toRemove.Add(fRep);
            }
            foreach (FoodRepresentation fRep in toRemove) RemoveFood(fRep);

            foreach (Room room in AI.creature.world.activeRooms)
            {
                foreach (List<PhysicalObject> layer in room.physicalObjects)
                {
                    foreach (PhysicalObject obj in layer)
                    {
                        if (obj is IPlayerEdible)
                        {
                            AddFood(obj.abstractPhysicalObject);
                        }
                    }
                }
            }

            BrainPlugin.Log($"treat tracker can see {foods.Count} edible items");
        }

        public void AddFood(AbstractPhysicalObject obj)
        {
            // don't add duplicates
            foreach (FoodRepresentation fRep in foods)
            {
                if (fRep.abstractObject == obj) return;
            }

            FoodRepresentation newFood = new FoodRepresentation(this, obj);
            if (newFood.Attractiveness <= 0) return;    // don't add undesirables

            foods.Add(newFood);

            // remove least desirable object if too many are stored
            if (foods.Count > maxFoodCount)
            {
                RemoveFood(LeastAttractiveFood);
            }
        }

        public void RemoveFood(FoodRepresentation fRep)
        {
            fRep.CleanDebugNode();
            foods.Remove(fRep);
        }
        public void RemoveFood(AbstractPhysicalObject obj)
        {
            for (int i = 0; i < foods.Count; i++)
            {
                if (foods[i].abstractObject == obj)
                {
                    foods[i].CleanDebugNode();
                    foods.RemoveAt(i);
                    return;
                }
            }
        }

        public FoodRepresentation LeastAttractiveFood
        {
            get
            {
                FoodRepresentation leastAttractive = null;
                float minAttraction = float.PositiveInfinity;

                for (int i = 0; i < foods.Count; i++)
                {
                    float n = foods[i].Attractiveness;
                    if (n < minAttraction)
                    {
                        leastAttractive = foods[i];
                        minAttraction = n;
                    }
                }

                return leastAttractive;
            }
        }

        public FoodRepresentation MostAttractiveFood
        {
            get
            {
                FoodRepresentation mostAttractive = null;
                float maxAttraction = float.NegativeInfinity;

                for (int i = 0; i < foods.Count; i++)
                {
                    float n = foods[i].Attractiveness;
                    if (foods[i] == lastMostAttractiveFood) n *= persistance;

                    if (n > maxAttraction)
                    {
                        mostAttractive = foods[i];
                        maxAttraction = n;
                    }
                }

                lastMostAttractiveFood = mostAttractive;
                return mostAttractive;
            }
        }

        FoodRepresentation lastMostAttractiveFood;

        public WorldCoordinate GetMostAttractiveFoodDestination(out FoodRepresentation fRep)
        {
            if (MostAttractiveFood != null)
            {
                fRep = MostAttractiveFood;
                return MostAttractiveFood.abstractObject.pos;
            }
            else
            {
                fRep = null;
                return new WorldCoordinate(-1, -1, -1, -1);
            }
        }

        /// <summary>
        /// Checks whether the player is close enough to food to grab it.
        /// </summary>
        public bool CheckFoodProximity(WorldCoordinate playerCoords, float playerChunkRad, out FoodRepresentation fRep)
        {
            WorldCoordinate edibleCoords = GetMostAttractiveFoodDestination(out fRep);

            if (fRep != null && fRep.RealizedObject != null)
            {
                BrainPlugin.Log($"player: {playerCoords}\t  food: {fRep.RealizedObject} {edibleCoords}  {fRep.Attractiveness}");
                BrainPlugin.Log($"dist to food {Custom.WorldCoordFloatDist(playerCoords, edibleCoords)}");

                if (Custom.DistLess(playerCoords, edibleCoords, playerChunkRad / 20f + 2f))
                {
                    return true;
                }
            }

            return false;
        }

        public void DrawDebugNodes()
        {
            foreach (FoodRepresentation fRep in foods)
            {
                fRep.DrawDebugNode();
            }
        }


        List<FoodRepresentation> foods;
        readonly int maxFoodCount;
        readonly float persistance;
        readonly float discourageDist;

        AImap AImap => AI.creature.realizedCreature.room?.aimap;

        
        public class FoodRepresentation
        {
            public FoodRepresentation(TreatTracker tracker, AbstractPhysicalObject obj)
            {
                this.tracker = tracker;
                abstractObject = obj;

                debugNode = new DebugNode(new Color(0.3f, 0.3f, 0.7f));
            }

            public float Attractiveness
            {
                get
                {
                    if (RealizedObject == null) return -1f;

                    float dist = (tracker.AI as SlugcatAI).EstimateTileDistance(tracker.AI.creature.pos, abstractObject.pos);
                    float score = Mathf.Lerp(1f, 0f, dist / tracker.discourageDist);

                    if (RealizedObject is DangleFruit || RealizedObject is EggBugEgg)
                    {
                        score *= 1.3f;
                    }

                    return score;
                }
            }

            public void DrawDebugNode()
            {
                if (debugNode != null && RealizedObject != null &&
                    RealizedObject.room == tracker.AI.creature.realizedCreature.room)
                    debugNode.UpdatePosition(RealizedObject.room, abstractObject.pos.Tile);
                else
                    CleanDebugNode();
            }

            public void CleanDebugNode()
            {
                if (debugNode == null) return;
                debugNode.Destroy();
                debugNode = null;
            }


            readonly TreatTracker tracker;
            public readonly AbstractPhysicalObject abstractObject;
            public PhysicalObject RealizedObject => abstractObject?.realizedObject;

            DebugNode debugNode;

        }

    }
}
