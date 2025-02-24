using Microsoft.Extensions.FileSystemGlobbing;

namespace SB.Core
{
    public static partial class VisualStudio
    {
        private static bool hasFoundVCVars = false;
        private static List<string> foundVCVars = new List<string>();
        public static List<string> FindVCVars()
        {
            if (!hasFoundVCVars)
            {
                Console.WriteLine("Write Once");
                var matcher = new Matcher();
                matcher.AddIncludePatterns(new[] { "./**/VC/Auxiliary/Build/vcvarsall.bat" });
                foreach (var Disk in Windows.EnumLogicalDrives())
                {
                    var searchDirectory = $"{Disk}:/Program Files/Microsoft Visual Studio";
                    IEnumerable<string> matchingFiles = matcher.GetResultsInFullPath(searchDirectory);
                    foreach (string file in matchingFiles)
                    {
                        foundVCVars.Add(file);
                    }
                }
                hasFoundVCVars = true;
            }
            return foundVCVars;
        }
    }
}