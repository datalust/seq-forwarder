using System;
using System.Linq;
using System.Security.Cryptography;

namespace Seq.Forwarder.Tests.Support
{
    static class Some
    {
        static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static byte[] Bytes(int count)
        {
            var bytes = new byte[count];
            Rng.GetBytes(bytes);
            return bytes;
        }

        public static string ApiKey()
        {
            return string.Join("", Bytes(8).Select(v => v.ToString("x2")).ToArray());
        }
    }
}
