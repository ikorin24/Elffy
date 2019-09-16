using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DefinitionGenerator
{
    public static class ModifiedChecker
    {
        private static readonly HashAlgorithm _hashProvider = new SHA1CryptoServiceProvider();
        public const string HashType = "SHA1";

        public static bool IsModified(string definitionFile, string generatedFile, out string hash)
        {
            if(definitionFile == null) { throw new ArgumentNullException(nameof(definitionFile)); }
            if(generatedFile == null) { throw new ArgumentNullException(nameof(generatedFile)); }
            string currentHash = default;
            hash = GetFileHash(definitionFile);
            if(File.Exists(generatedFile) == false) {
                return true;
            }
            using(var reader = new StreamReader(generatedFile)) {
                while(!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    if(line.Contains("hash:")) {
                        currentHash = line.Split(new[] { '=' }).Last().Trim();
                        break;
                    }
                }
            }
            return hash != currentHash;
        }

        public static string GetFileHash(string filename)
        {
            if(filename == null) { throw new ArgumentNullException(nameof(filename)); }
            using(var stream = File.OpenRead(filename)) {
                return string.Join("", _hashProvider.ComputeHash(stream).Select(b => b.ToString("x2")));
            }
        }
    }
}
