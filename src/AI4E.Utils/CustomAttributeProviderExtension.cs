using System;
using System.Reflection;

namespace AI4E.Utils
{
    public static class CustomAttributeProviderExtension
    {
        public static bool IsDefined<TCustomAttribute>(this ICustomAttributeProvider attributeProvider, bool inherit = false) 
            where TCustomAttribute : Attribute
        {
            if (attributeProvider == null)
                throw new ArgumentNullException(nameof(attributeProvider));

            return attributeProvider.IsDefined(typeof(TCustomAttribute), inherit);
        }

        public static TCustomAttribute[] GetCustomAttributes<TCustomAttribute>(this ICustomAttributeProvider attributeProvider, bool inherit = false)
            where TCustomAttribute : Attribute
        {
            return (TCustomAttribute[])attributeProvider.GetCustomAttributes(typeof(TCustomAttribute), inherit);
        }
    }
}
