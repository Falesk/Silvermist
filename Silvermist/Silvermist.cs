using UnityEngine;
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
        public int stalkSegs;
        public bool twilight;
        public AbstractConsumable AbstractSilvermist => abstractPhysicalObject as AbstractConsumable;
        public int TotalSprites => 1 + leaves.Length;

        public Silvermist(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, Vector2.zero, 5, 0.05f) };
            bodyChunkConnections = new BodyChunkConnection[0];
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
            leaves = new Leaf[Random.Range(5, 9)];
            bool flag = false;
            for (int i = 0; i < leaves.Length; i++)
            {
                float len = Random.Range(60f, 90f);
                Vector2[] Ps = ResolveLeafSegments(len);
                if (flag)
                {
                    Ps[0].x *= -1;
                    Ps[1].x *= -1;
                    Ps[2].x *= -1;
                }
                leaves[i] = new Leaf(len, Ps, this) {
                    mainColor = Color.Lerp(Color.black, Color.green, Mathf.Lerp(0.4f, 1f, i / (float)(leaves.Length - 1))),
                    secondColor = Color.Lerp(Color.black, new Color(0.2f, 0f, 0.5f), Mathf.Lerp(0.4f, 1f, i / (float)(leaves.Length - 1)))
                };
                flag = !flag;
            }
            Random.state = state;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (ModManager.MSC && MoreSlugcats.MMF.cfgCreatureSense.Value && room.world.game.IsStorySession && room.world.game.cameras[0]?.hud != null)
            {
                MoreSlugcats.PersistentObjectTracker tracker = new MoreSlugcats.PersistentObjectTracker(abstractPhysicalObject);
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
            Vector2[] Ps = new Vector2[3];
            Ps[0].x = Mathf.Lerp(0f, len / 10f, Random.value);
            Ps[0].y = Mathf.Lerp(len / 10f, Ps[0].x * 3f, Random.value);
            Ps[1].x = Mathf.Lerp(len / 2f, len - len / 5f, Random.value);
            if (Ps[0].y < len / 5f)
                Ps[1].y = Mathf.Lerp(Ps[0].y, len * 0.8f, Random.value);
            else Ps[1].y = Mathf.Lerp(-10f, Ps[0].y + len / 15f, Random.value);
            Ps[2].x = Mathf.Lerp(Ps[1].x + len / 5f, Ps[1].x + len / 5f + 0.2f * (Ps[1].x - Ps[0].x), Random.value);
            Ps[2].y = Mathf.Lerp(Ps[1].y - len / 10f, Ps[1].y / 2f, Random.value);
            return Ps;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TotalSprites];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(stalkSegs, false, true);
            for (int i = 1; i < TotalSprites; i++)
                sLeaser.sprites[i] = leaves[i - 1].InitTriangleMesh();
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
            Vector2 prevTilt = new Vector2(2.5f, 0f);
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
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].Draw(sLeaser.sprites[i + 1] as TriangleMesh, timeStacker, camPos, rCam.currentPalette);

            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
            {
                float num = (i / 4 + ((i % 4 < 2) ? 1 : 0)) / (float)stalkSegs;
                mesh.verticeColors[i] = Color.Lerp(Color.Lerp(palette.blackColor + new Color(0.2f, 0f, 0.5f) * 0.05f, color, num), palette.blackColor, darkness);
            }
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

        public class Leaf
        {
            public Silvermist owner;
            public Vector2[,] segs;
            public Color mainColor, secondColor;
            public float length, sizeFac, segLen = 10f;

            public Leaf(float len, Vector2[] BezierPoints, Silvermist silvermist)
            {
                segs = new Vector2[(int)(len / segLen), 2];
                length = len;
                owner = silvermist;
                sizeFac = Mathf.Lerp(0.8f, 1.2f, Random.value);
                SetSegs(BezierPoints);
            }

            public void SetSegs(Vector2[] BezierPoints)
            {
                if (BezierPoints.Length < 3)
                    return;
                Vector2 P1 = BezierPoints[0];
                Vector2 P2 = BezierPoints[1];
                Vector2 P3 = BezierPoints[2];

                Vector2[] points = FCustom.BezierCurve(segs.GetLength(0), P1, P2, P3);
                for (int i = 0; i < segs.GetLength(0); i++)
                {
                    segs[i, 0] = points[i];
                    segs[i, 1] = segs[i, 0];
                }
            }

            public void Update()
            {
                for (int i = 0; i < segs.GetLength(0); i++)
                    segs[i, 1] = segs[i, 0];
            }

            public TriangleMesh InitTriangleMesh() => TriangleMesh.MakeLongMesh(segs.GetLength(0), false, true);

            public void Draw(TriangleMesh mesh, float timeStacker, Vector2 camPos, RoomPalette palette)
            {
                Vector2 pos = owner.rootPos - camPos + (segs[0, 0].x < 0 ? - 3f : 3f) * Custom.PerpendicularVector(segs[0, 0].normalized) - 2f * segs[0, 0].normalized;
                Vector2 prev = Vector2.zero, prevDir = Vector2.zero;
                for (int i = 0; i < segs.GetLength(0); i++) {
                    Vector2 v = Vector2.Lerp(segs[i, 1], segs[i, 0], timeStacker);
                    Vector2 dir = v - prev;
                    for (int j = 0; j < 4; j++) {
                        float x = (i + (j < 2 ? 1 : 0)) / (float)segs.GetLength(0);
                        float size = -4f * x * (x - 1) * sizeFac;
                        Vector2 perp = Custom.PerpendicularVector(dir).normalized * 6f * size * (j % 2 == 0 ? -1 : 1);
                        Vector2 prevPerp = Custom.PerpendicularVector(prevDir).normalized * 6f * size * (j % 2 == 0 ? -1 : 1);
                        if (j < 2)
                            mesh.MoveVertice(i * 4 + j, pos + v + perp);
                        else mesh.MoveVertice(i * 4 + j, pos + prev + prevPerp);
                    }
                    prev = v;
                    prevDir = dir;
                }
                for (int i = 0; i < mesh.verticeColors.Length; i++)
                {
                    float x = (i / 4 + (i % 4 < 2 ? 1 : 0)) / (float)segs.GetLength(0);
                    mesh.verticeColors[i] = Color.Lerp(Color.Lerp(secondColor, mainColor, 1.25f * x - 0.15f), palette.blackColor, owner.darkness);
                }
            }
        }
    }
}
