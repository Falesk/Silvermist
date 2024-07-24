using UnityEngine;
using RWCustom;
using static UnityEngine.RectTransform;

namespace Silvermist
{
    public class DebugObj : PhysicalObject, IDrawable
    {
        public Vector2 rotation, lastRotation;
        public Vector2[,] points;
        public Vector2[] vertices;
        public SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;
        public float darkness, lastDarkness, axisRotation;
        public int segments;

        public DebugObj(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, Vector2.zero, 30, 0.05f) };
            bodyChunkConnections = new BodyChunkConnection[0];
            gravity = 0;//0.9f;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 0;
            bounce = 0.1f;
            buoyancy = 0.9f;
            segments = 8;
            points = new Vector2[segments * 2, 3];
            vertices = new Vector2[] { new Vector2(5f, 5f), new Vector2(-5f, 5f), new Vector2(5f, -5f), 
                new Vector2(-5f, -5f), new Vector2(5f, 10f), new Vector2(-5f, 10f) };
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastRotation = rotation;
            //if (firstChunk.ContactPoint.y < 0)
            //{
            //    rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.05f * firstChunk.vel.x).normalized;
            //    firstChunk.vel.x *= 0.8f;
            //}
            rotation = (rotation + Custom.PerpendicularVector(rotation) * 0.03f).normalized;
            axisRotation += 0.00625f;
            if (axisRotation > 1f)
                axisRotation = -1f;
            for (int i = 0; i < points.GetLength(0); i++)
            {
                float angle = Vector2.Angle(lastRotation, rotation) * ((firstChunk.vel.x > 0) ? -1 : 1);
                points[i, 1] = points[i, 0];
                points[i, 0] = FCustom.RotateVector(points[i, 0], angle);
                points[i, 2] *= airFriction;
                points[i, 2].y -= gravity;
            }
            for (int i = 0; i < points.GetLength(0); i++)
            {
                SharedPhysics.TerrainCollisionData data = scratchTerrainCollisionData.Set(points[i, 0], points[i, 1], points[i, 2], 1f, default, firstChunk.goThroughFloors);
                data = SharedPhysics.VerticalCollision(room, data);
                data = SharedPhysics.HorizontalCollision(room, data);
                data = SharedPhysics.SlopesVertically(room, data);
                points[i, 0] = data.pos;
                points[i, 2] = data.vel;
            }
            switch (Input.inputString)
            {
                case "1":
                    vertices[0] = Futile.mousePosition;
                    break;
                case "2":
                    vertices[1] = Futile.mousePosition;
                    break;
                case "3":
                    vertices[2] = Futile.mousePosition;
                    break;
                case "4":
                    vertices[3] = Futile.mousePosition;
                    break;
                case "5":
                    vertices[4] = Futile.mousePosition;
                    break;
                case "6":
                    vertices[5] = Futile.mousePosition;
                    break;
            }
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos));
            rotation = Custom.RNV();
            axisRotation = 0.5f;
            lastRotation = rotation;
            ResetPoints();
        }

        public void ResetPoints()
        {
            for (int i = 0; i < points.GetLength(0); i++)
            {
                float ang = 360f * i / points.GetLength(0);

                points[i, 0] = FCustom.RotateVector(Custom.PerpendicularVector(rotation), ang);
                points[i, 1] = points[i, 0];
                points[i, 2] *= 0f;
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[]
            {
                TriangleMesh.MakeLongMesh(segments, false, true),
                TriangleMesh.MakeLongMesh(1, false, true)
            };
            sLeaser.sprites[0].isVisible = false;
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            Vector2 rt = Vector3.Slerp(lastRotation, rotation, timeStacker);
            lastDarkness = darkness;
            darkness = rCam.room.Darkness(pos) * (1f - rCam.room.LightSourceExposure(pos));
            if (darkness != lastDarkness)
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            else UpdateColors(sLeaser, rCam.currentPalette);

            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            Vector2 v = pos - camPos;
            for (int i = 0; i < points.GetLength(0); i += 2)
            {
                int ind = i / 2;
                Vector2 v1 = Vector2.Lerp(points[i, 1], points[i, 0], timeStacker);
                Vector2 v2 = Vector2.Lerp(points[i + 1, 1], points[i + 1, 0], timeStacker);
                Vector2 v3 = Vector2.Lerp(points[(points.GetLength(0) - i > 2) ? i + 2 : 0, 1], points[(points.GetLength(0) - i > 2) ? i + 2 : 0, 0], timeStacker);
                mesh.MoveVertice(ind * 4, v + v1 * 30f);
                mesh.MoveVertice(ind * 4 + 1, v);
                mesh.MoveVertice(ind * 4 + 2, v + v2 * 30f);
                mesh.MoveVertice(ind * 4 + 3, v + v3 * 30f);
            }

            mesh = sLeaser.sprites[1] as TriangleMesh;
            float axisRt = Mathf.Sin(axisRotation * Mathf.PI);
            for (int i = 0; i < 4; i++)
            {
                Vector2 vector = new Vector2(20f, 20f);
                vector.x *= ((i == 1 || i == 3) ? -1 : 1) * axisRt;
                vector.y *= (i > 1) ? -1 : 1;
                vector = FCustom.RotateVector(vector, -Custom.VecToDeg(rt));
                mesh.MoveVertice(i, v + vector);
            }

            //for (int i = 0; i < 8; i++) {
            //    if (i == 4 || i == 5)
            //        mesh.MoveVertice(i, vertices[i - 4]);
            //    else if (i == 6 || i == 7)
            //        mesh.MoveVertice(i, vertices[i - 2]);
            //    else mesh.MoveVertice(i, vertices[i]);
            //}

            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
                mesh.verticeColors[i] = Color.Lerp((i % 4 == 1) ? Color.red : Color.green, palette.blackColor, darkness);

            UpdateColors(sLeaser, palette);
            //for (int i = 0; i < mesh.verticeColors.Length; i++)
            //{
            //    if (i < 2 || i == 4 || i == 5)
            //        mesh.verticeColors[i] = Color.Lerp(Color.green, palette.blackColor, 0.7f);
            //    else mesh.verticeColors[i] = Color.green;
            //}
        }

        public void UpdateColors(RoomCamera.SpriteLeaser sLeaser, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[1] as TriangleMesh;

            mesh.verticeColors[0] = Color.red;
            mesh.verticeColors[1] = Color.green;
            mesh.verticeColors[2] = Color.blue;
            mesh.verticeColors[3] = Color.yellow;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
                mesh.verticeColors[i] = Color.Lerp(mesh.verticeColors[i], palette.blackColor, axisRotation > 0 ? 0.1f : 0.6f);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = newContatiner ?? rCam.ReturnFContainer("Background");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
        }
    }
}
