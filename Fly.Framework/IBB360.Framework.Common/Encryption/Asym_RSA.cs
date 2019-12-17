using System;
using System.Security.Cryptography;
using System.Text;

namespace Fly.Framework.Common
{
    internal class Asym_RSA : ICrypto
    {
        private string m_RsaKey = "<RSAKeyValue><Modulus>otCy4XrCWmRvntxbTASxkJGb8TlV3sRD5rxJCvOjJnjrKv7Yl/q8joeh+yMmglYe7RFZd1P7Q3mtld2c/B1ks7j8zbH1qhpWO/cY8zF5U4r5cLob9/WIMCmuxyzSuVBHmQA+1i4HFE2C1ooVgYXSi5M1kCZYmN7Yu5602n8Tt5k=</Modulus><Exponent>AQAB</Exponent><P>w/FQM171HTmAIVMfee7G8jIJT0eg5DTfvfIpwwjcJ6UfIQtkU1LzC+A1Ij/EMbkKw1r9W9AXewwPKfB0qMKN5w==</P><Q>1LgJLCxL2Az9Cen2F84FLVmNLg5VlxqyCLyiuCz3XzczVMfRwwB1UziCgHarwoxdi2WAuAlA2DGmZggGulfefw==</Q><DP>d/pN0nbFfdSUmVMthdroZKqwuqOwZ6vciJE8cxj8vSXFTtWL4915xv7NaiBDgPK+HWqcklhz2DtFGbgLpr4iZw==</DP><DQ>CwQk5Xa9zsiNajAFoKH4vqp+lz4CzLqDMdSjEKqzfOjc7a0TfefOK6snhwOeTYr7ZTayfdVs2EVp+qq7vnbCfQ==</DQ><InverseQ>C5rv3iSOJJ5hd30UjfzLvD4Jy8TDR+FwJGUYBo+c4CaRFQLYBLw839FlIYcVkVsgDO8LomcBo9gzFBuMU4fyoA==</InverseQ><D>AuZzmUXSunUTJYhIhz66KJh/Z5+dPCrQ1TmOle30sdW+xdt+R87E/XqRbX2ZADXA6hLzpG2DJBbByDg6kj8cl3oXiN+C9Im4qxRkjG3kMaFXQ2dVVTQ0V/xTEcfu9iTm9UqEukUenTYE/xuHlP+IieB8f8Yc+cbC2MVgbToWI1U=</D></RSAKeyValue>";

        public string Decrypt(string encryptedBase64ConnectString)
        {
            byte[] rgb = Convert.FromBase64String(encryptedBase64ConnectString);
            CspParameters cspParams = new CspParameters();
            cspParams.Flags = CspProviderFlags.UseMachineKeyStore;
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider(cspParams);
            provider.FromXmlString(this.m_RsaKey);
            byte[] bytes = provider.Decrypt(rgb, false);
            return Encoding.Unicode.GetString(bytes);
        }

        public string Encrypt(string plainConnectString)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(plainConnectString);
            CspParameters cspParams = new CspParameters();
            cspParams.Flags = CspProviderFlags.UseMachineKeyStore;
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider(cspParams);
            provider.FromXmlString(this.m_RsaKey);
            byte[] inArray = provider.Encrypt(bytes, false);
            return Convert.ToBase64String(inArray, 0, inArray.Length);
        }
    }
}

