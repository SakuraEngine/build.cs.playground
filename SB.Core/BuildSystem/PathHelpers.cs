using System.Text;
using System.Security.Cryptography;

namespace SB
{
    public static partial class BuildSystem
    {
        public static string GetUniqueTempFileName(string File, string Hint, string Extension, IEnumerable<string>? Args = null)
        {
            string FullIdentifier = File + (Args is null ? "" : String.Join("", Args));
            var SHA = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(FullIdentifier));
            return $"{Path.GetFileName(File)}.{Hint}.{Convert.ToHexString(SHA)}.{Extension}";
        }

        public static string GlobalTempPath { get; set; } = Path.GetTempPath();
    }
}