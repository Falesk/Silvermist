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
            bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, Vector2.zero, 5, 0.05f) };
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
            releaseCounter = 0;
            timeSinceAttachment = -1;
            diving = 0f;
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
                lastGrabbed = grabbedBy[0].grabber;
                rotation = Custom.PerpendicularVector(Custom.DirVec(firstChunk.pos, grabbedBy[0].grabber.mainBodyChunk.pos));
                rotation.y = Mathf.Abs(rotation.y);
            }
            if (firstChunk.contactPoint.y < 0 && attachmentPos == null)
            {
                rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.1f * firstChunk.vel.x).normalized;
                firstChunk.vel.x *= 0.7f;
            }
            if (attachmentPos != null && mode == Mode.StuckInWall)
            {
                firstChunk.pos = attachmentPos.Value;
                firstChunk.vel *= 0f;
            }

            lastGlimmerTime += (lastGlimmerTime > 80) ? 0 : 1;
            lastGlimmer = glimmer;
            if (glimmer == 0f && lastGlimmerTime > 80 && Random.value < 0.033f && grabbedBy.Count == 0)
                glimmer = 1 - 0.25f * Random.value;

            lastDiving = diving;
            if (Submersion > 0.5f || diving != 0f)
                diving += 0.00834f;
            if (diving >= 1f)
                Destroy();

            if (timeSinceAttachment > -1)
                timeSinceAttachment += (timeSinceAttachment < 400) ? 1 : 0;

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

            if (mode == Mode.Free && grabbedBy.Count == 0)
            {
                firstChunk.lastLastPos = firstChunk.lastPos;
                firstChunk.lastPos = firstChunk.pos;
                Vector2 pos = firstChunk.pos;
                Vector2 vector = firstChunk.pos + firstChunk.vel;
                FloatRect? floatRect = SharedPhysics.ExactTerrainRayTrace(room, pos, vector);
                Vector2 vector2 = default;
                if (floatRect != null)
                    vector2 = new Vector2(floatRect.Value.left, floatRect.Value.bottom);
                SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(null, room, pos, ref vector, 1f, 1, lastGrabbed, false);
                if (floatRect != null && collisionResult.chunk != null)
                {
                    if (Vector2.Distance(pos, new Vector2(floatRect.Value.left, floatRect.Value.bottom)) < Vector2.Distance(pos, collisionResult.collisionPoint))
                        collisionResult.chunk = null;
                    else floatRect = null;
                }
                if (collisionResult.chunk != null && collisionResult.chunk.owner is Creature && collisionResult.chunk.owner != lastGrabbed)
                {
                    Droplets();
                    attachmentPos = vector2 + Custom.DirVec(vector2, pos) * 15f;
                    stuckInObject = collisionResult.chunk.owner;
                    ChangeMode(Mode.StuckInCreature);
                    stuckInChunkIndex = collisionResult.chunk.index;
                }
            }

            if (mode == Mode.StuckInWall)
            {
                releaseCounter -= (releaseCounter > 0) ? 1 : 0;
                if (releaseCounter == 0)
                {
                    if (stuckInObject == null)
                    {
                        foreach (PhysicalObject obj in room.physicalObjects[1])
                        {
                            if (obj is Creature creature && creature.bodyChunks.Any(ch => Vector2.Distance(ch.pos, firstChunk.pos) < 10f))
                            {
                                BodyChunk chunk = creature.bodyChunks.First(ch => Vector2.Distance(ch.pos, firstChunk.pos) < 10f);
                                stuckInObject = creature;
                                stuckInChunkIndex = chunk.index;
                                room.PlaySound(SoundID.Pole_Mimic_Grab_Player, firstChunk, false, 0.8f, 1.2f);
                                break;
                            }
                        }
                    }
                    else
                    {
                        float distance = Vector2.Distance(firstChunk.pos, StuckInChunk.pos);
                        if (Vector2.Distance(firstChunk.pos, StuckInChunk.lastPos) < distance && distance > ((StuckInChunk.owner is Player) ? 10f : 20f))
                        {
                            float num = (StuckInChunk.owner is Player) ? 1.75f * Mathf.Sqrt(distance) - 5f :  Mathf.Sqrt(distance) - 2;
                            StuckInChunk.vel += Custom.DirVec(StuckInChunk.pos, firstChunk.pos) * num;
                            if (Random.value < 0.1f * (distance / 30f))
                                room.AddObject(new WaterDrip(Vector2.Lerp(firstChunk.pos, StuckInChunk.pos, Random.value), Vector2.zero, false));
                        }
                        if (distance > 30f)
                        {
                            if (Random.value < 0.5f)
                            {
                                Droplets();
                                float deg = Custom.VecToDeg(StuckInChunk.vel);
                                attachmentPos = Custom.DegToVec(Random.Range(deg - 15f, deg + 15f)) * StuckInChunk.rad * Random.value;
                                ChangeMode(Mode.StuckInCreature);
                            }
                            else stuckInObject = null;
                        }
                    }
                }
            }
            else if (mode == Mode.StuckInCreature)
            {
                if ((stuckInObject as Creature).enteringShortCut != null || stuckInObject.slatedForDeletetion || (timeSinceAttachment > 200 && Random.value < 0.01f * (timeSinceAttachment / 400f)))
                {
                    ChangeMode(Mode.Free);
                    return;
                }
                StuckInChunk.vel.x *= 0.85f;
                firstChunk.vel = StuckInChunk.vel;
                firstChunk.MoveWithOtherObject(eu, StuckInChunk, attachmentPos.Value);
            }
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
            if (firstContact && mode == Mode.Thrown && grabbedBy.Count == 0)
            {
                if (speed > 3f)
                    Droplets();
                attachmentPos = firstChunk.pos + direction.ToVector2().normalized;
                ChangeMode(Mode.StuckInWall);
            }
            else if (mode == Mode.Free && grabbedBy.Count == 0)
            {
                Droplets();
                ChangeMode(Mode.StuckInWall);
            }
        }

        public void Droplets()
        {
            if (room.BeingViewed)
            {
                for (int i = 0; i < 4 + Random.Range(0, 5); i++)
                    room.AddObject(new WaterDrip(firstChunk.pos, -firstChunk.vel * Random.value * 0.5f + Custom.DegToVec(360f * Random.value) * firstChunk.vel.magnitude * Random.value * 0.5f, false));
                room.PlaySound(SoundID.Slime_Mold_Terrain_Impact, firstChunk, false, Custom.LerpMap(10 * Random.value, 0f, 8f, 0.2f, 1f), 1f);
                Debug.Log("Droplets");
            }
        }

        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);
            if (!(otherObject is Creature))
                return;
            if (mode == Mode.Thrown)
            {
                Droplets();
                stuckInObject = otherObject;
                stuckInChunkIndex = otherChunk;
                ChangeMode(Mode.StuckInCreature);
            }
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

            float num = Mathf.Lerp(lastSwallow, swallowed, timeStacker);
            float num2 = (mode == Mode.StuckInCreature) ? 0.8f : 1f;
            float num3 = Mathf.Lerp(lastDiving, diving, timeStacker);
            float subm = (diving > 0) ? 1f + 0.25f * num3 : 1f;
            sLeaser.sprites[0].scale = num * num2 * subm;
            sLeaser.sprites[1].scaleX = 1.3f * num * num2 * subm;
            sLeaser.sprites[1].scaleY = num * num2 * subm;
            sLeaser.sprites[2].scale = num * num2 * subm;
            sLeaser.sprites[3].scaleX = 0.85f * num * num2 * subm;
            sLeaser.sprites[3].scaleY = 0.65f * num * num2 * subm;
            sLeaser.sprites[4].scale = 1.1f * num * num2 * subm;

            if (glimmer != 0f)
            {
                float glim = Mathf.Lerp(lastGlimmer, glimmer, timeStacker);
                sLeaser.sprites[4].color = Color.Lerp(Color.Lerp(color, Color.white, glim), rCam.currentPalette.blackColor, 0.33f * Random.value);
                sLeaser.sprites[4].alpha = Mathf.Clamp01(Mathf.Lerp(Mathf.Lerp((0.7f - glim) * (Random.value + 0.5f), glim, glim), 0f, darkness) * 1.75f);
                sLeaser.sprites[3].color = Color.Lerp(color, blinkColor, sLeaser.sprites[4].alpha);
                glimmer *= (glimmer < 0.3f) ? 0 : 0.8f;
            }
            else
            {
                sLeaser.sprites[4].alpha = 0f;
                sLeaser.sprites[3].color = Color.Lerp(Color.white, rCam.currentPalette.blackColor, darkness);
            }
            if (darkness != lastDarkness || diving > 0)
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            if (blink > 0 && Random.value < 0.5f)
                sLeaser.sprites[1].color = blinkColor;
            else sLeaser.sprites[1].color = color;
            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            color = Color.Lerp(new Color(1f, 0.8f, 0.8f), palette.blackColor, darkness);
            //if (diving > 0)
            //{
            //    color.a = 1f - diving;
            //    Debug.Log(sLeaser.sprites[1].color.ToString());
            //}
            sLeaser.sprites[0].color = color;
            sLeaser.sprites[2].color = Color.Lerp(new Color(0.9f, 0.5f, 0.5f), palette.blackColor, darkness);
            sLeaser.sprites[3].color = Color.Lerp(Color.white, palette.blackColor, darkness);

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                float a = (i == 1) ? 0.8f : (i == 3) ? 0.7f : 1f;
                sLeaser.sprites[i].alpha = a * (1f - diving);
            }
            sLeaser.sprites[0].isVisible = diving == 0;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = newContatiner ?? rCam.ReturnFContainer("Items");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
            if (diving == 0f)
                rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
            else rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[4]);
        }

        public void BitByPlayer(Creature.Grasp grasp, bool eu)
        {
            room.PlaySound(SoundID.Slugcat_Bite_Slime_Mold, firstChunk.pos);
            firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
            (grasp.grabber as Player).ObjectEaten(this);
            (grasp.grabber as Player).Stun(400 + (int)(Random.value * 100));
            grasp.Release();
            Destroy();
        }

        public void ThrowByPlayer() => ChangeMode(Mode.Thrown);

        public override void PickedUp(Creature upPicker)
        {
            base.PickedUp(upPicker);
            ChangeMode(Mode.Free);
        }

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
            if (mode == Mode.StuckInCreature)
                stuckInObject = null;
            if (newMode == Mode.StuckInCreature || newMode == Mode.StuckInWall)
                timeSinceAttachment = 0;
            else timeSinceAttachment = -1;

            if (newMode == Mode.Thrown || newMode == Mode.Free)
                ChangeCollisionLayer(1);
            else ChangeCollisionLayer(0);
            if (newMode == Mode.StuckInWall)
            {
                if (mode == Mode.Free)
                    releaseCounter = 60;
                else releaseCounter = 0;
                attachmentPos = attachmentPos ?? firstChunk.pos;
            }
            if (newMode != Mode.StuckInWall && newMode != Mode.StuckInCreature)
                attachmentPos = null;
            mode = newMode;
        }

        public int BitesLeft => 1;
        public int FoodPoints => 0;
        public bool Edible => true;
        public bool AutomaticPickUp => true;
        public BodyChunk StuckInChunk => stuckInObject.bodyChunks[stuckInChunkIndex];
        public Mode mode;
        public Vector2 rotation;
        public Vector2 lastRotation;
        public Vector2? attachmentPos;
        public PhysicalObject stuckInObject;
        public PhysicalObject lastGrabbed;
        public float darkness;
        public float lastDarkness;
        public float glimmer;
        public float lastGlimmer;
        public float swallowed;
        public float lastSwallow;
        public float diving;
        public float lastDiving;
        public int lastGlimmerTime;
        public int jellySprite;
        public int releaseCounter;
        public int stuckInChunkIndex;
        public int timeSinceAttachment;
        public bool tracked;
    }
}
