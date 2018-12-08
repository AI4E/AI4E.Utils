using System;

namespace AI4E.Utils.ObjectClone.Test
{
    public static class CopyFunctionSelection
    {
        public static Func<object, object> CopyMethod;

        static CopyFunctionSelection()
        {
            CopyMethod = (obj) => ObjectExtension.DeepClone(obj);
            //CopyMethod = DeepCopyByReflection.Copy;
            //CopyMethod = DeepCopyBySerialization.DeepClone;
        }
    }
}
