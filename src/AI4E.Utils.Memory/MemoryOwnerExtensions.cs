using AI4E.Utils;

namespace System.Buffers
{
    public static class AI4EUtilsMemoryMemoryOwnerExtensions
    {
        public static SlicedMemoryOwner<T> Slice<T>(this IMemoryOwner<T> memoryOwner, int start)
        {
            return new SlicedMemoryOwner<T>(memoryOwner, start);
        }

        public static SlicedMemoryOwner<T> Slice<T>(this IMemoryOwner<T> memoryOwner, int start, int length)
        {
            return new SlicedMemoryOwner<T>(memoryOwner, start, length);
        }
    }
}
