using UnityEngine;
using RWCustom;

namespace Silvermist
{
    public class DebugObj : PhysicalObject, IDrawable
    {
        public bool updatePoints;
        public int segments = 16, pointer = -1;
        public float angle;
        public Vector2[] bezierPoints, mainPoints;
        public FLabel angleText;

        public DebugObj(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, Vector2.zero, 1, 0.05f) };
            bodyChunkConnections = new BodyChunkConnection[0];
            gravity = 0;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 0;
            bounce = 0.1f;
            buoyancy = 0.9f;
            bezierPoints = new Vector2[] { new Vector2(15f, 35f), new Vector2(45f, 45f), new Vector2(75f, 0f) };
            updatePoints = true;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (Input.GetKeyDown("/") && ++pointer > 2)
                pointer = -1;
            if (pointer != -1)
            {
                switch (Input.inputString)
                {
                    case "l":
                        bezierPoints[pointer].x += 2f;
                        break;
                    case "i":
                        bezierPoints[pointer].y += 2f;
                        break;
                    case "j":
                        bezierPoints[pointer].x += -2f;
                        break;
                    case "k":
                        bezierPoints[pointer].y += -2f;
                        break;
                }
                updatePoints = true;
            }
            if (updatePoints)
            {
                mainPoints = FCustom.BezierCurve(segments, bezierPoints[0], bezierPoints[1], bezierPoints[2]);
                updatePoints = false;
            }

            if (Input.GetKey("]")) angle += 2f;
            else if (Input.GetKey("[")) angle -= 2f;
            if (angle > 360f) angle -= 360f;
            else if (angle < 0f) angle += 360f;
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos.Tile));
        }

        public override void Destroy()
        {
            angleText.RemoveFromContainer();
            base.Destroy();
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[5];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(segments, false, true);
            for (int i = 1; i < 5; i++)
                sLeaser.sprites[i] = new FSprite("Circle20") { scale = 0.25f };
            angleText = new FLabel(Custom.GetFont(), $"ang: {angle}");
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            Vector2 pivot = Vector2.zero;
            float rt = Mathf.Cos(angle * Mathf.PI / 180f);
            for (int i = 0; i < segments; i++)
            {
                Vector2 w = Custom.PerpendicularVector(mainPoints[i] - pivot).normalized;
                Vector2 v = new Vector2(rt * mainPoints[i].x, mainPoints[i].y);
                mesh.MoveVertice(4 * i, pos + v + w);
                mesh.MoveVertice(4 * i + 1, pos + v - w);
                mesh.MoveVertice(4 * i + 2, pos + pivot + w);
                mesh.MoveVertice(4 * i + 3, pos + pivot - w);
                pivot = mainPoints[i];
                pivot.x *= rt;
            }
            for (int i = 0; i < 3; i++)
            {
                sLeaser.sprites[i + 1].x = pos.x + rt * bezierPoints[i].x;
                sLeaser.sprites[i + 1].y = pos.y + bezierPoints[i].y;
            }
            sLeaser.sprites[4].SetPosition(pos);
            angleText.x = pos.x + bezierPoints[2].x + 10f;
            angleText.y = Mathf.Min(pos.y - 10f, pos.y - 10f + bezierPoints[2].y);
            angleText.text = $"ang: {angle}";
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
                mesh.verticeColors[i] = Color.Lerp(Color.green, palette.blackColor, 0.35f);
            for (int i = 1; i < 5; i++)
                sLeaser.sprites[i].color = Color.red;
            if (pointer != -1)
                sLeaser.sprites[1 + pointer].color = Color.blue;
            angleText.color = Color.Lerp(Color.red, palette.blackColor, 0.35f);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = newContatiner ?? rCam.ReturnFContainer("Items");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
            angleText.RemoveFromContainer();
            rCam.ReturnFContainer("HUD").AddChild(angleText);
        }
    }
}
