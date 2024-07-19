using UnityEngine;
using RWCustom;

namespace Silvermist
{
    public class DebugObj : PhysicalObject, IDrawable
    {
        public Vector2 rotation, lastRotation;
        public Vector2[,] points;
        public SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;
        public float darkness, lastDarkness;
        public int segments;

        public DebugObj(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, Vector2.zero, 30, 0.05f) };
            bodyChunkConnections = new BodyChunkConnection[0];
            gravity = 0.9f;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 1;
            bounce = 0.1f;
            buoyancy = 0.9f;
            segments = 8;
            points = new Vector2[segments * 2, 3];
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastRotation = rotation;
            if (firstChunk.ContactPoint.y < 0)
            {
                rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.05f * firstChunk.vel.x).normalized;
                firstChunk.vel.x *= 0.8f;
            }
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
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos));
            rotation = Custom.RNV();
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
            };
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            lastDarkness = darkness;
            darkness = rCam.room.Darkness(pos) * (1f - rCam.room.LightSourceExposure(pos));
            if (darkness != lastDarkness)
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);

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

            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
                mesh.verticeColors[i] = Color.Lerp((i % 4 == 1) ? Color.red : Color.green, palette.blackColor, darkness);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = newContatiner ?? rCam.ReturnFContainer("Items");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
        }
    }
}
