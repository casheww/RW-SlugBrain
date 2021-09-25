using RWCustom;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    /// <summary>
    /// AIModule for tracking treats (food edibles)
    /// </summary>
    class TreatTracker : SlugcatAIModule        // not to be confused with the threat trackers :)))
    {
        public TreatTracker(ArtificialIntelligence ai, int maxFoodCount, float persistance, float discourageDist)
            : base(ai)
        {
            _foods = new List<FoodRepresentation>();
            this._maxFoodCount = maxFoodCount;
            this._persistance = persistance;

            this._discourageDist = Mathf.Clamp(discourageDist, 20f, 500f);

            _foodColor = DebugColors.GetColor(DebugColors.Subject.Food);
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

            if (_refreshTimer >= 40)
            {
                _refreshTimer = 0;
                Refresh();
            }
            else _refreshTimer++;

        }
        private int _refreshTimer = 0;

        public void Refresh()
        {
            // remove undesirable foods
            List<FoodRepresentation> toRemove = new List<FoodRepresentation>();
            foreach (FoodRepresentation fRep in _foods)
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
                "treats", $"refreshed - {FoodsInRoom(AI.creature.Room, false).Count} in room ({_foods.Count} total)",
                _foodColor, 20);
        }

        public void AddFood(AbstractPhysicalObject obj)
        {
            // don't add duplicates
            foreach (FoodRepresentation fRep in _foods)
            {
                if (fRep.abstractObject == obj) return;
            }

            FoodRepresentation newFood = new FoodRepresentation(this, obj);
            if (newFood.Attractiveness <= 0) return;    // don't add undesirables

            _foods.Add(newFood);

            // remove least desirable object if too many are stored
            if (_foods.Count > _maxFoodCount)
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
            for (int i = 0; i < _foods.Count; i++)
            {
                if (_foods[i].abstractObject == obj)
                {
                    RemoveFood(_foods[i]);
                    return;
                }
            }
        }
        public void RemoveFood(FoodRepresentation fRep)
        {
            fRep.CleanDebugNode();
            _foods.Remove(fRep);
        }

        public FoodRepresentation LeastAttractiveFood
        {
            get
            {
                FoodRepresentation leastAttractive = null;
                float minAttraction = float.PositiveInfinity;

                for (int i = 0; i < _foods.Count; i++)
                {
                    float n = _foods[i].Attractiveness;
                    if (n < minAttraction)
                    {
                        leastAttractive = _foods[i];
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

                for (int i = 0; i < _foods.Count; i++)
                {
                    float n = _foods[i].Attractiveness;
                    if (_foods[i] == _lastMostAttractiveFood) n *= _persistance;

                    if (n > maxAttraction)
                    {
                        mostAttractive = _foods[i];
                        maxAttraction = n;
                    }
                }

                BrainPlugin.TextManager.Write("best food", mostAttractive, _foodColor);
                _lastMostAttractiveFood = mostAttractive;
                return mostAttractive;
            }
        }

        private FoodRepresentation _lastMostAttractiveFood;

        public WorldCoordinate GetMostAttractiveFoodDestination(out FoodRepresentation fRep)
        {
            if (MostAttractiveFood != null)
            {
                fRep = MostAttractiveFood;
                return MostAttractiveFood.abstractObject.pos;
            }
            else
            {
                BrainPlugin.TextManager.Write("treats", "no attractive food :(", _foodColor, 30);
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

        public List<FoodRepresentation> FoodsInRoom(AbstractRoom room, bool shouldBeAccessible)
        {
            List<FoodRepresentation> foodsInRoom = _foods.Where(f => f.abstractObject.Room == room).ToList();

            if (shouldBeAccessible)
            {
                foodsInRoom = foodsInRoom.Where(f => f.Attractiveness >= 0).ToList();
            }

            return foodsInRoom;
        }

        public void DrawDebugNodes()
        {
            foreach (FoodRepresentation fRep in _foods)
            {
                fRep.DrawDebugNode();
            }
        }

        public override void UpdateRoomRepresentation(RoomRepresentation rRep)
        {
            rRep.food = FoodsInRoom(rRep.room, true).Count;
        }


        private readonly List<FoodRepresentation> _foods;
        private readonly int _maxFoodCount;
        private readonly float _persistance;
        private readonly float _discourageDist;
        private readonly Color _foodColor;

        
        public class FoodRepresentation
        {
            public FoodRepresentation(TreatTracker tracker, AbstractPhysicalObject obj)
            {
                _tracker = tracker;
                abstractObject = obj;

                _fSprite = new FSprite("pixel")
                {
                    color = DebugColors.GetColor(DebugColors.Subject.Food),
                    scale = 5f
                };
            }

            public float Attractiveness
            {
                get
                {
                    if (RealizedObject == null || RealizedObject.room == null) return -1f;

                    if (!RealizedObject.room.readyForAI ||
                        !(_tracker.AI as SlugcatAI).jumpModule.CheckJumpAndGrabbable(RealizedObject.room.aimap, abstractObject.pos.Tile))
                    {
                        return -1f;
                    }

                    float dist = (_tracker.AI as SlugcatAI).EstimateTileDistance(_tracker.AI.creature.pos, abstractObject.pos);
                    float score = Mathf.Lerp(1f, 0f, dist / _tracker._discourageDist);

                    if (RealizedObject is DangleFruit || RealizedObject is EggBugEgg)
                    {
                        score *= 1.2f;
                    }

                    return score;
                }
            }

            public void DrawDebugNode()
            {
                if (RealizedObject?.room != null &&
                    RealizedObject.room == _tracker.AI.creature.realizedCreature.room)
                {
                    if (_sprite == null)
                    {
                        _sprite = new DebugSprite(Vector2.one, _fSprite, RealizedObject.room);
                        RealizedObject.room.AddObject(_sprite);
                    }
                    else
                        _sprite.pos = new Vector2(abstractObject.pos.Tile.x * 20 + 10, abstractObject.pos.Tile.y * 20 + 10);
                }
                    
                else
                    CleanDebugNode();
            }

            public void CleanDebugNode()
            {
                _sprite?.RemoveFromRoom();
            }

            public override string ToString()
            {
                string realStatus = RealizedObject != null ? "" : " (unrealised)";
                return $"{abstractObject.type}{realStatus}  {abstractObject.pos}";
            }


            private readonly TreatTracker _tracker;
            public readonly AbstractPhysicalObject abstractObject;
            public PhysicalObject RealizedObject => abstractObject?.realizedObject;

            private DebugSprite _sprite;
            private FSprite _fSprite;

        }

    }
}
