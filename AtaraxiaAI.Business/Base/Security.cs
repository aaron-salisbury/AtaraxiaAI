using System;
using System.Security.Cryptography;
using System.Text;

namespace AtaraxiaAI.Business.Base
{
    // https://qawithexperts.com/article/c-sharp/encrypt-password-decrypt-it-c-console-application-example/169
    internal class Security
    {
        //TODO: This security key should be very complex and random.
        private const string SECURITY_KEY = "ComplexKeyHere_12121";

        internal static string EncryptPlainTextToCipherText(string PlainText)
        {
            byte[] toEncryptedArray = Encoding.UTF8.GetBytes(PlainText);
            MD5 objMD5CryptoService = MD5.Create();
            byte[] securityKeyArray = objMD5CryptoService.ComputeHash(Encoding.UTF8.GetBytes(SECURITY_KEY));
            objMD5CryptoService.Clear();

            TripleDES objTripleDESCryptoService = TripleDES.Create();
            objTripleDESCryptoService.Key = securityKeyArray;
            objTripleDESCryptoService.Mode = CipherMode.ECB;
            objTripleDESCryptoService.Padding = PaddingMode.PKCS7;

            ICryptoTransform objCrytpoTransform = objTripleDESCryptoService.CreateEncryptor();
            byte[] resultArray = objCrytpoTransform.TransformFinalBlock(toEncryptedArray, 0, toEncryptedArray.Length);
            objTripleDESCryptoService.Clear();

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        internal static string DecryptCipherTextToPlainText(string CipherText)
        {
            byte[] toEncryptArray = Convert.FromBase64String(CipherText);
            MD5 objMD5CryptoService = MD5.Create();
            byte[] securityKeyArray = objMD5CryptoService.ComputeHash(Encoding.UTF8.GetBytes(SECURITY_KEY));
            objMD5CryptoService.Clear();

            TripleDES objTripleDESCryptoService = TripleDES.Create();
            objTripleDESCryptoService.Key = securityKeyArray;
            objTripleDESCryptoService.Mode = CipherMode.ECB;
            objTripleDESCryptoService.Padding = PaddingMode.PKCS7;

            ICryptoTransform objCrytpoTransform = objTripleDESCryptoService.CreateDecryptor();
            byte[] resultArray = objCrytpoTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            objTripleDESCryptoService.Clear();

            return Encoding.UTF8.GetString(resultArray);
        }
    }
}
