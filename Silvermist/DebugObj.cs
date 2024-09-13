using RWCustom;
using System;
using System.Linq;
using UnityEngine;

namespace Silvermist
{
    public class DebugObj : PhysicalObject, IDrawable
    {
        public bool updatePoints;
        public int segments = 16, pointer = -1;
        public float rotation;
        public Quaternion Q, Qr;
        public Vector2[] bezierPoints, mainPoints;
        public Vector3[] cube, changedCube;
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
            cube = new Vector3[]
            { new Vector3(-20f, -20f, 20f), new Vector3(20f, -20f, 20f), new Vector3(-20f, 20f, 20f), new Vector3(20f, 20f, 20f),
              new Vector3(-20f, -20f, -20f), new Vector3(20f, -20f, -20f), new Vector3(-20f, 20f, -20f), new Vector3(20f, 20f, -20f) };
            changedCube = new Vector3[cube.Length];
            cube.CopyTo(changedCube, 0);
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

            //var Q1 = Q;
            if (Input.GetKey("]")) Q.x += 0.1f;
            else if (Input.GetKey("[")) Q.x -= 0.1f;
            if (Input.GetKey("'")) Q.y += 0.1f;
            else if (Input.GetKey(";")) Q.y -= 0.1f;
            if (Input.GetKey(".")) Q.z += 0.1f;
            else if (Input.GetKey(",")) Q.z -= 0.1f;
            if (Input.GetKey("j")) Q.w += 1f;
            Q.Normalize();

            Qr = new Quaternion(Mathf.Sin(rotation / 2f) * Q.x, Mathf.Sin(rotation / 2f) * Q.y, Mathf.Sin(rotation / 2f) * Q.z, Mathf.Cos(rotation / 2f)).normalized;
            for (int i = 0; i < cube.Length; i++)
                changedCube[i] = ((Qr * cube[i]).ToQuaternion() * Qr.Сonjugate()).ToVector3();
            //if (Q == Q1)
            //{
            //    Qr = new Quaternion(Mathf.Sin(rotation / 2f) * Q.x, Mathf.Sin(rotation / 2f) * Q.y, Mathf.Sin(rotation / 2f) * Q.z, Mathf.Cos(rotation / 2f)).normalized;
            //    for (int i = 0; i < cube.Length; i++)
            //        changedCube[i] = ((Qr * cube[i]).ToQuaternion() * Qr.Сonjugate()).ToVector3();
            //}
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
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(2 * segments, false, true);
            sLeaser.sprites[1] = TriangleMesh.MakeLongMesh(segments, false, true);
            sLeaser.sprites[0].isVisible = false;
            sLeaser.sprites[1].isVisible = false;
            sLeaser.sprites[2] = TriangleMesh.MakeLongMesh(6, false, true);
            angleText = new FLabel(Custom.GetFont(), "");
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            TriangleMesh mesh = sLeaser.sprites[1] as TriangleMesh;
            Vector2 prev = Vector2.zero;
            for (int i = 0; i < segments; i++)
            {
                Vector2 v = (Vector2)(Qr * ((Vector3)mainPoints[i]).ToQuaternion() * Qr.Сonjugate()).ToVector3();
                Vector2 w = Custom.PerpendicularVector(v - prev).normalized;
                mesh.MoveVertice(4 * i, pos + v + w);
                mesh.MoveVertice(4 * i + 1, pos + v - w);
                mesh.MoveVertice(4 * i + 2, pos + prev + w);
                mesh.MoveVertice(4 * i + 3, pos + prev - w);
                prev = v;
            }
            angleText.x = pos.x;
            angleText.y = pos.y - 50f;
            angleText.text = $"Q:{Q}\nQ_rt:{Qr}\nAngle: {(int)(rotation * 180f / Mathf.PI)}  N: {pointer}";

            mesh = sLeaser.sprites[2] as TriangleMesh;
            float Pz1 = (changedCube[0].z + changedCube[1].z + changedCube[2].z + changedCube[3].z) / 4f;
            float Pz4 = (changedCube[4].z + changedCube[5].z + changedCube[6].z + changedCube[7].z) / 4f;

            float Pz2 = (changedCube[0].z + changedCube[2].z + changedCube[4].z + changedCube[6].z) / 4f;
            float Pz5 = (changedCube[1].z + changedCube[3].z + changedCube[5].z + changedCube[7].z) / 4f;

            float Pz3 = (changedCube[0].z + changedCube[1].z + changedCube[4].z + changedCube[5].z) / 4f;
            float Pz6 = (changedCube[2].z + changedCube[3].z + changedCube[6].z + changedCube[7].z) / 4f;

            float[] Pzs = new float[] { Pz1, Pz2, Pz3, Pz4, Pz5, Pz6 };
            Array.Sort(Pzs);
            for (int i = 0; i < 6; i++)
            {
                int ind = 0;
                Vector3 w = changedCube[(i < 3) ? Array.IndexOf(changedCube, changedCube.MinZ()) : Array.IndexOf(changedCube, changedCube.MaxZ())];
                for (int j = 0; j < changedCube.Length; j++)
                    if (w == changedCube[j]) ind = j;
                Vector3[] segment;

                float Pz = Pzs[i];
                if (Pz == Pz1 || Pz == Pz4) segment = (ind > 3) ? new Vector3[] { changedCube[0], changedCube[1], changedCube[2], changedCube[3] } :
                        new Vector3[] { changedCube[5], changedCube[4], changedCube[7], changedCube[6] };
                else if (Pz == Pz2 || Pz == Pz5) segment = (ind % 2 == 0) ? new Vector3[] { changedCube[0], changedCube[4], changedCube[2], changedCube[6] } :
                        new Vector3[] { changedCube[5], changedCube[1], changedCube[7], changedCube[3] };
                else segment = (ind < 6 && !(ind == 2 || ind == 3)) ? new Vector3[] { changedCube[5], changedCube[1], changedCube[4], changedCube[0] } :
                         new Vector3[] { changedCube[3], changedCube[7], changedCube[2], changedCube[6] };

                mesh.MoveVertice(4 * i + 0, pos + (Vector2)segment[0]);
                mesh.MoveVertice(4 * i + 1, pos + (Vector2)segment[1]);
                mesh.MoveVertice(4 * i + 2, pos + (Vector2)segment[2]);
                mesh.MoveVertice(4 * i + 3, pos + (Vector2)segment[3]);
            }


            //for (int i = 0; i < points.Length; i++)
            //{
            //    for (int j = 0; j < 3; j++)
            //        mesh.MoveVertice(3 * i + j, pos + (Vector2)points[i]);
            //}

            //bool rev = 150f < angle && angle < 330f;

            //mesh = sLeaser.sprites[0] as TriangleMesh;
            //prev = Vector2.zero;
            //float rX = angleX * Mathf.PI / 180f;
            ////float rY = angleY * Mathf.PI / 180f;
            //float tp = 0f;
            //for (int i = 0; i < segments; i++)
            //{
            //    float t = -0.00390625f * Mathf.Pow(-segments + 2 * (i + 1), 2) + 1;
            //    Vector2 v = FCustom.RotateVector(mainPoints[i], -angleY);
            //    v.x *= Mathf.Cos(rX);
            //    float s = Custom.VecToDeg(v) * Mathf.PI / 180f;
            //    Vector2 w = new Vector2(Mathf.Sin(rX + 0.5f) * Mathf.Cos(s), Mathf.Sin(0.5f) * Mathf.Sin(s));
            //    mesh.MoveVertice(8 * i, pos + v);
            //    mesh.MoveVertice(8 * i + 1, pos + v + new Vector2(20f * t * w.x, w.y));
            //    mesh.MoveVertice(8 * i + 2, pos + prev);
            //    mesh.MoveVertice(8 * i + 3, pos + v + new Vector2(20f * tp * w.x, w.y));

            //    w.x = Mathf.Sin(0.5f - rX) * Mathf.Cos(s);
            //    mesh.MoveVertice(8 * i + 4, pos + v);
            //    mesh.MoveVertice(8 * i + 5, pos + v + new Vector2(20f * t * w.x, w.y));
            //    mesh.MoveVertice(8 * i + 6, pos + prev);
            //    mesh.MoveVertice(8 * i + 7, pos + v + new Vector2(20f * tp * w.x, w.y));

            //    tp = t;
            //    prev = v;
            //}
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[1] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
                mesh.verticeColors[i] = Color.Lerp(Color.green, palette.blackColor, 0.4f);
            //mesh = sLeaser.sprites[0] as TriangleMesh;
            //for (int i = 0; i < mesh.verticeColors.Length; i++)
            //    mesh.verticeColors[i] = Color.Lerp(Color.green, palette.blackColor, (i % 2 == 0) ? 0.55f : 0.4f);

            mesh = sLeaser.sprites[2] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
            {
                Color c = Color.Lerp(Color.red, palette.blackColor, 0.9f - 0.6f * ((float)(i + 1) / mesh.verticeColors.Length));
                mesh.verticeColors[i] = c;
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
