namespace System
{
    public static class CopyToArraySpanExtension
    {
        public static T[] CopyToArray<T>(in this ReadOnlySpan<T> span)
        {
            var result = new T[span.Length];
            span.CopyTo(result);
            return result;
        }

        public static T[] CopyToArray<T>(in this ReadOnlyMemory<T> memory)
        {
            return memory.Span.CopyToArray();
        }
    }
}
