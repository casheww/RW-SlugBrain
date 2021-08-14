using System;
using RWCustom;
using UnityEngine;

namespace SlugBrain
{
    class DebugNode : CosmeticSprite
    {
        public DebugNode(Color baseColor)
        {
            color = baseColor;
        }

        public void UpdatePosition(Room room, IntVector2 pos)
        {
            if (this.room != room && room != null)
            {
                RemoveFromRoom();
                room.AddObject(this);
            }

            tileCoords = pos;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("pixel", true)
            {
                scaleX = 5f,
                scaleY = 5f
            };
            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[0].isVisible = true;
            sLeaser.sprites[0].SetPosition(room.MiddleOfTile(tileCoords) - camPos);
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, null);
            sLeaser.sprites[0].RemoveFromContainer();
            rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[0]);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = color;
        }


        IntVector2 tileCoords;
        Color color;

    }
}
