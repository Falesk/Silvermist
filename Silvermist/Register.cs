namespace Silvermist
{
    public static class Register
    {
        public static void RegisterValues()
        {
            Nectar = new AbstractPhysicalObject.AbstractObjectType("Nectar", true);
        }

        public static void UnregisterValues()
        {
            AbstractPhysicalObject.AbstractObjectType nectar = Nectar;
            nectar?.Unregister();
            Nectar = null;
        }

        public static AbstractPhysicalObject.AbstractObjectType Nectar;
    }
}
