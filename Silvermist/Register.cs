namespace Silvermist
{
    public static class Register
    {
        public static class ObjectTypes
        {
            public static void RegisterValues()
            {
                Nectar = new AbstractPhysicalObject.AbstractObjectType("Nectar", true);
                Silvermist = new AbstractPhysicalObject.AbstractObjectType("Silvermist", true);
            }

            public static void UnregisterValues()
            {
                AbstractPhysicalObject.AbstractObjectType nectar = Nectar;
                nectar?.Unregister();
                Nectar = null;

                AbstractPhysicalObject.AbstractObjectType silvermist = Silvermist;
                silvermist?.Unregister();
                Silvermist = null;
            }

            public static AbstractPhysicalObject.AbstractObjectType Nectar;
            public static AbstractPhysicalObject.AbstractObjectType Silvermist;
        }

        public static class OracleConvos
        {
            public static void RegisterValues()
            {
                Nectar = new SLOracleBehaviorHasMark.MiscItemType("Nectar", true);
            }

            public static void UnregisterValues()
            {
                SLOracleBehaviorHasMark.MiscItemType nectar = Nectar;
                nectar?.Unregister();
                Nectar = null;
            }

            public static SLOracleBehaviorHasMark.MiscItemType Nectar;
        }

        public static class PlacedObjectTypes
        {
            public static void RegisterValues()
            {
                Silvermist = new PlacedObject.Type("Silvermist", true);
            }

            public static void UnregisterValues()
            {
                PlacedObject.Type silvermist = Silvermist;
                silvermist?.Unregister();
                Silvermist = null;
            }

            public static PlacedObject.Type Silvermist;
        }

        public static void RegisterAll()
        {
            ObjectTypes.RegisterValues();
            OracleConvos.RegisterValues();
            PlacedObjectTypes.RegisterValues();
        }

        public static void UnregisterAll()
        {
            ObjectTypes.UnregisterValues();
            OracleConvos.UnregisterValues();
            PlacedObjectTypes.UnregisterValues();
        }
    }
}
