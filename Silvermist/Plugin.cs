using BepInEx;
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
        public string modPath;
        public void Awake()
        {
            //Mod enable / disable
            On.RainWorld.OnModsInit += delegate (On.RainWorld.orig_OnModsInit orig, RainWorld self)
            {
                orig(self);
                Register.RegisterValues();
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                    if (mod.id == GUID)
                        modPath = mod.path;
            };
            On.RainWorld.OnModsDisabled += delegate (On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
            {
                orig(self, newlyDisabledMods);
                if (newlyDisabledMods.Any(mod => mod.id == GUID))
                    Register.UnregisterValues();
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
                if (self.type == Register.Nectar)
                    self.realizedObject = new Nectar(self);
            };
            On.Player.Grabability += delegate (On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
            {
                if (obj is Nectar)
                    return Player.ObjectGrabability.OneHand;
                return orig(self, obj);
            };
            On.Player.GrabUpdate += Player_GrabUpdate;

            //Icon
            On.ItemSymbol.SpriteNameForItem += (On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData) => (itemType == Register.Nectar) ? "Symbol_Nectar" : orig(itemType, intData);
            On.ItemSymbol.ColorForItem += (On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData) => (itemType == Register.Nectar) ? new Color(0.93f, 0.56f, 0.53f) : orig(itemType, intData);
        }

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
