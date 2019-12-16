using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Fly.Framework.Common
{
    internal abstract class HashAlgorithmCrypto : ICrypto
    {
        protected abstract HashAlgorithm CreateHashAlgorithm();
 
        public string Decrypt(string encryptedBase64String)
        {
            throw new ApplicationException("Nonreversible algorithm.");
        }

        public string Encrypt(string plainString)
        {
            using (HashAlgorithm h = CreateHashAlgorithm())
            {
                byte[] hashValue = h.ComputeHash(Encoding.UTF8.GetBytes(plainString));
                return Convert.ToBase64String(hashValue);
            }
        }
    }

    internal class Hash_MD5 : HashAlgorithmCrypto
    {
        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return MD5.Create();
        }
    }

    internal class Hash_SHA1 : HashAlgorithmCrypto
    {
        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return SHA1.Create();
        }
    }

    internal class Hash_SHA256 : HashAlgorithmCrypto
    {
        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return SHA256.Create();
        }
    }

    internal class Hash_SHA512 : HashAlgorithmCrypto
    {
        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return SHA512.Create();
        }
    }
}
