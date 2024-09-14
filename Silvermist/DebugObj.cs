using RWCustom;
using UnityEngine;

namespace Silvermist
{
    public class DebugObj : PhysicalObject, IDrawable
    {
        public bool updatePoints;
        public int segments = 10/*16*/, pointer = -1;
        public float rotation;
        public Quaternion Q, Qr;
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
            Q = new Quaternion(0f, 0f, 0f, 0f);
            Qr = new Quaternion(0f, 0f, 0f, 0f);
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
                mainPoints = FCustom.BezierCurve(segments, bezierPoints);
                updatePoints = false;
            }

            if (Input.GetKey("t")) rotation += (rotation > Mathf.PI * 2f) ? -Mathf.PI * 2f : Mathf.PI / 120f;
            else if (Input.GetKey("r")) rotation -= (rotation < 0) ? -Mathf.PI * 2f : Mathf.PI / 120f;

            if (Input.GetKey("]")) Q.x += 0.1f;
            else if (Input.GetKey("[")) Q.x -= 0.1f;
            if (Input.GetKey("'")) Q.y += 0.1f;
            else if (Input.GetKey(";")) Q.y -= 0.1f;
            if (Input.GetKey(".")) Q.z += 0.1f;
            else if (Input.GetKey(",")) Q.z -= 0.1f;
            if (Input.GetKey("j")) Q.w += 1f;
            Q.Normalize();

            Qr = new Quaternion(Mathf.Sin(rotation / 2f) * Q.x, Mathf.Sin(rotation / 2f) * Q.y, Mathf.Sin(rotation / 2f) * Q.z, Mathf.Cos(rotation / 2f)).normalized;
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
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(segments, false, true);
            sLeaser.sprites[1] = TriangleMesh.MakeLongMesh(2 * segments, false, true);
            angleText = new FLabel(Custom.GetFont(), "");
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            Vector3[] points = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                Vector2 v = (Vector2)(Qr * ((Vector3)mainPoints[i]).ToQuaternion() * Qr.Сonjugate()).ToVector3();
                Vector2 prev = (i == 0) ? Vector2.zero : (Vector2)points[i - 1];
                points[i] = v;
                Vector2 w = Custom.PerpendicularVector(v - prev).normalized;
                mesh.MoveVertice(4 * i, pos + v + w);
                mesh.MoveVertice(4 * i + 1, pos + v - w);
                mesh.MoveVertice(4 * i + 2, pos + prev + w);
                mesh.MoveVertice(4 * i + 3, pos + prev - w);
            }
            angleText.x = pos.x;
            angleText.y = pos.y - 50f;
            angleText.text = $"Q_v:{Q}\nQ_rt:{Qr}\nAngle: {(int)(rotation * 180f / Mathf.PI)}";

            mesh = sLeaser.sprites[1] as TriangleMesh;
            Vector3[,] leafVertices = new Vector3[2 * segments, 4];
            Vector3 prevCenter = Vector3.zero;
            Vector3 prevPeref = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                Vector3 v = new Vector3(0f, 0f, 10f) * (-0.00390625f * Mathf.Pow((i + 1) / (float)segments * 32 - 16, 2) + 1);
                Quaternion Qv = ((Vector3)mainPoints[i] - prevCenter).ToQuaternion();
                Qv.Normalize();
                Qv = new Quaternion(Qv.x * Mathf.Sin(15f * Mathf.PI / 180f), Qv.y * Mathf.Sin(15f * Mathf.PI / 180f), Qv.z * Mathf.Sin(15f * Mathf.PI / 180f), Mathf.Cos(15f * Mathf.PI / 180f)).normalized;
                v = (Qv * v.ToQuaternion() * Qv.Сonjugate()).ToVector3();
                v += (Vector3)mainPoints[i];

                leafVertices[2 * i, 0] = new Vector3(mainPoints[i].x, mainPoints[i].y, 0f);
                leafVertices[2 * i, 1] = new Vector3(v.x, v.y, v.z);
                leafVertices[2 * i, 2] = new Vector3(prevCenter.x, prevCenter.y, 0f);
                leafVertices[2 * i, 3] = new Vector3(prevPeref.x, prevPeref.y, prevPeref.z);
                leafVertices[2 * i + 1, 0] = new Vector3(mainPoints[i].x, mainPoints[i].y, 0f);
                leafVertices[2 * i + 1, 1] = new Vector3(v.x, v.y, -v.z);
                leafVertices[2 * i + 1, 2] = new Vector3(prevCenter.x, prevCenter.y, 0f);
                leafVertices[2 * i + 1, 3] = new Vector3(prevPeref.x, prevPeref.y, -prevPeref.z);
                prevCenter = (Vector3)mainPoints[i];
                prevPeref = v;
            }
            for (int i = 0; i < leafVertices.Length; i++)
                leafVertices[i / 4, i % 4] = (Qr * leafVertices[i / 4, i % 4].ToQuaternion() * Qr.Сonjugate()).ToVector3();
            leafVertices = FCustom.SortVertices(leafVertices);
            for (int i = 0; i < mesh.vertices.Length; i++)
                mesh.MoveVertice(i, pos + (Vector2)leafVertices[i / 4, i % 4]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
            {
                Color c1 = Color.Lerp(Color.green, palette.blackColor, 0.9f - 0.8f * i / mesh.verticeColors.Length);
                Color c2 = Color.Lerp(Color.green, palette.blackColor, 0.9f - 0.8f * (i + 1) / mesh.verticeColors.Length);
                mesh.verticeColors[i] = (i % 4 < 2) ? c2 : c1;
            }
            mesh = sLeaser.sprites[1] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
            {
                Color c1 = Color.Lerp(Color.green, palette.blackColor, 0.7f - 0.4f * i / mesh.verticeColors.Length);
                Color c2 = Color.Lerp(Color.green, palette.blackColor, 0.7f - 0.4f * (i + 1) / mesh.verticeColors.Length);
                mesh.verticeColors[i] = (i % 4 < 2) ? c2 : c1;
            }
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
