using UnityEngine;
using RWCustom;
using HUD;
using System.Linq;

namespace Silvermist
{
    public class Silvermist : PhysicalObject, IDrawable
    {
        public Vector2 placedPos, rootPos;
        public Vector2[,] stalkSegments;
        public Color color;
        public float darkness, lastDarkness;
        public int stalkSegs, leaves;
        public bool twilight;
        public AbstractConsumable AbstractSilvermist => abstractPhysicalObject as AbstractConsumable;
        public int TotalSprites => 11 + leaves * 2;

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
            SetSegs(stalkSegments);
            if (twilight)
                color = Custom.HSL2RGB(Custom.WrappedRandomVariation(0.8f, 0.05f, 0.6f), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            else color = Custom.HSL2RGB(Mathf.Lerp(0f, 0.167f, Random.value), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
            leaves = Random.Range(4, 8);
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
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            if (AbstractSilvermist.placedObjectIndex > -1 && AbstractSilvermist.placedObjectIndex < placeRoom.roomSettings.placedObjects.Count)
                placedPos = placeRoom.roomSettings.placedObjects[AbstractSilvermist.placedObjectIndex].pos;
            else placedPos = placeRoom.MiddleOfTile(AbstractSilvermist.pos);

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

        public void SetSegs(Vector2[,] segs)
        {
            Vector2 lastV = Vector2.up;
            Vector2 slope = lastV;
            for (int i = 0; i < segs.GetLength(0); i++)
            {
                float ctan = slope.x / slope.y;
                float num = 0f;
                if (ctan > 0.36f) num = -0.3f;
                else if (ctan < -0.36f) num = 0.3f;
                else if (Mathf.Abs(ctan) > 0.2f) num = Mathf.Lerp(0f, ctan / 3f, Random.value);
                segs[i, 0] = (lastV + new Vector2(Mathf.Lerp(-0.2f + num, 0.2f + num, Random.value), 0f)).normalized;
                segs[i, 1] *= 0f;
                lastV = segs[i, 0];
                slope = (slope + lastV).normalized;
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1]; //TotalSprites
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(stalkSegs, false, true);
            //float num = 90f;
            //for (int i = 1; i < 6; i++)
            //{
            //    int r = Random.Range(1, 11);
            //    foreach (var item in Futile.atlasManager._allElementsByName)
            //    {
            //        if (item.Value.name.Contains($"Silvermist{r}leaf2"))
            //            sLeaser.sprites[i + 5] = new FSprite(item.Value) { anchorX = 0f, anchorY = 0f, scale = stalkSegs / 16f };
            //        else if (item.Value.name.Contains($"Silvermist{r}leaf"))
            //            sLeaser.sprites[i] = new FSprite(item.Value) { anchorX = 0f, anchorY = 0f, scale = stalkSegs / 16f };
            //    }
            //    float rt = (num < 100f) ? Mathf.Lerp(num - 60f, num, Random.value) : Mathf.Lerp(num, num + 60f, Random.value);
            //    sLeaser.sprites[i + 5].rotation = rt;
            //    sLeaser.sprites[i].rotation = rt;
            //    num += 180f;
            //}
            //num = 90f;
            //for (int i = 11; i < 11 + leaves; i++)
            //{
            //    int r = Random.Range(1, 11);
            //    foreach (var item in Futile.atlasManager._allElementsByName)
            //    {
            //        if (item.Value.name.Contains($"Silvermist{r}leaf2"))
            //            sLeaser.sprites[i + leaves] = new FSprite(item.Value) { anchorX = 0f, anchorY = 0f, scale = stalkSegs / 14f };
            //        else if (item.Value.name.Contains($"Silvermist{r}leaf"))
            //            sLeaser.sprites[i] = new FSprite(item.Value) { anchorX = 0f, anchorY = 0f, scale = stalkSegs / 14f };
            //    }
            //    float rt = (num < 100f) ? Mathf.Lerp(num - 60f, num, Random.value) : Mathf.Lerp(num, num + 60f, Random.value);
            //    sLeaser.sprites[i + leaves].rotation = rt;
            //    sLeaser.sprites[i].rotation = rt;
            //    num += 180f;
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

            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            Vector2 prevTilt = new Vector2(2.5f, 0f);
            Vector2 point = rootPos - camPos;
            for (int i = 0; i < stalkSegs; i++)
            {
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

            //for (int i = 1; i < TotalSprites; i++)
            //{
            //    sLeaser.sprites[i].x = rootPos.x - camPos.x + 2f;
            //    sLeaser.sprites[i].y = rootPos.y - camPos.y;
            //}

            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < mesh.verticeColors.Length; i++)
            {
                float num = (i / 4 + ((i % 4 < 2) ? 1 : 0)) / (float)stalkSegs;
                mesh.verticeColors[i] = Color.Lerp(Color.Lerp(palette.blackColor + color * 0.05f, color, num), palette.blackColor, darkness);
            }
            //for (int i = 1; i < 6; i++)
            //{
            //    sLeaser.sprites[i].color = Color.Lerp(new Color(0.1f, 0.05f, 0.6f + Mathf.Lerp(-0.1f, 0.1f, Random.value)), palette.blackColor, darkness);
            //    sLeaser.sprites[i + 5].color = Color.Lerp(new Color(0f, 0.9f + Mathf.Lerp(-0.1f, 0.1f, Random.value), 0f), palette.blackColor, darkness);
            //}
            //for (int i = 11; i < 11 + leaves; i++)
            //{
            //    sLeaser.sprites[i].color = Color.Lerp(new Color(0.3f, 0.1f, 0.7f + Mathf.Lerp(-0.1f, 0.1f, Random.value)), palette.blackColor, darkness);
            //    sLeaser.sprites[i + leaves].color = Color.Lerp(new Color(0.1f, 0.9f + Mathf.Lerp(-0.1f, 0.1f, Random.value), 0f), palette.blackColor, darkness);
            //}
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = newContatiner ?? rCam.ReturnFContainer("Background");
            foreach (FSprite sprite in sLeaser.sprites)
                sprite.RemoveFromContainer();
            newContatiner.AddChild(sLeaser.sprites[0]);
            for (int i = 0; i < TotalSprites; i++)
                rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
        }

        public class Leaf
        {
            public Silvermist owner;
            public Vector2 startDir;
            public float size, thickness, rtPoint;

            public Leaf(Silvermist owner, Vector2 direction, float size)
            {
                this.owner = owner;
                startDir = direction;
                this.size = size;
            }
        }
    }
}
