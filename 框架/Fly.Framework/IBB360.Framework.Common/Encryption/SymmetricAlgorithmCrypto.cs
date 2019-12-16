using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fly.Framework.Common
{
    internal abstract class SymmetricAlgorithmCrypto : ICrypto
    {
        private byte[] s_DesIV = new byte[] { 0x1d, 0x47, 0x22, 9, 0x41, 1, 0x61, 0x32 };
        private byte[] s_DesKey = new byte[] { 3, 0x4d, 0x54, 0x41, 0x45, 90, 0x55, 0x2c };

        protected abstract SymmetricAlgorithm CreateSymmetricAlgorithm();

        public string Decrypt(string encryptedBase64ConnectString)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                //stream.SetLength(0L);
                byte[] buffer = Convert.FromBase64String(encryptedBase64ConnectString);
                using (SymmetricAlgorithm crypto = CreateSymmetricAlgorithm())
                {
                    crypto.KeySize = 0x40;
                    using (CryptoStream stream2 = new CryptoStream(stream, crypto.CreateDecryptor(s_DesKey, s_DesIV), CryptoStreamMode.Write))
                    {
                        stream2.Write(buffer, 0, buffer.Length);
                        stream2.FlushFinalBlock();
                        stream.Flush();
                        stream.Seek(0L, SeekOrigin.Begin);
                        byte[] buffer2 = new byte[stream.Length];
                        stream.Read(buffer2, 0, buffer2.Length);
                        stream2.Close();
                        stream.Close();
                        return Encoding.Unicode.GetString(buffer2);
                    }
                }
            }
        }

        public string Encrypt(string plainConnectString)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                //stream.SetLength(0L);
                byte[] bytes = Encoding.Unicode.GetBytes(plainConnectString);
                using (SymmetricAlgorithm crypto = CreateSymmetricAlgorithm())
                {
                    using (CryptoStream stream2 = new CryptoStream(stream, crypto.CreateEncryptor(s_DesKey, s_DesIV), CryptoStreamMode.Write))
                    {
                        stream2.Write(bytes, 0, bytes.Length);
                        stream2.FlushFinalBlock();
                        stream.Flush();
                        stream.Seek(0L, SeekOrigin.Begin);
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        stream2.Close();
                        stream.Close();
                        return Convert.ToBase64String(buffer, 0, buffer.Length);
                    }
                }
            }
        }
    }

    internal class Sym_Aes : SymmetricAlgorithmCrypto
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            return new AesCryptoServiceProvider();
        }
    }

    internal class Sym_DES : SymmetricAlgorithmCrypto
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            return new DESCryptoServiceProvider();
        }
    }

    internal class Sym_RC2 : SymmetricAlgorithmCrypto
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            return new RC2CryptoServiceProvider();
        }
    }

    internal class Sym_Rijndael : SymmetricAlgorithmCrypto
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            return new RijndaelManaged();
        }
    }

    internal class Sym_TripleDES : SymmetricAlgorithmCrypto
    {
        protected override SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            return new TripleDESCryptoServiceProvider();
        }
    }
}
