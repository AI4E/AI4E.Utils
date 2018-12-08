using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AI4E.Utils.ObjectClone.Test.TestTypes
{
    [Serializable]
    public class GenericClass<T>
    {
        public T Item1;

        public readonly T Item2;

        public GenericClass(T item1, T item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }
}
