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

            foodColor = DebugColors.GetColor(DebugColors.Subject.Food);
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
            return Mathf.Clamp(foodScore, 0f, 0.5f);
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
                refreshTimer = 0;
                Refresh();
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

            BrainPlugin.TextManager.Write(
                "treats", $"refreshed - {FoodsInRoom(AI.creature.Room, false).Count} in room ({foods.Count} total)",
                foodColor, 20);
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

        public void RegisterFoodEaten(AbstractPhysicalObject obj)
        {
            RemoveFood(obj);
            AI.pathFinder.Reset(AI.creature.realizedCreature.room);
        }
        public void RemoveFood(AbstractPhysicalObject obj)
        {
            for (int i = 0; i < foods.Count; i++)
            {
                if (foods[i].abstractObject == obj)
                {
                    RemoveFood(foods[i]);
                    return;
                }
            }
        }
        public void RemoveFood(FoodRepresentation fRep)
        {
            fRep.CleanDebugNode();
            foods.Remove(fRep);
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

                BrainPlugin.TextManager.Write("best food", mostAttractive, foodColor);
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
                BrainPlugin.TextManager.Write("treats", "no attractive food :(", foodColor, 30);
                fRep = null;
                return new WorldCoordinate(-1, -1, -1, -1);
            }
        }

        /// <summary>
        /// Checks whether the player is close enough to food to grab it.
        /// </summary>
        public bool CheckFoodProximity(WorldCoordinate playerCoords, out FoodRepresentation fRep)
        {
            WorldCoordinate edibleCoords = GetMostAttractiveFoodDestination(out fRep);

            if (fRep != null && fRep.RealizedObject != null)
            {
                if (Custom.DistLess(playerCoords, edibleCoords, (AI.creature.realizedCreature as SuperSlugcat).GrabRange))
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


        readonly List<FoodRepresentation> foods;
        readonly int maxFoodCount;
        readonly float persistance;
        readonly float discourageDist;
        readonly Color foodColor;

        public List<FoodRepresentation> FoodsInRoom(AbstractRoom room, bool shouldBeAccessible)
        {
            List<FoodRepresentation> foodsInRoom = foods.Where(f => f.abstractObject.Room == room).ToList();

            if (shouldBeAccessible)
            {
                foodsInRoom = foodsInRoom.Where(f => f.Attractiveness >= 0).ToList();
            }

            return foodsInRoom;
        }
            

        public class FoodRepresentation
        {
            public FoodRepresentation(TreatTracker tracker, AbstractPhysicalObject obj)
            {
                this.tracker = tracker;
                abstractObject = obj;

                debugNode = new DebugNode(tracker.foodColor);
            }

            public float Attractiveness
            {
                get
                {
                    if (RealizedObject == null || RealizedObject.room == null) return -1f;

                    if (!RealizedObject.room.readyForAI ||
                        !(tracker.AI as SlugcatAI).jumpModule.CheckJumpAndGrabbable(RealizedObject.room.aimap, abstractObject.pos.Tile))
                    {
                        return -1f;
                    }

                    float dist = (tracker.AI as SlugcatAI).EstimateTileDistance(tracker.AI.creature.pos, abstractObject.pos);
                    float score = Mathf.Lerp(1f, 0f, dist / tracker.discourageDist);

                    if (RealizedObject is DangleFruit || RealizedObject is EggBugEgg)
                    {
                        score *= 1.2f;
                    }

                    return score;
                }
            }

            public void DrawDebugNode()
            {
                if (debugNode != null && RealizedObject != null && RealizedObject.room != null &&
                    RealizedObject.room == tracker.AI.creature.realizedCreature.room)
                    debugNode.SetPosition(RealizedObject.room, abstractObject.pos.Tile);
                else
                    CleanDebugNode();
            }

            public void CleanDebugNode()
            {
                if (debugNode == null) return;
                debugNode.Destroy();
                debugNode = null;
            }

            public override string ToString()
            {
                return $"{RealizedObject.GetType()}  {abstractObject.pos.Tile}";
            }


            readonly TreatTracker tracker;
            public readonly AbstractPhysicalObject abstractObject;
            public PhysicalObject RealizedObject => abstractObject?.realizedObject;

            DebugNode debugNode;

        }

    }
}
