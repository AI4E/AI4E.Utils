using System;

namespace AI4E.Utils
{
    public static class TypeExtension
    {
        public static bool IsArray(this Type type)
        {
            return type.IsArray;
        }

        public static bool IsDelegate(this Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }

        public static bool IsClassOtherThanString(this Type type)
        {
            return !type.IsValueType && type != typeof(string);
        }
    }
}
