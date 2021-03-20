using System;
using RWCustom;

namespace AutoSlugcat
{
    class PlayerHooks
    {
        public static void Apply()
        {
            On.Player.ctor += Player_ctor;
            On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
            On.Player.Update += Player_Update;
        }
        public static void UnApply()
        {
            On.Player.ctor -= Player_ctor;
            On.Player.SpitOutOfShortCut -= Player_SpitOutOfShortCut;
            On.Player.Update -= Player_Update;
        }

        static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature creature, World world)
        {
            orig(self, creature, world);

            Plugin.Manager.SetPlayer(self, world);
            Plugin.Manager.DeterminePathThroughRoom(self.room);
        }

        static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self,
                IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig(self, pos, newRoom, spitOutAllSticks);

            // not sure why this is called twice for what appears to be one pipe exit... so here is a botch
            if (!secondSpitOut)
            {
                secondSpitOut = true;
                return;
            }
            else secondSpitOut = false;

            Plugin.Log($"entered {newRoom.abstractRoom.name}");

            if (newRoom.abstractRoom.name == Plugin.Manager.NextRoomName)
            {
                Plugin.Manager.Advance();
            }

            Plugin.Manager.DeterminePathThroughRoom(newRoom);
        }
        static bool secondSpitOut = false;

        static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (Plugin.Manager.PathThroughRoomObtained)
            {
                Plugin.Manager.Move();
            }
            else
            {
                Plugin.Manager.DeterminePathThroughRoom(self.room);
            }
        }
    }
}
