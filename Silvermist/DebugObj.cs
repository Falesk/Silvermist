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
        public Vector3[,] leafVertices;
        public FLabel angleText;

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
            bezierPoints = [ new (15f, 35f), new (45f, 45f), new (75f, 0f) ];
            updatePoints = true;
            Q = new Quaternion(0f, 0f, 0f, 0f);
            Qr = new Quaternion(0f, 0f, 0f, 0f);
            leafVertices = new Vector3[2 * segments, 4];
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            //if (Input.GetKeyDown("/") && ++pointer > 2)
            //    pointer = -1;
            //if (pointer != -1)
            //{
            //    switch (Input.inputString)
            //    {
            //        case "l":
            //            bezierPoints[pointer].x += 2f;
            //            break;
            //        case "i":
            //            bezierPoints[pointer].y += 2f;
            //            break;
            //        case "j":
            //            bezierPoints[pointer].x += -2f;
            //            break;
            //        case "k":
            //            bezierPoints[pointer].y += -2f;
            //            break;
            //    }
            //    updatePoints = true;
            //}
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

            float a = Mathf.Sin(rotation / 2f);
            Qr = new Quaternion(a * Q.x, a * Q.y, a * Q.z, Mathf.Cos(rotation / 2f)).normalized;

            Vector3 prevCenter = Vector3.zero, prevPeref = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                Vector3 v = new Vector3(0f, 0f, 10f) * (-0.00390625f * Mathf.Pow((i + 1) / (float)segments * 32 - 16, 2) + 1);
                Quaternion Qv = ((Vector3)mainPoints[i] - prevCenter).ToQuaternion();
                Qv.Normalize();
                float ang = -Mathf.Sin(20f * Mathf.PI / 180f);
                Qv = new Quaternion(Qv.x * ang, Qv.y * ang, Qv.z * ang, Mathf.Cos(20f * Mathf.PI / 180f)).normalized;
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
            leafVertices = FCustom.ReverseIfNecessary(leafVertices);
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
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(2 * segments, false, true);
            //sLeaser.sprites[1] = TriangleMesh.MakeLongMesh(2, false, true);
            angleText = new FLabel(Custom.GetFont(), "");
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.vertices.Length; i++)
                mesh.MoveVertice(i, pos + (Vector2)leafVertices[i / 4, i % 4]);

            Vector3 n = FCustom.CrossProduct(leafVertices[2, 2] - leafVertices[2, 0], leafVertices[2, 1] - leafVertices[2, 0]);
            angleText.x = pos.x;
            angleText.y = pos.y - 50f;
            angleText.text = $"Q_v:{Q}\nQ_rt:{Qr}\nAngle: {(int)(rotation * 180f / Mathf.PI)}\n{n}";

            //mesh = sLeaser.sprites[1] as TriangleMesh;
            //for (int i = 0; i < 2; i++)
            //{
            //    Vector2 v = new Vector2(-30f + 30f * i, 30f + 30f * i);
            //    mesh.MoveVertice(i * 4 + 0, pos + v + new Vector2(-10f, 10f));
            //    mesh.MoveVertice(i * 4 + 1, pos + v + new Vector2(10f, 10f));
            //    mesh.MoveVertice(i * 4 + 2, pos + v + new Vector2(-10f, -10f));
            //    mesh.MoveVertice(i * 4 + 3, pos + v + new Vector2(10f, -10f));
            //}
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length / 8; i++)
            {
                bool b = leafVertices[0, 2].z == 0f;
                Color main = new(0.25f, 0.05f, 0.45f), second = Color.green;
                float fl = i / (float)(leafVertices.GetLength(0) / 2f), fh = (i + 1) / (float)(leafVertices.GetLength(0) / 2f);
                Color cl = Color.Lerp(b ? main : second, b ? second : main, fl - (b ? 0.2f : -0.2f)), ch = Color.Lerp(b ? main : second, b ? second : main, fh - (b ? 0.2f : -0.2f));
                //Vector3 n = FCustom.CrossProduct(mesh.vertices[i * 8 + 2] - mesh.vertices[i * 8], mesh.vertices[i * 8 + 1] - mesh.vertices[i * 8]);
                mesh.verticeColors[i * 8 + 0] = Color.Lerp(b ? ch : cl, palette.blackColor, 0.3f);
                mesh.verticeColors[i * 8 + 1] = Color.Lerp(b ? ch : cl, palette.blackColor, 0.3f);
                mesh.verticeColors[i * 8 + 2] = Color.Lerp(b ? cl : ch, palette.blackColor, 0.3f);
                mesh.verticeColors[i * 8 + 3] = Color.Lerp(b ? cl : ch, palette.blackColor, 0.3f);
                mesh.verticeColors[i * 8 + 4] = Color.Lerp(b ? ch : cl, palette.blackColor, 0.3f);
                mesh.verticeColors[i * 8 + 5] = Color.Lerp(b ? ch : cl, palette.blackColor, 0.3f);
                mesh.verticeColors[i * 8 + 6] = Color.Lerp(b ? cl : ch, palette.blackColor, 0.3f);
                mesh.verticeColors[i * 8 + 7] = Color.Lerp(b ? cl : ch, palette.blackColor, 0.3f);
            }
            mesh.verticeColors[8] = Color.red;
            mesh.verticeColors[9] = Color.red;
            mesh.verticeColors[10] = Color.red;
            mesh.verticeColors[11] = Color.red;
            //mesh = sLeaser.sprites[1] as TriangleMesh;
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
            angleText.RemoveFromContainer();
            rCam.ReturnFContainer("HUD").AddChild(angleText);
        }
    }
}
