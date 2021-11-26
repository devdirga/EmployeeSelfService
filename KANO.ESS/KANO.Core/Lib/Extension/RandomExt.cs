using System;

namespace KANO.Core.Lib.Extension
{
    public static class RandomExt
    {
        public static UInt64 NextUInt64(this Random rnd)
        {
            var buffer = new byte[sizeof(Int64)];
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }
    }
}
