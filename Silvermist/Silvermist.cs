﻿using UnityEngine;
using RWCustom;
using HUD;
using System.Linq;

namespace Silvermist
{
    public class Silvermist : PhysicalObject, IDrawable
    {
        public Leaf[] leaves;
        public Vector2 placedPos, rootPos;
        public Vector2[,] stalkSegments;
        public Color color;
        public float darkness, lastDarkness;
        public int stalkSegs, stalkSprite;
        public bool twilight;
        public AbstractConsumable AbstractSilvermist => abstractPhysicalObject as AbstractConsumable;
        public int TotalSprites => 1 + leaves.Length;

        public Silvermist(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = [ new BodyChunk(this, 0, Vector2.zero, 5, 0.05f) ];
            bodyChunkConnections = [];
            gravity = 0f;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 2;
            bounce = 0.1f;
            buoyancy = 0.1f;
            Random.State state = Random.state;
            Random.InitState(abstr.ID.RandomSeed);
            stalkSegments = new Vector2[Random.Range(8, 15), 2];
            stalkSegs = stalkSegments.GetLength(0);
            SetStalkSegs();
            if (twilight)
                color = Custom.HSL2RGB(Custom.WrappedRandomVariation(0.8f, 0.05f, 0.6f), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            else color = Custom.HSL2RGB(Mathf.Lerp(0f, 0.167f, Random.value), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            leaves = new Leaf[Random.Range(7, 10)];
            for (int i = 0; i < leaves.Length; i++)
            {
                float len = Random.Range(60f, 90f) * Mathf.Lerp(1f, 1.2f, Mathf.InverseLerp(8, 14, stalkSegs));
                Vector2[] Ps = ResolveLeafSegments(len);
                leaves[i] = new Leaf(this, i * 2f * Mathf.PI * (1f - FCustom.FI) + Mathf.Lerp(-Mathf.PI / 30f, Mathf.PI / 30f, Random.value), Ps)
                {
                    main = Color.Lerp(Color.black, Color.green, Mathf.Lerp(0.4f, 1f, i / (float)(leaves.Length - 1))),
                    second = Color.Lerp(Color.black, new(0.25f, 0.05f, 0.45f), Mathf.Lerp(0.4f, 1f, i / (float)(leaves.Length - 1)))
                };
            }
            Random.state = state;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (ModManager.MSC && MoreSlugcats.MMF.cfgCreatureSense.Value && room.world.game.IsStorySession && room.world.game.cameras[0]?.hud != null)
            {
                MoreSlugcats.PersistentObjectTracker tracker = new(abstractPhysicalObject);
                Map map = room.world.game.cameras[0].hud.map;
                if (!map.mapData.objectTrackers.Any(tr => tr.obj.ID == abstractPhysicalObject.ID && tr.obj.type == Register.ObjectTypes.Silvermist))
                    map.addTracker(tracker);
                else if (map.mapData.objectTrackers.Any(tr => tr.obj.Room != abstractPhysicalObject.Room && tr.obj.ID == abstractPhysicalObject.ID && tr.obj.type == Register.ObjectTypes.Silvermist))
                    map.removeTracker(tracker);
            }
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].Update();
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            if (AbstractSilvermist.placedObjectIndex > -1 && AbstractSilvermist.placedObjectIndex < placeRoom.roomSettings.placedObjects.Count)
                placedPos = placeRoom.roomSettings.placedObjects[AbstractSilvermist.placedObjectIndex].pos;
            else placedPos = placeRoom.MiddleOfTile(abstractPhysicalObject.pos);

            rootPos = default;
            if (!placeRoom.GetTile(placedPos).Solid)
            {
                int x = placeRoom.GetTile(placedPos).X;
                for (int y = placeRoom.GetTile(placedPos).Y; y > 0; y--)
                {
                    if (placeRoom.Tiles[x, y].Solid)
                    {
                        rootPos = placeRoom.MiddleOfTile(x, y);
                        break;
                    }
                }
            }
            if (rootPos == default)
                rootPos = placedPos;
            if (placeRoom.GetTile(rootPos).Solid)
            {
                int x = placeRoom.GetTile(rootPos).X;
                for (int y = placeRoom.GetTile(rootPos).Y + 3; y < placeRoom.Tiles.GetLength(1); y++)
                {
                    if (!(placeRoom.Tiles[x, y].Solid || placeRoom.Tiles[x, y - 1].Solid || placeRoom.Tiles[x, y - 2].Solid) && placeRoom.Tiles[x, y - 3].Solid)
                    {
                        rootPos = placeRoom.MiddleOfTile(x, y - 3);
                        break;
                    }
                }
            }

            rootPos.y += 10f;
            rootPos.x += Random.Range(-10f, 10f);
            firstChunk.HardSetPosition(new Vector2(rootPos.x, rootPos.y + 10f));
        }

        public void SetStalkSegs()
        {
            Vector2 lastV = Vector2.up;
            Vector2 slope = lastV;
            for (int i = 0; i < stalkSegments.GetLength(0); i++) {
                float ctan = slope.x / slope.y;
                float num = 0f;
                if (ctan > 0.36f) num = -0.3f;
                else if (ctan < -0.36f) num = 0.3f;
                else if (Mathf.Abs(ctan) > 0.2f) num = Mathf.Lerp(0f, ctan / 3f, Random.value);
                stalkSegments[i, 0] = (lastV + new Vector2(Mathf.Lerp(-0.2f + num, 0.2f + num, Random.value), 0f)).normalized;
                stalkSegments[i, 1] *= 0f;
                lastV = stalkSegments[i, 0];
                slope = (slope + lastV).normalized;
            }
        }

        public Vector2[] ResolveLeafSegments(float len)
        {
            //Vector2[] Ps = new Vector2[3];
            //Ps[0].x = Mathf.Lerp(0f, len / 10f, Random.value);
            //Ps[0].y = Mathf.Lerp(len / 10f, Ps[0].x * 3f, Random.value);
            //Ps[1].x = Mathf.Lerp(len / 2f, len - len / 5f, Random.value);
            //if (Ps[0].y < len / 5f)
            //    Ps[1].y = Mathf.Lerp(Ps[0].y, len * 0.8f, Random.value);
            //else Ps[1].y = Mathf.Lerp(-10f, Ps[0].y + len / 15f, Random.value);
            //Ps[2].x = Mathf.Lerp(Ps[1].x + len / 5f, Ps[1].x + len / 5f + 0.2f * (Ps[1].x - Ps[0].x), Random.value);
            //Ps[2].y = Mathf.Lerp(Ps[1].y - len / 10f, Ps[1].y / 2f, Random.value);
            //return Ps;
            return [new Vector2(15f, 35f) * Mathf.Lerp(1f, 1.2f, Mathf.InverseLerp(90, 130, len)), new(45f, 45f), new(75f, 0f)];
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TotalSprites];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(stalkSegs, false, true);
            for (int i = 1; i < TotalSprites; i++)
                sLeaser.sprites[i] = leaves[i - 1].InitTriangleMesh();

            //int j = 0;
            //for (int i = 0; i < leaves.Length; i++)
            //{
            //    if (leaves[i].angle < Mathf.PI)
            //    {
            //        (sLeaser.sprites[j], sLeaser.sprites[i]) = (sLeaser.sprites[i], sLeaser.sprites[j]);
            //        sLeaser.sprites[j] = leaves[j++].InitTriangleMesh();
            //    }
            //}
            //stalkSprite = j;
            //sLeaser.sprites[stalkSprite] = TriangleMesh.MakeLongMesh(stalkSegs, false, true);
            //j++;
            //for (int i = 0; i < leaves.Length; i++)
            //{
            //    if (leaves[i].angle >= Mathf.PI)
            //    {
            //        (sLeaser.sprites[j], sLeaser.sprites[i]) = (sLeaser.sprites[i], sLeaser.sprites[j]);
            //        sLeaser.sprites[j] = leaves[j++].InitTriangleMesh();
            //    }
            //}
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            lastDarkness = darkness;
            darkness = rCam.room.Darkness(pos) * (1f - rCam.room.LightSourceExposure(pos));
            if (darkness != lastDarkness)
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            TriangleMesh mesh = sLeaser.sprites[stalkSprite] as TriangleMesh;
            Vector2 prevTilt = new (2.5f, 0f);
            Vector2 point = rootPos - camPos;

            for (int i = 0; i < stalkSegs; i++) {
                float num = Mathf.Lerp(5f, 0f, i / (float)(stalkSegs - 1));
                Vector2 v = stalkSegments[i, 0];
                Vector2 t = 0.5f * num * v;
                mesh.MoveVertice(i * 4 + 0, point + v * 15f + new Vector2(t.y, -t.x));
                mesh.MoveVertice(i * 4 + 1, point + v * 15f + new Vector2(-t.y, t.x));
                mesh.MoveVertice(i * 4 + 2, point + prevTilt);
                mesh.MoveVertice(i * 4 + 3, point - prevTilt);
                prevTilt = new Vector2(v.y, -v.x);
                point += v * 15f;
            }
            for (int i = 0, j = 0; i < TotalSprites; i++)
            {
                if (i != stalkSprite) leaves[i - j].Draw(sLeaser.sprites[i] as TriangleMesh, camPos, rCam.currentPalette);
                else j++;
            }

            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[stalkSprite] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
            {
                float num = (i / 4 + ((i % 4 < 2) ? 1 : 0)) / (float)stalkSegs;
                mesh.verticeColors[i] = Color.Lerp(Color.Lerp(palette.blackColor + new Color(0.2f, 0f, 0.5f) * 0.05f, color, num), palette.blackColor, darkness);
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Background");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
        }

        public class Leaf
        {
            public Silvermist owner;
            public Quaternion Qr;
            public Vector2[] mainPoints;
            public Vector3[,] leafVertices;
            public Color main, second;
            public int segments = 10;
            public float angle;
            public bool dirtyUpdate, dirtyPalette;

            public Leaf(Silvermist silvermist, float ang, Vector2[] points)
            {
                owner = silvermist;
                angle = ang - (int)(ang / Mathf.PI * 2f) * Mathf.PI * 2f;
                angle += (angle < 0) ? Mathf.PI * 2f : 0f;
                Quaternion Q = new Quaternion(Mathf.Lerp(-0.1f, 0.1f, Random.value), 1f, Mathf.Lerp(-0.1f, 0.1f, Random.value), 0f).normalized;
                float a = Mathf.Sin(angle / 2f);
                Qr = new Quaternion(a * Q.x, a * Q.y, a * Q.z, Mathf.Cos(angle / 2f)).normalized;
                leafVertices = new Vector3[2 * segments, 4];
                mainPoints = FCustom.BezierCurve(segments, points);
                dirtyUpdate = true;
                dirtyPalette = true;
            }

            public void Update()
            {
                if (dirtyUpdate)
                {
                    Vector3 prevCenter = Vector3.zero, prevPeref = Vector3.zero;
                    for (int i = 0; i < segments; i++)
                    {
                        Vector3 v = new Vector3(0f, 0f, 10f) * (-0.00390625f * Mathf.Pow((i + 1) / (float)segments * 32 - 16, 2) + 1 + (i == 0 ? 0.1f : 0f));
                        Quaternion Qv = ((Vector3)mainPoints[i] - prevCenter).ToQuaternion();
                        Qv.Normalize();
                        float ang = -Mathf.Sin(15f * Mathf.PI / 180f);
                        Qv = new Quaternion(Qv.x * ang, Qv.y * ang, Qv.z * ang, Mathf.Cos(15f * Mathf.PI / 180f)).normalized;
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
                    leafVertices = FCustom.ReverseIfNecessary(leafVertices, angle);

                    dirtyUpdate = false;
                }
            }

            public TriangleMesh InitTriangleMesh() => TriangleMesh.MakeLongMesh(2 * segments, false, true);
            public void Draw(TriangleMesh mesh, Vector2 camPos, RoomPalette palette)
            {
                Vector2 pos = owner.rootPos - camPos;
                ApplyPalette(mesh, palette);

                for (int i = 0; i < mesh.vertices.Length; i++)
                    mesh.MoveVertice(i, pos + (Vector2)leafVertices[i / 4, i % 4]);
            }
            public void ApplyPalette(TriangleMesh mesh, RoomPalette palette)
            {
                for (int i = 0; i < mesh.verticeColors.Length / 8; i++)
                {
                    bool b = leafVertices[0, 2].z == 0f;
                    float fl = i / (float)(leafVertices.GetLength(0) / 2f), fh = (i + 1) / (float)(leafVertices.GetLength(0) / 2f);
                    Color cl = Color.Lerp(b ? second : main, b ? main : second, fl + (b ? -0.2f : 0.2f)), ch = Color.Lerp(b ? second : main, b ? main : second, fh + (b ? -0.2f : 0.2f));
                    for (int j = 0; j < 8; j++)
                        mesh.verticeColors[i * 8 + j] = Color.Lerp(j % 4 < 2 ? (b ? ch : cl) : (b ? cl : ch), palette.blackColor, 0.3f);
                }
            }
        }
    }
}
