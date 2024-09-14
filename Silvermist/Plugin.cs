using BepInEx;
using DevInterface;
using System.Linq;
using UnityEngine;

namespace Silvermist
{
    [BepInPlugin(GUID, Name, Version)]
    class Plugin : BaseUnityPlugin
    {
        public const string GUID = "falesk.silvermist";
        public const string Name = "Silvermist";
        public const string Version = "1.0";
        public void Awake()
        {
            //Mod enable / disable
            On.RainWorld.OnModsInit += delegate (On.RainWorld.orig_OnModsInit orig, RainWorld self)
            {
                orig(self);
                Register.RegisterAll();
            };
            On.RainWorld.OnModsDisabled += delegate (On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
            {
                orig(self, newlyDisabledMods);
                if (newlyDisabledMods.Any(mod => mod.id == GUID))
                    Register.UnregisterAll();
            };
            On.RainWorld.LoadModResources += delegate (On.RainWorld.orig_LoadModResources orig, RainWorld self)
            {
                orig(self);
                Futile.atlasManager.LoadAtlas("assets/sprites");
            };
            On.RainWorld.UnloadResources += delegate (On.RainWorld.orig_UnloadResources orig, RainWorld self)
            {
                orig(self);
                Futile.atlasManager.UnloadAtlas("assets/sprites");
            };

            //Main
            On.AbstractPhysicalObject.Realize += delegate (On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
            {
                orig(self);
                if (self.type == Register.ObjectTypes.Nectar)
                    self.realizedObject = new Nectar(self);
                else if (self.type == Register.ObjectTypes.Silvermist)
                    self.realizedObject = new Silvermist(self);
                else if (self.type == Register.ObjectTypes.DebugObj)
                    self.realizedObject = new DebugObj(self);
            };
            On.Player.Grabability += delegate (On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
            {
                if (obj is Nectar nectar && nectar.diving == 0f)
                    return Player.ObjectGrabability.OneHand;
                return orig(self, obj);
            };
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += delegate (On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
            {
                orig(self);
                if (self.id == Conversation.ID.Moon_Misc_Item && self.describeItem == Register.OracleConvos.Nectar)
                {
                    if (self.myBehavior.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Russian)
                        self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Прес качат, бегит, анжуманя"), 0));
                    else self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Something about item"), 0));
                    return;
                }
            };
            On.SLOracleBehaviorHasMark.TypeOfMiscItem += (On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem) => (testItem is Nectar) ? Register.OracleConvos.Nectar : orig(self, testItem);

            //Icon
            On.ItemSymbol.SpriteNameForItem += delegate (On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
            {
                if (itemType == Register.ObjectTypes.Nectar)
                    return "Symbol_Nectar";
                if (itemType == Register.ObjectTypes.Silvermist)
                    return "Symbol_Silvermist";
                return orig(itemType, intData);
            };
            On.ItemSymbol.ColorForItem += delegate (On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
            {
                if (itemType == Register.ObjectTypes.Nectar)
                    return new Color(0.93f, 0.56f, 0.53f);
                if (itemType == Register.ObjectTypes.Silvermist)
                    return new Color(0.93f, 0.56f, 0.53f);
                if (itemType == Register.ObjectTypes.DebugObj)
                    return Color.black;
                return orig(itemType, intData);
            };

            //Placed Object
            On.PlacedObject.GenerateEmptyData += delegate (On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
            {
                orig(self);
                if (self.type == Register.PlacedObjectTypes.Silvermist)
                    self.data = new PlacedObject.ConsumableObjectData(self);
            };
            On.PlacedObject.ConsumableObjectData.ctor += delegate (On.PlacedObject.ConsumableObjectData.orig_ctor orig, PlacedObject.ConsumableObjectData self, PlacedObject owner)
            {
                if (owner.type == Register.PlacedObjectTypes.Silvermist)
                {
                    self.minRegen = 1;
                    self.maxRegen = 1;
                }
                else orig(self, owner);
            };
            On.DevInterface.ObjectsPage.CreateObjRep += delegate (On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
            {
                if (tp == Register.PlacedObjectTypes.Silvermist)
                {
                    if (pObj == null)
                    {
                        pObj = new PlacedObject(tp, null)
                        { pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + RWCustom.Custom.DegToVec(Random.value * 360f) * 0.2f };
                        self.RoomSettings.placedObjects.Add(pObj);
                    }
                    PlacedObjectRepresentation por = new ConsumableRepresentation(self.owner, tp.ToString() + "_Rep", self, pObj, tp.ToString());
                    self.tempNodes.Add(por);
                    self.subNodes.Add(por);
                }
                else orig(self, tp, pObj);
            };
            On.DevInterface.ObjectsPage.DevObjectGetCategoryFromPlacedType += delegate (On.DevInterface.ObjectsPage.orig_DevObjectGetCategoryFromPlacedType orig, ObjectsPage self, PlacedObject.Type type)
            {
                if (type == Register.PlacedObjectTypes.Silvermist)
                    return ObjectsPage.DevObjectCategories.Consumable;
                return orig(self, type);
            };
            //IL.Room.Loaded += Room_Loaded;
            On.Room.Loaded += Room_Loaded;
            On.AbstractConsumable.IsTypeConsumable += (On.AbstractConsumable.orig_IsTypeConsumable orig, AbstractPhysicalObject.AbstractObjectType type) => type == Register.ObjectTypes.Silvermist || orig(type);
        }

        private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            if (self.game == null)
                return;
            foreach (PlacedObject po in self.roomSettings.placedObjects)
            {
                if (po.active && po.type == Register.PlacedObjectTypes.Silvermist && self.abstractRoom.firstTimeRealized)
                {
                    AbstractConsumable abstr = new AbstractConsumable(
                        self.world,
                        Register.ObjectTypes.Silvermist,
                        null,
                        self.GetWorldCoordinate(po.pos),
                        self.game.GetNewID(),
                        self.abstractRoom.index,
                        self.roomSettings.placedObjects.IndexOf(po),
                        po.data as PlacedObject.ConsumableObjectData);
                    self.abstractRoom.entities.Add(abstr);
                }
            }
            orig(self);
        }

        //private void Room_Loaded(ILContext il)
        //{
        //    try
        //    {
        //        ILCursor c = new ILCursor(il);
        //        c.GotoNext(MoveType.After,
        //            x => x.MatchLdarg(0),
        //            x => x.MatchCallOrCallvirt(typeof(Room).GetMethod("get_abstractRoom")),
        //            x => x.MatchLdfld(typeof(AbstractRoom).GetField(nameof(AbstractRoom.entities))),
        //            x => x.MatchLdloc(62),
        //            x => x.MatchCallOrCallvirt(typeof(List<AbstractWorldEntity>).GetMethod(nameof(List<AbstractWorldEntity>.Add))),
        //            x => x.Match(OpCodes.Br)
        //            );

        //        int num = (int)(new ILCursor(il).GotoNext(MoveType.After, x => x.MatchLdloc(43)).Instrs[0].Operand);
        //        flag = num;

        //        c.MoveAfterLabels();
        //        c.Emit(OpCodes.Ldarg_0);

        //        c.EmitDelegate<Action<Room>>((Room self) =>
        //        {
        //            Debug.Log($"{num} - num");
        //            if (self.roomSettings.placedObjects[num].type == Register.PlacedObjectTypes.Silvermist)
        //            {
        //                PlacedObject po = self.roomSettings.placedObjects[num];
        //                AbstractPhysicalObject abstr = new AbstractPhysicalObject(self.world, Register.ObjectTypes.Silvermist, null, self.GetWorldCoordinate(po.pos), self.game.GetNewID());
        //                self.abstractRoom.entities.Add(abstr);
        //            }
        //        });
        //    }
        //    catch (Exception ex) { Debug.LogException(ex); }
        //}

        private void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (!ModManager.MMF || self.input[0].y == 0)
            {
                bool flag = self.wantToPickUp < 1 && (self.input[0].pckp || self.eatCounter <= 15) && self.Consious && RWCustom.Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 3.6f);
                bool flag2 = true;
                for (int i = 0; i < 2; i++)
                {
                    if (flag && self.eatCounter > 0 && self.grasps[i] != null && self.grasps[i].grabbed is Nectar && self.FoodInStomach < self.MaxFoodInStomach && (i == 0 || !(self.grasps[0]?.grabbed is IPlayerEdible)))
                        (self.graphicsModule as PlayerGraphics).BiteStruggle(i);
                    else if (!flag && flag2 && self.eatCounter < 40)
                    {
                        flag2 = false;
                        self.eatCounter++;
                    }
                }
            }
        }
    }
}
