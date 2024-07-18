using UnityEngine;
using RWCustom;

namespace Silvermist
{
    public class DebugObj : PhysicalObject, IDrawable
    {
        public Vector2 rotation, lastRotation;
        public float darkness, lastDarkness;
        public int segments;

        public DebugObj(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, Vector2.zero, 15, 0.05f) };
            bodyChunkConnections = new BodyChunkConnection[0];
            gravity = 0.9f;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 1;
            bounce = 0.1f;
            buoyancy = 0.9f;
            segments = 8;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastRotation = rotation;
            if (firstChunk.ContactPoint.y < 0)
            {
                rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.1f * firstChunk.vel.x).normalized;
                firstChunk.vel.x *= 0.8f;
            }
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos));
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[]
            {
                TriangleMesh.MakeLongMesh(segments, false, true),
                new FSprite("Circle20") { scale = 0.4f, anchorY = 0f }
            };
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            Vector2 rt = Custom.PerpendicularVector(Vector3.Slerp(lastRotation, rotation, timeStacker));
            lastDarkness = darkness;
            darkness = rCam.room.Darkness(pos) * (1f - rCam.room.LightSourceExposure(pos));
            if (darkness != lastDarkness)
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            Vector2 v = pos - camPos;
            for (int i = 0; i < segments; i++)
            {
                float ang = i / (float)segments * 360f;
                float angRad = ang * Mathf.PI / 180f;
                float angNext = (i + 1) / (float)segments * 360f;

                mesh.MoveVertice(i * 4, v + new Vector2(rt.x * Mathf.Cos(angRad) - rt.y * Mathf.Sin(angRad), rt.x * Mathf.Sin(angRad) + rt.y * Mathf.Cos(angRad)) * 15f);
                mesh.MoveVertice(i * 4 + 1, v);
                mesh.MoveVertice(i * 4 + 2, v + FCustom.RotateVector(rt, ang - 180f / segments) * 15f);
                mesh.MoveVertice(i * 4 + 3, v + FCustom.RotateVector(rt, angNext) * 15f);
            }
            float angle = Custom.VecToDeg((Vector2)Futile.mousePosition - v) - 90f;
            sLeaser.sprites[1].SetPosition(v);

            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
            {
                Color color = (i % 4 == 1) ? Color.red : Color.green;
                mesh.verticeColors[i] = Color.Lerp(color, palette.blackColor, darkness);
            }
            sLeaser.sprites[1].color = Color.red;
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
