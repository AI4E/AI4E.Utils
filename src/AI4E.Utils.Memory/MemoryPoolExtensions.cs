using AI4E.Utils;

namespace System.Buffers
{
    public static class AI4EUtilsMemoryMemoryPoolExtensions
    {
        public static SlicedMemoryOwner<T> RentExact<T>(this MemoryPool<T> memoryPool, int length)
        {
#pragma warning disable CA1062
            var memoryOwner = memoryPool.Rent(length);
#pragma warning restore CA1062
            try
            {
                return new SlicedMemoryOwner<T>(memoryOwner, start: 0, length);
            }
            catch
            {
                memoryOwner.Dispose();
                throw;
            }
        }
    }
}
