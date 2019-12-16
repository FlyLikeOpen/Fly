using System;

namespace Fly.Framework.Common
{
    public interface ICrypto
    {
        string Decrypt(string encryptedBase64String);
        string Encrypt(string plainConnectString);
    }
}

