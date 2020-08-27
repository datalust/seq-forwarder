using Microsoft.AspNetCore.DataProtection;

namespace Seq.Forwarder.Cryptography
{
    /// <summary>
    /// Echoes <see cref="IDataProtector"/> and should eventually be replaced by
    /// that interface.
    /// </summary>
    public interface IStringDataProtector
    {
        string Protect(string value);
        string Unprotect(string @protected);
    }
}
