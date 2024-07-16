using UnityEngine;
using RWCustom;
using HUD;
using System.Linq;

namespace Silvermist
{
    public class Silvermist : PhysicalObject, IDrawable
    {
        public Vector2 placedPos, rootPos;
        public float darkness, lastDarkness;
        public int isthmusSprite;
        public AbstractConsumable AbstractSilvermist => abstractPhysicalObject as AbstractConsumable;
        public int TotalSprites => 1 + isthmusSprite * 2;

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
            isthmusSprite = Random.Range(8, 15);
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

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TotalSprites];
            for (int i = 0; i < isthmusSprite; i++)
                sLeaser.sprites[i] = new FSprite("pixel") { scaleY = 15f, scaleX = 2f };
            for (int i = isthmusSprite; i < TotalSprites - 1; i++)
                sLeaser.sprites[i] = new FSprite("Circle20") { scale = 0.3f };
            sLeaser.sprites[TotalSprites - 1] = new FSprite("Circle20") { scale = 0.5f };
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            lastDarkness = darkness;
            darkness = rCam.room.Darkness(pos) * (1f - rCam.room.LightSourceExposure(pos));
            if (darkness != lastDarkness)
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            for (int i = 0; i < isthmusSprite; i++)
            {
                sLeaser.sprites[i].x = rootPos.x - camPos.x;
                sLeaser.sprites[i].y = rootPos.y - camPos.y + i * 15f + 7.5f;
            }
            for (int i = isthmusSprite; i < TotalSprites - 1; i++)
            {
                sLeaser.sprites[i].x = rootPos.x - camPos.x;
                sLeaser.sprites[i].y = rootPos.y - camPos.y + (i - (isthmusSprite - 1)) * 15f;
            }
            sLeaser.sprites[TotalSprites - 1].x = rootPos.x - camPos.x;
            sLeaser.sprites[TotalSprites - 1].y = rootPos.y - camPos.y;
            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = 0; i < isthmusSprite; i++)
                sLeaser.sprites[i].color = Color.Lerp(new Color(0f, i / (float)(isthmusSprite - 1), 0.2f), palette.blackColor, darkness);
            for (int i = isthmusSprite; i < TotalSprites; i++)
                sLeaser.sprites[i].color = Color.Lerp(Color.red, palette.blackColor, darkness);
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
