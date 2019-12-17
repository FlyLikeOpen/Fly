using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fly.Framework.Common
{
    public static class Crypto
    {
        public static ICrypto GetCrypto(CryptoType algorithm)
        {
            switch (algorithm)
            {
                case CryptoType.Aes:
                    return new Sym_Aes();

                case CryptoType.DES:
                    return new Sym_DES();

                case CryptoType.RC2:
                    return new Sym_RC2();

                case CryptoType.Rijndael:
                    return new Sym_Rijndael();

                case CryptoType.TripleDES:
                    return new Sym_TripleDES();

                case CryptoType.RSA:
                    return new Asym_RSA();

                case CryptoType.MD5:
                    return new Hash_MD5();

                case CryptoType.SHA1:
                    return new Hash_SHA1();

                case CryptoType.SHA256:
                    return new Hash_SHA256();

                case CryptoType.SHA512:
                    return new Hash_SHA512();
            }
            return null;
        }

        public static int SaltLengthForSign = 8;

        public static string SignData(string plainString)
        {
            int saltLength = SaltLengthForSign;
            byte[] saltValue = new byte[saltLength];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(saltValue);
            return SignData(plainString, saltValue);
        }

        public static bool VerifyData(string plainString, string signedData)
        {
            int saltLength = SaltLengthForSign;
            byte[] encryptedBytes = Convert.FromBase64String(signedData);
            byte[] saltValue = new byte[saltLength];
            Array.Copy(encryptedBytes, encryptedBytes.Length - saltLength, saltValue, 0, saltLength);
            return signedData == SignData(plainString, saltValue);
        }

        private static string SignData(string plainString, byte[] saltValue)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainString);

            byte[] saltedPlainBytes = new byte[plainBytes.Length + saltValue.Length];

            plainBytes.CopyTo(saltedPlainBytes, 0);
            saltValue.CopyTo(saltedPlainBytes, plainBytes.Length);

            byte[] saltedencryptedBytes;
            using (MD5 md5 = MD5.Create())
            {
                saltedencryptedBytes = md5.ComputeHash(saltedPlainBytes);
            }
            byte[] encryptedBytes = new byte[saltedencryptedBytes.Length + saltValue.Length];
            saltedencryptedBytes.CopyTo(encryptedBytes, 0);
            saltValue.CopyTo(encryptedBytes, saltedencryptedBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }
    }

    public enum CryptoType
    {
        Aes,
        DES,
        RC2,
        Rijndael,
        TripleDES,
        RSA,
        MD5,
        SHA1,
        SHA256,
        SHA512
    }
}
