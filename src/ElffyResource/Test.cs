using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;

namespace ElffyResource
{
    public class Test
    {
        private const string FILE = "hoge.mp4";
        private const string COMPRESSED_FILE = "hoge.dat";
        private const string OUT_FILE = "out.mp4";
        private const string PASSWORD = "password";
        private static readonly byte[] _salt = new byte[16]
        {
            0x6A, 0x10, 0xAF, 0xAC,
            0x45, 0xA6, 0xA7, 0xCF,
            0x69, 0x41, 0xE5, 0x0B,
            0xF6, 0x95, 0xBD, 0x99,
        };
        private const int AES_KEY_SIZE = 16;
        private const int IV_SIZE = 16;
        private static readonly byte[] _buf = new byte[1024 * 1024];

        public static void Build()
        {
            if(File.Exists(COMPRESSED_FILE)) {
                File.Delete(COMPRESSED_FILE);
            }
            GenerateAesKey(PASSWORD, out var aesKey, out var iv);
            using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = aesKey, IV = iv })
            using(var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using(var outfs = File.OpenWrite(COMPRESSED_FILE))
            using(var cs = new CryptoStream(outfs, encryptor, CryptoStreamMode.Write))
            using(var infs = File.OpenRead(FILE)) {
                while(true) {
                    var readlen = infs.Read(_buf, 0, _buf.Length);
                    if(readlen == 0) { break; ; }
                    cs.Write(_buf, 0, readlen);
                }
            }
        }

        public static void Decompress()
        {
            if(File.Exists(OUT_FILE)) {
                File.Delete(OUT_FILE);
            }
            GenerateAesKey(PASSWORD, out var aesKey, out var iv);
            using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = aesKey, IV = iv })
            using(var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using(var outfs = File.OpenWrite(OUT_FILE))
            using(var cs = new CryptoStream(outfs, decryptor, CryptoStreamMode.Write))
            using(var infs = File.OpenRead(COMPRESSED_FILE)) {
                while(true) {
                    var readlen = infs.Read(_buf, 0, _buf.Length);
                    if(readlen == 0) { return; }
                    cs.Write(_buf, 0, readlen);
                }
            }
        }

        #region GenerateAesKey
        /// <summary>パスワードからAESの鍵を生成します</summary>
        /// <param name="password">パスワード</param>
        /// <param name="aesKey">生成されたAESの鍵</param>
        /// <param name="iv">生成されたAESの初期化ベクトル</param>
        private static void GenerateAesKey(string password, out byte[] aesKey, out byte[] iv)
        {
            // パスワードにsaltを付与してaesの鍵を生成します。ついでに初期化ベクトルも一緒に生成します。
            using(var deriveBytes = new Rfc2898DeriveBytes(password, _salt)) {
                aesKey = deriveBytes.GetBytes(AES_KEY_SIZE);
                iv = deriveBytes.GetBytes(IV_SIZE);
            }
        }
        #endregion
    }
}
