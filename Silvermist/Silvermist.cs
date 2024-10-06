using UnityEngine;
using RWCustom;
using HUD;
using System.Linq;

namespace Silvermist
{
    public class Silvermist : PhysicalObject, IDrawable
    {
        public Leaf[] leaves;
        public Petal[] petals;
        public Vector2 placedPos, rootPos;
        public Vector2[] bezierPoints;
        public Vector2[,] stalkSegments;
        public Color color;
        public float darkness, lastDarkness, catching;
        public int stalkSegs, stalkSprite, crtTime;
        public bool twilight, dirtyPoints;
        public AbstractConsumable AbstractSilvermist => abstractPhysicalObject as AbstractConsumable;
        public int TotalSprites => 1 + leaves.Length + petals.Length * 5;
        public BodyChunk collidedChunk;

        public Silvermist(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = [ new BodyChunk(this, 0, Vector2.zero, 1, 0.05f) ];
            bodyChunkConnections = [];
            gravity = 0f;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 2;
            bounce = 0.1f;
            buoyancy = 0.1f;
            dirtyPoints = false;
            Random.State state = Random.state;
            Random.InitState(abstr.ID.RandomSeed);
            twilight = Random.value < 0.66f;
            if (twilight)
                color = Random.value < 0.5f ? Custom.HSL2RGB(Custom.WrappedRandomVariation(0.8f, 0.07f, 0.6f), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f)) :
                    Custom.HSL2RGB(Custom.WrappedRandomVariation(0.05f, 0.03f, 0.6f), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            else color = Custom.HSL2RGB(Custom.WrappedRandomVariation(0.1f, 0.05f, 0.6f), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            stalkSegments = new Vector2[Random.Range(30, 43), 2];
            stalkSegs = stalkSegments.GetLength(0);
            SetStalkSegments();
            CreateLeaves();
            CreatePetals();
            firstChunk.rad = stalkSegs;
            Random.state = state;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            for (int i = 0; i < room.physicalObjects[1].Count; i++)
                if (room.physicalObjects[1][i] is Creature c && Mathf.Abs(rootPos.x - c.mainBodyChunk.pos.x) < stalkSegs && Mathf.Abs(rootPos.y - c.mainBodyChunk.pos.y) < stalkSegments[stalkSegs - 1, 0].y)
                    collidedChunk = c.mainBodyChunk;
            if (collidedChunk != null && (collidedChunk.owner == null || Mathf.Abs(rootPos.x - collidedChunk.pos.x) > 2f * stalkSegs || Mathf.Abs(rootPos.y - collidedChunk.pos.y) > 1.5f * stalkSegments[stalkSegs - 1, 0].y))
                collidedChunk = null;

            if (collidedChunk != null) crtTime++;
            else crtTime = 0;
            if (crtTime > 200 && bezierPoints[2].y > 5f)
                CatchProcess();

            if (dirtyPoints)
            {
                Vector2[] points = FCustom.BezierCurve(stalkSegs, bezierPoints);
                for (int i = 0; i < stalkSegs; i++)
                    stalkSegments[i, 0] = points[i];
                for (int i = 0; i < petals.Length; i++)
                    petals[i].pos = rootPos + stalkSegments[(int)(i * 2.5f) + 2, 0];
            }

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
            for (int i = 0; i < petals.Length; i++)
                petals[i].Update();
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            if (AbstractSilvermist.placedObjectIndex > -1 && AbstractSilvermist.placedObjectIndex < placeRoom.roomSettings.placedObjects.Count)
                placedPos = placeRoom.roomSettings.placedObjects[AbstractSilvermist.placedObjectIndex].pos;
            else placedPos = placeRoom.MiddleOfTile(abstractPhysicalObject.pos);

            rootPos = default;
            catching = default;
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
            for (int i = 0; i < petals.Length; i++)
                petals[i].pos += rootPos;
        }

        public void CreateLeaves()
        {
            leaves = new Leaf[Random.Range(8, 15)];
            float randAng = Mathf.Lerp(0f, Mathf.PI * 2f, Random.value);
            for (int i = 0; i < leaves.Length; i++)
            {
                float len = Random.Range(60f, 90f) * Mathf.Lerp(0.8f, 1.2f, Mathf.InverseLerp(30, 42, stalkSegs));
                Vector2[] Ps = SetLeafSegments(len, i < leaves.Length / 2);
                float ang = randAng + i * FCustom.gAngle + Mathf.Lerp(-Mathf.PI / 30f, Mathf.PI / 30f, Random.value);
                Color cl1 = twilight ? new(Mathf.Lerp(-0.05f, 0.05f, Random.value), Mathf.Lerp(-0.05f, 0.05f, Random.value), Mathf.Lerp(-0.05f, 0.05f, Random.value)) :
                    new(Mathf.Lerp(-0.05f, 0.15f, Random.value), Mathf.Lerp(-0.05f, 0.15f, Random.value), Mathf.Lerp(-0.05f, 0.15f, Random.value));
                Color cl2 = twilight ? new(Mathf.Lerp(-0.03f, 0.03f, Random.value), Mathf.Lerp(-0.03f, 0.03f, Random.value), Mathf.Lerp(-0.03f, 0.03f, Random.value)) :
                    new(Mathf.Lerp(-0.05f, 0.08f, Random.value), Mathf.Lerp(-0.05f, 0.08f, Random.value), Mathf.Lerp(-0.05f, 0.08f, Random.value));
                Color c1 = Color.Lerp(Color.black, twilight ? new(0f, 0.3f, 0f) : new(0.1f, 0.65f, 0.1f), Mathf.Lerp(0.4f, 1f, Mathf.Pow(Mathf.Sin(ang / 2f - Mathf.PI / 4f), 2))) + cl1;
                Color c2 = Color.Lerp(Color.black, new(0.25f, 0.12f, 0.45f), Mathf.Lerp(0.4f, 1f, Mathf.Pow(Mathf.Sin(ang / 2f - Mathf.PI / 4f), 2))) + cl2;
                leaves[i] = new Leaf(this, ang, Ps)
                {
                    clrMain = twilight ? c2 : c1,
                    clrSecond = twilight ? c1 : c2
                };
            }
        }

        public void CreatePetals()
        {
            int len = (int)(stalkSegs / 2.5f);
            len -= (len % 2 == 0) ? 1 : 0;
            petals = new Petal[len];
            float t = 90 - Mathf.Lerp(35, 50, Random.value);
            for (int i = 0; i < len; i++)
            {
                Vector2 attachPos = stalkSegments[(int)(i * 2.5f) + 2, 0];
                float size = 6f * Mathf.Lerp(0.8f, 1.2f, Mathf.InverseLerp(30, 42, stalkSegs)) * Mathf.Lerp(1f, 0.5f, 2f * (i / 2) / (len - 1));
                float ang = 90 + Custom.VecToDeg(attachPos) + ((i % 2 == 0 ? 1.2f * t : -t) * Mathf.InverseLerp(1, 0, Mathf.Pow((i / 2) / (float)((len - 1) / 2), 3f))) + Mathf.Lerp(-5, 5, Random.value);
                petals[i] = new Petal(this, attachPos, size, ang, i);
            }
        }

        public void SetStalkSegments()
        {
            Vector2 P1 = new Vector2(0f, stalkSegs * 2.5f) + new Vector2(Mathf.Lerp(-15, 15, Random.value), Mathf.Lerp(-15, 15, Random.value));
            Vector2 P2 = new Vector2(0f, stalkSegs * 2.5f) + new Vector2(Mathf.Lerp(-15, 15, Random.value), Mathf.Lerp(-15, 15, Random.value));
            Vector2 P3 = new Vector2(0f, stalkSegs * 5f) + new Vector2(Mathf.Lerp(-15, 15, Random.value), Mathf.Lerp(-15, 15, Random.value));
            bezierPoints = [P1, P2, P3];
            Vector2[] points = FCustom.BezierCurve(stalkSegs, bezierPoints);
            for (int i = 0; i < stalkSegs; i++)
            {
                stalkSegments[i, 0] = points[i];
                stalkSegments[i, 1] = points[i];
            }
        }

        public Vector2[] SetLeafSegments(float len, bool straight)
        {
            Vector2 P1 = (straight ? new Vector2(25f, 20f) : new Vector2(25f, 10f)) + new Vector2(Mathf.Lerp(-8f, 8f, Random.value), Mathf.Lerp(-8f, 8f, Random.value));
            Vector2 P2 = (straight ? new Vector2(30f, 50f) : new Vector2(30f, 30f)) + new Vector2(Mathf.Lerp(-8f, 8f, Random.value), Mathf.Lerp(-8f, 8f, Random.value));
            Vector2 P3 = (straight ? new Vector2(45f, 50f) : new Vector2(60f, 5f)) + new Vector2(Mathf.Lerp(-8f, 8f, Random.value), Mathf.Lerp(-8f, 8f, Random.value));
            float m = Mathf.Lerp(0.7f, 1.2f, Mathf.InverseLerp(50f, 110f, len));
            return [P1 * m, P2 * m, P3 * m];
        }

        public void CatchProcess()
        {
            if (collidedChunk == null) return;
            bezierPoints[2] = FCustom.RotateVector(bezierPoints[2], Mathf.PI / 10f);
            bezierPoints[2].y += Mathf.Sin(-Mathf.PI / 10f) / 2f;
            catching = Mathf.InverseLerp(40, 5, bezierPoints[2].y);
            dirtyPoints = true;
            for (int i = 0; i < petals.Length; i++)
                petals[i].angle += Mathf.PI / 10f;
        }

        public void SortLeaves()
        {
            int j = 0;
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i].angle < Mathf.PI) {
                    (leaves[j], leaves[i]) = (leaves[i], leaves[j]);
                    j++;
                }
            stalkSprite = j;
            for (int i = 0, k = 0; i < j; i++)
                if (leaves[i].mainPoints[leaves[i].mainPoints.Length - 1].y < 40f) {
                    (leaves[k], leaves[i]) = (leaves[i], leaves[k]);
                    k++;
                }
            for (int i = j, k = 0; i < leaves.Length; i++)
                if (leaves[i].mainPoints[leaves[i].mainPoints.Length - 1].y > 40f) {
                    (leaves[k], leaves[i]) = (leaves[i], leaves[k]);
                    k++;
                }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TotalSprites];
            SortLeaves();
            for (int i = 0; i < stalkSprite; i++)
                sLeaser.sprites[i] = leaves[i].InitTriangleMesh();
            sLeaser.sprites[stalkSprite] = TriangleMesh.MakeLongMesh(stalkSegs, false, true);

            int n = stalkSprite + 1;
            for (int i = petals.Length - 1; i >= 0; i--)
            {
                FSprite[] sprs = petals[i].InitSprites(rCam);
                for (int j = 0; j < sprs.Length; j++)
                    sLeaser.sprites[n++] = sprs[j];
            }
            for (int i = stalkSprite; i < leaves.Length; i++)
                sLeaser.sprites[n++] = leaves[i].InitTriangleMesh();
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
            Vector2 prevSlp = new (3f, 0f), prevVec = Vector2.zero, point = rootPos - camPos;
            for (int i = 0; i < stalkSegs; i++) {
                float num = Mathf.Lerp(3f, 0.2f, i / (float)stalkSegs);
                Vector2 v = (stalkSegments[i, 0] - prevVec).normalized;
                Vector2 t = num * v;
                t = new Vector2(t.y, -t.x);
                v *= 7f;
                Vector2 ps = point + prevVec;
                mesh.MoveVertice(i * 4 + 0, ps + v - t);
                mesh.MoveVertice(i * 4 + 1, ps + v + t);
                mesh.MoveVertice(i * 4 + 2, ps - prevSlp);
                mesh.MoveVertice(i * 4 + 3, ps + prevSlp);
                prevSlp = t;
                prevVec = stalkSegments[i, 0];
            }
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].Draw(camPos, rCam.currentPalette);
            for (int i = 0; i < petals.Length; i++)
                petals[i].Draw(camPos, rCam.currentPalette);

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
            public TriangleMesh mesh;
            public Quaternion Qr;
            public Vector2[] mainPoints;
            public Vector3[,] leafVertices;
            public Color clrMain, clrSecond;
            public int segments = 10;
            public float angle;
            public bool dirtyUpdate;

            public Leaf(Silvermist silvermist, float ang, Vector2[] points)
            {
                owner = silvermist;
                angle = ang - (int)(ang / (Mathf.PI * 2f)) * Mathf.PI * 2f;
                Quaternion Q = new Quaternion(Mathf.Lerp(-0.1f, 0.1f, Random.value), 1f, Mathf.Lerp(-0.1f, 0.1f, Random.value), 0f).normalized;
                float a = Mathf.Sin(angle / 2f);
                Qr = new Quaternion(a * Q.x, a * Q.y, a * Q.z, Mathf.Cos(angle / 2f)).normalized;
                leafVertices = new Vector3[2 * segments, 4];
                mainPoints = FCustom.BezierCurve(segments, points);
                dirtyUpdate = true;
            }

            public void Update()
            {
                if (dirtyUpdate)
                {
                    Vector3 prevCenter = Vector3.zero, prevPeref = new (3, 0, 0);
                    for (int i = 0; i < segments; i++)
                    {
                        Vector3 v = new Vector3(0f, 0f, 10f) * (-0.00390625f * Mathf.Pow((i + 1) / (float)segments * 32 - 16, 2) + 1);
                        Quaternion Qv = ((Vector3)mainPoints[i] - prevCenter).ToQuaternion().normalized;
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

            public TriangleMesh InitTriangleMesh()
            {
                mesh = TriangleMesh.MakeLongMesh(2 * segments, false, true);
                return mesh;
            }

            public void Draw(Vector2 camPos, RoomPalette palette)
            {
                Vector2 pos = owner.rootPos - camPos;
                ApplyPalette(palette);

                for (int i = 0; i < mesh.vertices.Length; i++)
                    mesh.MoveVertice(i, pos + (Vector2)leafVertices[i / 4, i % 4]);
            }

            public void ApplyPalette(RoomPalette palette)
            {
                for (int i = 0; i < mesh.verticeColors.Length / 8; i++)
                {
                    bool b = leafVertices[0, 2].z == 0f;
                    float fl = i / (float)(leafVertices.GetLength(0) / 2f), fh = (i + 1) / (float)(leafVertices.GetLength(0) / 2f);
                    Color cl = Color.Lerp(b ? clrSecond : clrMain, b ? clrMain : clrSecond, fl + (b || owner.twilight ? -0.2f : 0.2f) + (owner.twilight ? 0.3f : 0f)), ch = Color.Lerp(b ? clrSecond : clrMain, b ? clrMain : clrSecond, fh + (b || owner.twilight ? -0.2f : 0.2f) + (owner.twilight ? 0.3f : 0f));
                    for (int j = 0; j < 8; j++)
                        mesh.verticeColors[i * 8 + j] = Color.Lerp(j % 4 < 2 ? (b ? ch : cl) : (b ? cl : ch), palette.blackColor, Mathf.Pow(owner.darkness, 2));
                }
            }
        }

        public class Petal
        {
            public Silvermist owner;
            public FSprite[] sprites;
            public Vector2[] points;
            public Vector2 pos, nectarPos;
            public float size, angle, nectarGrowth, threadLength;
            public int level, index;
            public bool attached;

            public Petal(Silvermist silvermist, Vector2 attachPos, float _size, float ang, int st)
            {
                owner = silvermist;
                pos = attachPos;
                size = _size;
                angle = ang;
                index = st;
                level = st / 2;
                points = new Vector2[4];
                nectarPos = pos;
                Vector2 p = Vector2.zero, prevDir = Vector2.right;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 d = FCustom.RotateVector(prevDir, Mathf.Lerp(-7f, 7f, Random.value));
                    points[i] = p + d;
                    prevDir = d;
                    p += d;
                }
            }

            public void Update()
            {
                nectarGrowth += (nectarGrowth >= 1) ? 0f : 0.005f * Mathf.Lerp(0.8f, 1.2f, Random.value);
                nectarGrowth = Mathf.Clamp01(nectarGrowth);

                if (owner.collidedChunk != null)
                {
                    float dist = Vector2.Distance(nectarPos, owner.collidedChunk.pos);
                    if (dist < 15f && !attached)
                        attached = true;
                    if (attached && dist < 60f)
                    {
                        threadLength = dist;
                        owner.collidedChunk.vel *= 0.8f - owner.catching * 0.6f;
                    }
                    else attached = false;
                    if (!attached) threadLength = Mathf.Lerp(threadLength, 0, 0.5f);
                }
            }

            public FSprite[] InitSprites(RoomCamera rCam)
            {
                sprites =
                [
                    new FSprite("Circle20") { anchorX = 0f },
                    TriangleMesh.MakeLongMesh(4, false, true),
                    new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["WaterNut"] },
                    new FSprite("Circle20") { anchorX = 0f },
                    new FSprite("pixel") { anchorX = 0f }
                ];
                return sprites;
            }

            public void Draw(Vector2 camPos, RoomPalette palette)
            {
                Vector2 p = pos - camPos;
                ApplyPalette(palette);

                TriangleMesh mesh = sprites[1] as TriangleMesh;
                mesh.SetPosition(p);
                p = Vector2.zero;
                Vector2 prev = Vector2.zero, prevPerp = Vector2.up;
                float t = size / 2f;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 v = points[i] - prev;
                    Vector2 perp = Custom.PerpendicularVector(v);
                    float m = (i == 3) ? 0f : 1f;
                    t *= (i == 3) ? 0.5f : 1f;
                    mesh.MoveVertice(i * 4 + 0, p + t * v + m * perp);
                    mesh.MoveVertice(i * 4 + 1, p + t * v - m * perp);
                    mesh.MoveVertice(i * 4 + 2, p + prevPerp);
                    mesh.MoveVertice(i * 4 + 3, p - prevPerp);
                    prevPerp = perp;
                    prev = v;
                    p += t * v;
                }
                mesh.rotation = -angle;
                p = mesh.GetPosition();

                nectarPos = p + camPos + FCustom.RotateVector(mesh.vertices[15], angle) * 1.5f + Vector2.up * 3f;
                sprites[2].SetPosition(nectarPos - camPos);
                float num = Mathf.Pow(Mathf.InverseLerp(1, 0, level / (float)(owner.petals.Length / 2)), 0.75f);
                sprites[2].scaleX = nectarGrowth * size / 4 * num;
                sprites[2].scaleY = nectarGrowth * size / 6 * num;
                sprites[2].rotation = -angle;

                sprites[0].SetPosition(p);
                sprites[0].rotation = -angle;
                sprites[0].scaleX = size / 6f * (num + 1f);
                sprites[0].scaleY = size / 10f * (num + 1f);

                sprites[3].SetPosition(p + Vector2.down * 6f * (1f - (index / (float)(owner.petals.Length - 1))));
                sprites[3].rotation = -angle;
                sprites[3].scaleX = size / 5f * (num + 1f);
                sprites[3].scaleY = size / 12f * (num + 1f);

                if (owner.collidedChunk != null && threadLength != 0)
                {
                    sprites[4].SetPosition(nectarPos - camPos + Vector2.up * 5f * num);
                    Vector2 d = owner.collidedChunk.pos - nectarPos;
                    sprites[4].rotation = -FCustom.AngleX(d);
                    sprites[4].scaleX = threadLength;
                }
                else sprites[4].scaleX = 0;
            }

            public void ApplyPalette(RoomPalette palette)
            {
                TriangleMesh mesh = sprites[1] as TriangleMesh;
                Color cm = Color.Lerp(owner.color, palette.blackColor, 0.25f);
                Color cs = Color.Lerp(owner.color, Color.white, 0.25f);
                for (int i = 0; i < mesh.verticeColors.Length; i++)
                {
                    if (i < mesh.vertices.Length - 8) mesh.verticeColors[i] = Color.Lerp(cm, palette.blackColor, owner.darkness);
                    else mesh.verticeColors[i] = Color.Lerp((i % 4 < 2 || i > mesh.vertices.Length - 5) ? cs : cm, palette.blackColor, owner.darkness);
                }
                sprites[0].color = Color.Lerp(Color.Lerp(owner.color, palette.blackColor, 0.6f), palette.blackColor, owner.darkness);
                sprites[2].color = Color.Lerp(cs, Color.white, 0.6f);
                sprites[3].color = Color.Lerp(owner.color, palette.blackColor, owner.darkness + 0.2f * (1f - (index / (float)(owner.petals.Length - 1))));
                sprites[4].color = sprites[2].color;
            }
        }
    }
}
