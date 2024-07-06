using HUD;
using RWCustom;
using System.Linq;
using UnityEngine;
using Mode = Weapon.Mode;

namespace Silvermist
{
    public class Nectar : PlayerCarryableItem, IDrawable, IPlayerEdible
    {
        public Nectar(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, Vector2.zero, 7, 0.05f) };
            bodyChunkConnections = new BodyChunkConnection[0];
            gravity = 0.9f;
            airFriction = 0.999f;
            waterFriction = 0.9f;
            surfaceFriction = 0.9f;
            collisionLayer = 1;
            bounce = 0.1f;
            buoyancy = 0.9f;
            canBeHitByWeapons = false;
            glimmer = 0f;
            lastGlimmerTime = 0;
            swallowed = 1f;
            mode = Mode.Free;
            Random.State state = Random.state;
            Random.InitState(abstr.ID.RandomSeed);
            jellySprite = Random.Range(1, 8);
            Random.state = state;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastRotation = rotation;
            if (grabbedBy.Count > 0)
            {
                rotation = Custom.PerpendicularVector(Custom.DirVec(firstChunk.pos, grabbedBy[0].grabber.mainBodyChunk.pos));
                rotation.y = Mathf.Abs(rotation.y);
            }
            if (firstChunk.contactPoint.y < 0)
            {
                rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.1f * firstChunk.vel.x).normalized;
                firstChunk.vel.x *= 0.7f;
            }

            lastGlimmerTime += (lastGlimmerTime > 80) ? 0 : 1;
            lastGlimmer = glimmer;
            if (glimmer == 0f && lastGlimmerTime > 80 && Random.value < 0.033f && grabbedBy.Count == 0)
                glimmer = 1 - 0.25f * Random.value;

            if (ModManager.MSC && MoreSlugcats.MMF.cfgCreatureSense.Value && room.world.game.IsStorySession)
            {
                MoreSlugcats.PersistentObjectTracker tracker = new MoreSlugcats.PersistentObjectTracker(abstractPhysicalObject);
                Map map = room.world.game.cameras[0].hud.map;
                if (!map.mapData.objectTrackers.Any(tr => tr.obj.ID == abstractPhysicalObject.ID && tr.obj.type == Register.Nectar))
                    map.addTracker(tracker);
                else if (map.mapData.objectTrackers.Any(tr => tr.obj.Room != abstractPhysicalObject.Room && tr.obj.ID == abstractPhysicalObject.ID && tr.obj.type == Register.Nectar))
                    map.removeTracker(tracker);
            }

            lastSwallow = swallowed;
            bool flag = grabbedBy.Count > 0 && grabbedBy[0]?.grabber is Player player && player.input[0].pckp &&
                ((player.grasps[0]?.grabbed is Nectar nectar && nectar.abstractPhysicalObject.ID == abstractPhysicalObject.ID) ||
                (player.grasps[1]?.grabbed is Nectar && (player.grasps[0] == null || !(player.grasps[0].grabbed is IPlayerEdible))));
            swallowed = SwallowedChange(swallowed, flag);
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos.Tile));
            rotation = Custom.RNV();
            lastRotation = rotation;
        }

        public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            base.TerrainImpact(chunk, direction, speed, firstContact);
            if (firstContact && speed > 3f)
                room.PlaySound(SoundID.Slime_Mold_Terrain_Impact, firstChunk, false, Custom.LerpMap(speed, 0f, 8f, 0.2f, 1f), 1f);
        }

        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);
            foreach (BodyChunk chunk in otherObject.bodyChunks)
                chunk.vel *= 0.8f;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            float rd = (180f / 8f * jellySprite + ((jellySprite < 3) ? 0 : 10 * (-0.5f * jellySprite + 3))) * Mathf.PI / 180f;
            sLeaser.sprites = new FSprite[]
            {
                new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["WaterNut"] },
                new FSprite($"Cicada{jellySprite}head") { anchorX = 0.5f - 0.18f * Mathf.Sin(rd), anchorY =  0.5f - 0.18f * Mathf.Cos(rd), alpha = 0.8f },
                new FSprite("DangleFruit2A") { anchorY = 0.7f, anchorX = 0.6f },
                new FSprite("JetFishEyeA") { anchorY = 0.8f, alpha = 0.7f },
                new FSprite("Futile_White") { alpha = 0f, shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"] },
            };
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

            sLeaser.sprites[0].x = pos.x - camPos.x;
            sLeaser.sprites[0].y = pos.y - camPos.y;
            sLeaser.sprites[0].rotation = Custom.VecToDeg(rt);
            sLeaser.sprites[1].x = pos.x - camPos.x;
            sLeaser.sprites[1].y = pos.y - camPos.y;
            sLeaser.sprites[1].rotation = Custom.VecToDeg(rt);
            sLeaser.sprites[2].x = pos.x - camPos.x;
            sLeaser.sprites[2].y = pos.y - camPos.y;
            sLeaser.sprites[2].rotation = 140f + Custom.VecToDeg(rt);
            sLeaser.sprites[3].x = pos.x - camPos.x - 3f;
            sLeaser.sprites[3].y = pos.y - camPos.y + 4f;
            sLeaser.sprites[3].rotation = 120f;
            sLeaser.sprites[4].x = pos.x - camPos.x - 3f;
            sLeaser.sprites[4].y = pos.y - camPos.y + 4f;

            if (swallowed < 1f)
            {
                float num = Mathf.Lerp(lastSwallow, swallowed, timeStacker);
                sLeaser.sprites[0].scale = num;
                sLeaser.sprites[1].scaleX = 1.3f * num;
                sLeaser.sprites[1].scaleY = num;
                sLeaser.sprites[2].scale = num;
                sLeaser.sprites[3].scaleX = 0.85f * num;
                sLeaser.sprites[3].scaleY = 0.65f * num;
                sLeaser.sprites[4].scale = 1.1f * num;
            }
            else
            {
                sLeaser.sprites[0].scale = 1f;
                sLeaser.sprites[1].scaleX = 1.3f;
                sLeaser.sprites[1].scaleY = 1f;
                sLeaser.sprites[2].scale = 1f;
                sLeaser.sprites[3].scaleX = 0.85f;
                sLeaser.sprites[3].scaleY = 0.65f;
                sLeaser.sprites[4].scale = 1.1f;
            }

            if (glimmer != 0f)
            {
                float num = Mathf.Lerp(lastGlimmer, glimmer, timeStacker);
                sLeaser.sprites[4].color = Color.Lerp(Color.Lerp(color, Color.white, num), rCam.currentPalette.blackColor, 0.33f * Random.value);
                sLeaser.sprites[4].alpha = Mathf.Clamp01(Mathf.Lerp(Mathf.Lerp((0.7f - num) * (Random.value + 0.5f), num, num), 0f, darkness) * 1.75f);
                sLeaser.sprites[3].color = Color.Lerp(color, blinkColor, sLeaser.sprites[4].alpha);
                glimmer *= (glimmer < 0.3f) ? 0 : 0.8f;
            }
            else
            {
                sLeaser.sprites[4].alpha = 0f;
                sLeaser.sprites[3].color = Color.Lerp(Color.white, rCam.currentPalette.blackColor, darkness);
            }

            if (blink > 0 && Random.value < 0.5f)
                sLeaser.sprites[1].color = blinkColor;
            else sLeaser.sprites[1].color = color;
            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            color = Color.Lerp(new Color(1f, 0.8f, 0.8f), palette.blackColor, darkness);
            sLeaser.sprites[0].color = color;
            sLeaser.sprites[2].color = Color.Lerp(new Color(0.9f, 0.5f, 0.5f), palette.blackColor, darkness);
            sLeaser.sprites[3].color = Color.Lerp(Color.white, palette.blackColor, darkness);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = newContatiner ?? rCam.ReturnFContainer("Items");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
            rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[4]);
        }

        public void BitByPlayer(Creature.Grasp grasp, bool eu)
        {
            room.PlaySound(SoundID.Slugcat_Bite_Slime_Mold, firstChunk.pos);
            firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
            (grasp.grabber as Player).ObjectEaten(this);
            (grasp.grabber as Player).Stun(500 + (int)(Random.value * 100));
            grasp.Release();
            Destroy();
        }

        public void ThrowByPlayer() { }

        public static float SwallowedChange(float swallowed, bool growth)
        {
            if (swallowed == 1f && !growth)
                return swallowed;
            float x = Mathf.Sqrt(5) * (1 - Mathf.Sqrt(swallowed));
            x += growth ? 0.025f : -0.025f;
            x = Mathf.Clamp01(x);
            swallowed = Mathf.Clamp(0.2f * Mathf.Pow(x - Mathf.Sqrt(5), 2), 0.3f, 1f);
            return (swallowed > 0.99f) ? 1f : swallowed;
        }

        public void ChangeMode(Mode newMode)
        {
            if (newMode == mode)
                return;
            if (newMode == Mode.Thrown || newMode == Mode.Free)
                ChangeCollisionLayer(1);
            else if (newMode == Mode.StuckInWall || newMode == Mode.StuckInCreature)
                ChangeCollisionLayer(0);
            mode = newMode;
        }

        public int BitesLeft => 1;
        public int FoodPoints => 0;
        public bool Edible => true;
        public bool AutomaticPickUp => true;
        public Mode mode;
        public Vector2 rotation;
        public Vector2 lastRotation;
        public float darkness;
        public float lastDarkness;
        public float glimmer;
        public float lastGlimmer;
        public float swallowed;
        public float lastSwallow;
        public int lastGlimmerTime;
        public int jellySprite;
        public bool tracked;
    }
}
