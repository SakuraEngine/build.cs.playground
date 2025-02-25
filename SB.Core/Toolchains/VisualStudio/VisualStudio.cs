using Microsoft.Extensions.FileSystemGlobbing;
using System.Diagnostics;

namespace SB.Core
{
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

        public ICompiler Compiler()
        {
            return null;
        }

        public IArchiver Archiver()
        {
            return null;
        }

        public ILinker Linker()
        {
            return null;
        }

        private void FindVCVars(Version version)
        {
            Console.WriteLine("Write Once");
            var matcher = new Matcher();
            matcher.AddIncludePatterns(new[] { "./**/VC/Auxiliary/Build/vcvarsall.bat" });
            foreach (var Disk in Windows.EnumLogicalDrives())
            {
                var VersionPostfix = (version == Version.V2022) ? "/2022" : "";
                var searchDirectory = $"{Disk}:/Program Files/Microsoft Visual Studio{VersionPostfix}";
                IEnumerable<string> matchingFiles = matcher.GetResultsInFullPath(searchDirectory);
                foreach (string file in matchingFiles)
                {
                    VCVarsAllBat = file;
                    VCVars64Bat = file.Replace("vcvarsall", "vcvars64");
                }
            }
        }

        private void DumpVCVars()
        {
            var oldEnvPath = Path.Combine(Path.GetTempPath(), "vcvars_prev.txt");
            var newEnvPath = Path.Combine(Path.GetTempPath(), "vcvars_post.txt");
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = $"/c set > \"{oldEnvPath}\" && \"{VCVars64Bat}\" && set > \"{newEnvPath}\"";
            cmd.Start();
            cmd.WaitForExit();

            var oldEnv = EnvReader.Load(oldEnvPath);
            var newEnv = EnvReader.Load(newEnvPath);
            foreach (var oldVar in oldEnv)
            {
                if (newEnv.ContainsKey(oldVar.Key) && newEnv[oldVar.Key] == oldEnv[oldVar.Key])
                    newEnv.Remove(oldVar.Key);
            }
            VCEnvVariables = newEnv;
        }

        public readonly Version version;
        public string? VCVarsAllBat { get; private set; }
        public string? VCVars64Bat { get; private set; }
        public Dictionary<string, string?> VCEnvVariables { get; private set; }
    }
}