using Microsoft.Extensions.FileSystemGlobbing;
using System.Diagnostics;

namespace SB.Core
{
    public interface IToolchain
    {
        public Task<bool> Initialize();
    }

    public partial class VisualStudio : IToolchain
    {
        public enum Version
        {
            V2022
        };

        public VisualStudio(Version version = Version.V2022)
        {
            this.version = version;
        }

        public async Task<bool> Initialize()
        {
            return await Task.Run<bool>(() =>
            {
                FindVCVars(version);
                DumpVCVars();
                return true;
            });
        }

        private void FindVCVars(Version version)
        {
            Console.WriteLine("Write Once");
            var matcher = new Matcher();
            matcher.AddIncludePatterns(new[] { "./**/VC/Auxiliary/Build/vcvars64.bat" });
            foreach (var Disk in Windows.EnumLogicalDrives())
            {
                var VersionPostfix = (version == Version.V2022) ? "/2022" : "";
                var searchDirectory = $"{Disk}:/Program Files/Microsoft Visual Studio{VersionPostfix}";
                IEnumerable<string> matchingFiles = matcher.GetResultsInFullPath(searchDirectory);
                foreach (string file in matchingFiles)
                {
                    VCVars64Bat = file;
                }
            }
        }

        private void DumpVCVars()
        {
            var oldEnv = Path.Combine(Path.GetTempPath(), "vcvars_prev.txt");
            var newEnv = Path.Combine(Path.GetTempPath(), "vcvars_post.txt");
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = $"/c set > \"{oldEnv}\" && \"{VCVars64Bat}\" && set > \"{newEnv}\"";
            cmd.Start();
            cmd.WaitForExit();
        }

        public readonly Version version;
        public string? VCVars64Bat { get; private set; }
    }
}