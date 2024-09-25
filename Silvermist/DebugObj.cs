using RWCustom;
using UnityEngine;

namespace Silvermist
{
    public class DebugObj : PhysicalObject, IDrawable
    {
        public float rotation, anchorX, anchorY;

        public DebugObj(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = [ new (this, 0, Vector2.zero, 1, 0.05f) ];
            bodyChunkConnections = [];
            gravity = 0;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 0;
            bounce = 0.1f;
            buoyancy = 0.9f;
            anchorX = 0.5f;
            anchorY = 0.5f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (Input.GetKey("."))
                rotation += (rotation > Mathf.PI * 2f) ? -(Mathf.PI * 2f) : Mathf.PI / 120f;
            if (Input.GetKey("]")) anchorX = Mathf.Clamp01(anchorX + 0.01f);
            else if (Input.GetKey("[")) anchorX = Mathf.Clamp01(anchorX - 0.01f);
            if (Input.GetKey("'")) anchorY = Mathf.Clamp01(anchorY + 0.01f);
            else if (Input.GetKey(";")) anchorY = Mathf.Clamp01(anchorY - 0.01f);
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos.Tile));
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("KrakenBody");
            sLeaser.sprites[1] = new FSprite("Circle20") { scale = 0.2f, color = Color.red };
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            sLeaser.sprites[0].SetPosition(pos);
            sLeaser.sprites[1].SetPosition(pos);
            sLeaser.sprites[0].SetAnchor(new Vector2(anchorX, anchorY));
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            //TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            //for (int i = 0; i < mesh.verticeColors.Length; i++)
            //    mesh.verticeColors[i] = Color.red;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Items");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
        }
    }
}
