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
    }
}
