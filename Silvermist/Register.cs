namespace Silvermist
{
    public static class Register
    {
        public static void RegisterValues()
        {
            Nectar = new AbstractPhysicalObject.AbstractObjectType("Nectar", true);
            NectarConv = new SLOracleBehaviorHasMark.MiscItemType("NectarConv", true);
        }

        public static void UnregisterValues()
        {
            AbstractPhysicalObject.AbstractObjectType nectar = Nectar;
            nectar?.Unregister();
            Nectar = null;

            SLOracleBehaviorHasMark.MiscItemType nectarConv = NectarConv;
            nectarConv?.Unregister();
            NectarConv = null;
        }

        public static AbstractPhysicalObject.AbstractObjectType Nectar;
        public static SLOracleBehaviorHasMark.MiscItemType NectarConv;
    }
}
