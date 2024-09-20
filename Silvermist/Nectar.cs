using RWCustom;
using System.Linq;
using UnityEngine;
using Mode = Weapon.Mode;

namespace Silvermist
{
    public class Nectar : PlayerCarryableItem, IDrawable, IPlayerEdible
    {
        public BodyChunk StuckInChunk => stuckInObject.bodyChunks[stuckInChunkIndex];
        public Mode mode;
        public PhysicalObject stuckInObject;
        public Vector2 rotation, lastRotation;
        public Vector2? attachmentPos;
        public float darkness, lastDarkness, glimmer, lastGlimmer, swallowed, lastSwallow, diving, lastDiving;
        public int lastGlimmerTime, jellySprite, releaseCounter, stuckInChunkIndex, timeSinceAttachment;

        public Nectar(AbstractPhysicalObject abstr) : base(abstr)
        {
            bodyChunks = [ new BodyChunk(this, 0, Vector2.zero, 5, 0.05f) ];
            bodyChunkConnections = [];
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
                rotation = Custom.PerpendicularVector(Custom.DirVec(firstChunk.pos, grabbedBy[0].grabber.mainBodyChunk.pos));
                rotation.y = Mathf.Abs(rotation.y);
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

            lastSwallow = swallowed;
            bool flag = grabbedBy.Count > 0 && grabbedBy[0]?.grabber is Player player && player.input[0].pckp &&
                ((player.grasps[0]?.grabbed is Nectar nectar && nectar.abstractPhysicalObject.ID == abstractPhysicalObject.ID) ||
                (player.grasps[1]?.grabbed is Nectar && (player.grasps[0] == null || player.grasps[0].grabbed is not IPlayerEdible)));
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
                SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(null, room, pos, ref vector, 1f, 1, null, false);
                if (floatRect != null && collisionResult.chunk != null)
                {
                    if (Vector2.Distance(pos, new Vector2(floatRect.Value.left, floatRect.Value.bottom)) < Vector2.Distance(pos, collisionResult.collisionPoint))
                        collisionResult.chunk = null;
                    else floatRect = null;
                }
                if (collisionResult.chunk != null && collisionResult.chunk.owner is Creature)
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
                            if (obj is Creature creature && creature.bodyChunks.Any(ch => Vector2.Distance(ch.pos, firstChunk.pos) < ((creature is Player) ? 10f : 20f)))
                            {
                                BodyChunk chunk = creature.bodyChunks.First(ch => Vector2.Distance(ch.pos, firstChunk.pos) < ((creature is Player) ? 10f : 20f));
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
                        if (Vector2.Distance(firstChunk.pos, StuckInChunk.lastPos) < distance && distance > 10f)
                        {
                            float num = ((StuckInChunk.owner is Player) ? 5f : 3.5f) * (distance / 30f);
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
                attachmentPos = firstChunk.pos;
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
            }
        }

        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);
            if (otherObject is not Creature)
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
            var jelSpr = Futile.atlasManager.GetElementWithName($"Cicada{jellySprite}head");
            Vector2 anchors = FCustom.TrimmedAnchors(jelSpr);

            sLeaser.sprites =
            [
                new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["WaterNut"] },
                new FSprite(jelSpr) { anchorX = anchors.x, anchorY = anchors.y, alpha = 0.8f },
                new FSprite("DangleFruit2A") { anchorY = 0.7f, anchorX = 0.6f },
                new FSprite("Futile_White") { alpha = 0f, shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"] },
            ];
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
            sLeaser.sprites[0].rotation = 0f;
            sLeaser.sprites[1].x = pos.x - camPos.x;
            sLeaser.sprites[1].y = pos.y - camPos.y;
            sLeaser.sprites[1].rotation = Custom.VecToDeg(rt);
            sLeaser.sprites[2].x = pos.x - camPos.x;
            sLeaser.sprites[2].y = pos.y - camPos.y;
            sLeaser.sprites[2].rotation = 140f + Custom.VecToDeg(rt);
            sLeaser.sprites[3].x = pos.x - camPos.x - 3f;
            sLeaser.sprites[3].y = pos.y - camPos.y + 4f;

            float num1 = Mathf.Lerp(lastSwallow, swallowed, timeStacker);
            float num2 = (mode == Mode.StuckInCreature) ? 0.8f : 1f;
            float num3 = Mathf.Lerp(lastDiving, diving, timeStacker);
            float subm = ((diving > 0) ? 1f + 0.25f * num3 : 1f) * num1 * num2;
            sLeaser.sprites[0].scale = subm;
            sLeaser.sprites[1].scaleX = 1.3f * subm;
            sLeaser.sprites[1].scaleY = 1.1f * subm;
            sLeaser.sprites[2].scale = subm;
            sLeaser.sprites[3].scale = 1.1f * subm;

            if (mode == Mode.StuckInWall && stuckInObject != null)
            {
                Vector2 v = StuckInChunk.pos - pos;
                float angR = Mathf.Acos(v.x / v.magnitude) * ((v.y > 0) ? 1 : -1f);
                float dist = Vector2.Distance(pos, StuckInChunk.pos);
                sLeaser.sprites[0].x = pos.x - camPos.x + 5f * Mathf.Cos(angR) * (dist / 30f);
                sLeaser.sprites[0].y = pos.y - camPos.y + 5f * Mathf.Sin(angR) * (dist / 30f);
                sLeaser.sprites[0].rotation = -angR * 180f / Mathf.PI;
                sLeaser.sprites[0].scaleX = 1f + (dist / 30f);
            }

            if (glimmer != 0f && diving == 0f)
            {
                float glim = Mathf.Lerp(lastGlimmer, glimmer, timeStacker);
                sLeaser.sprites[3].color = Color.Lerp(Color.Lerp(color, Color.white, glim), rCam.currentPalette.blackColor, 0.33f * Random.value);
                sLeaser.sprites[3].alpha = Mathf.Clamp01(Mathf.Lerp(Mathf.Lerp((0.7f - glim) * (Random.value + 0.5f), glim, glim), 0f, darkness) * 1.75f);
                glimmer *= (glimmer < 0.3f) ? 0 : 0.8f;
            }
            else sLeaser.sprites[3].alpha = 0f;

            if (blink > 0 && Random.value < 0.5f)
                sLeaser.sprites[1].color = blinkColor;
            else sLeaser.sprites[1].color = color;

            if (diving > 0f)
            {
                for (int i = 0; i < sLeaser.sprites.Length - 1; i++)
                    sLeaser.sprites[i].color = Custom.RGB2RGBA(Color.Lerp(sLeaser.sprites[i].color, rCam.PixelColorAtCoordinate(pos), num3), 1f - 0.5f * num3);
                sLeaser.sprites[0].isVisible = false;
            }
            if (slatedForDeletetion || rCam.room != room)
                sLeaser.CleanSpritesAndRemove();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            color = Color.Lerp(new Color(1f, 0.8f, 0.8f), palette.blackColor, darkness);
            sLeaser.sprites[0].color = color;
            sLeaser.sprites[2].color = Color.Lerp(new Color(0.9f, 0.5f, 0.5f), palette.blackColor, darkness);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Items");
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
            rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[3]);
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

        public override void RecreateSticksFromAbstract()
        {
            foreach (var apo in abstractPhysicalObject.stuckObjects)
            {
                if (apo is AbstractPhysicalObject.AbstractSpearStick abstr && abstr.Spear == abstractPhysicalObject && abstr.LodgedIn.realizedObject != null)
                {
                    stuckInObject = abstr.LodgedIn.realizedObject;
                    stuckInChunkIndex = abstr.chunk;
                    ChangeMode(Mode.StuckInCreature);
                }
            }
        }

        public void PulledOutOfStuckObject()
        {
            foreach (var stuckObject in abstractPhysicalObject.stuckObjects)
                if (stuckObject is AbstractPhysicalObject.AbstractSpearStick abstr && abstr.Spear == abstractPhysicalObject)
                {
                    abstr.Deactivate();
                    break;
                }
            stuckInObject = null;
            stuckInChunkIndex = 0;
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
            {
                room?.PlaySound(SoundID.Spear_Dislodged_From_Creature, firstChunk, false, 0.8f, 1.2f);
                PulledOutOfStuckObject();
            }
            if (newMode == Mode.StuckInCreature || newMode == Mode.StuckInWall)
                timeSinceAttachment = 0;
            else timeSinceAttachment = -1;

            if (newMode == Mode.Thrown || newMode == Mode.Free)
            {
                ChangeCollisionLayer(1);
                stuckInObject = null;
            }
            else ChangeCollisionLayer(0);
            if (newMode == Mode.StuckInWall)
            {
                if (mode == Mode.Free)
                    releaseCounter = 60;
                else releaseCounter = 0;
            }
            if (newMode != Mode.StuckInWall && newMode != Mode.StuckInCreature)
                attachmentPos = null;
            mode = newMode;
        }

        public int BitesLeft => 1;
        public int FoodPoints => 0;
        public bool Edible => true;
        public bool AutomaticPickUp => true;
    }
}
