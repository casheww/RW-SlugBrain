using System.Collections.Generic;
using UnityEngine;

namespace SlugBrain.GameClasses
{
    class TreatTracker : AIModule
    {
        public TreatTracker(ArtificialIntelligence AI) : base(AI)
        {
            edibles = new List<AbstractPhysicalObject>();
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

        public void Refresh()
        {
            // remove objects that are no longer realised
            List<AbstractPhysicalObject> ediblesToRemove = new List<AbstractPhysicalObject>();
            foreach (AbstractPhysicalObject o in edibles)
            {
                if (o.realizedObject == null) ediblesToRemove.Add(o);
            }
            foreach (AbstractPhysicalObject o in ediblesToRemove)
            {
                edibles.Remove(o);
            }

            // check all loaded rooms for edibles
            foreach (Room r in AI.creature.world.activeRooms)
            {
                foreach (List<PhysicalObject> layer in r.physicalObjects)
                {
                    foreach (PhysicalObject obj in layer)
                    {
                        if (obj is IPlayerEdible)
                        {
                            edibles.Add(obj.abstractPhysicalObject);
                        }
                    }
                }
            }

            BrainPlugin.Log($"treat tracker can see {edibles.Count} edible items");
        }

        public WorldCoordinate GetNearestEdibleLocation(out PhysicalObject edible)
        {
            edible = null;
            int nearest = -1;
            float minDist = float.PositiveInfinity;

            for (int i = 0; i < edibles.Count; i++)
            {
                if (AI.pathFinder.CoordinateReachable(edibles[i].pos))
                {
                    // distance to other rooms is complicated so that's a TODO

                    float dist = RWCustom.Custom.WorldCoordFloatDist(AI.creature.pos, edibles[i].pos);
                    if (edibles[i] == target) dist -= targetPersistance;

                    if (dist > -1 && dist < minDist)
                    {
                        nearest = i;
                        minDist = dist;
                    }
                }
            }

            if (nearest > -1)
            {
                target = edibles[nearest];
                edible = target.realizedObject;
                return edibles[nearest].pos;
            }
            else return AI.creature.pos;        // no edibles found -- TODO add random at some point
        }

        float targetPersistance = 40f;
        AbstractPhysicalObject target;
        public readonly List<AbstractPhysicalObject> edibles;

    }
}
