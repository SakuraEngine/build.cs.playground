using Microsoft.Extensions.FileSystemGlobbing;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace SB.Core
{
    public partial class VisualStudio : IToolchain
    {
        public VisualStudio(int VSVersion = 2022, Architecture? HostArch = null, Architecture? TargetArch = null)
        {
            this.VSVersion = VSVersion;
            this.HostArch = HostArch ?? HostInformation.HostArch;
            this.TargetArch = TargetArch ?? HostInformation.HostArch;
        }

        public async Task<bool> Initialize()
        {
            return await Task.Run<bool>(() =>
            {
                FindVCVars();
                RunVCVars();
                return true;
            });
        }

        public Version Version => ToolchainVersion;
        public ICompiler Compiler => CLCC;
        public ILinker Linker => LINK;
        public string BuildTempPath => Directory.CreateDirectory(Path.Combine(SourceLocation.BuildTempPath, this.Version.ToString())).FullName;

        private void FindVCVars()
        {
            Console.WriteLine($"Checking for Visual Studio {VSVersion} Toolchain...");
            var matcher = new Matcher();
            matcher.AddIncludePatterns(new[] { "./**/VC/Auxiliary/Build/vcvarsall.bat" });
            foreach (var Disk in Windows.EnumLogicalDrives())
            {
                var VersionPostfix = (VSVersion == 2022) ? "/2022" : "";
                var searchDirectory = $"{Disk}:/Program Files/Microsoft Visual Studio{VersionPostfix}";
                IEnumerable<string> matchingFiles = matcher.GetResultsInFullPath(searchDirectory);
                foreach (string file in matchingFiles)
                {
                    VCVarsAllBat = file;
                    VCVars64Bat = file.Replace("vcvarsall", "vcvars64");
                }
            }
        }

        static readonly Dictionary<Architecture, string> archStringMap = new Dictionary<Architecture, string> { { Architecture.X86, "x86" }, { Architecture.X64, "x64" }, { Architecture.ARM64, "arm64" } };
        private void RunVCVars()
        {
            string ArchString = (TargetArch == HostArch) ? archStringMap[TargetArch] : $"{archStringMap[HostArch]}_{archStringMap[TargetArch]}";
            var oldEnvPath = Path.Combine(Path.GetTempPath(), $"vcvars_{VSVersion}_prev_{HostArch}_{TargetArch}.txt");
            var newEnvPath = Path.Combine(Path.GetTempPath(), $"vcvars_{VSVersion}_post_{HostArch}_{TargetArch}.txt");

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = $"/c set > \"{oldEnvPath}\" && \"{VCVarsAllBat}\" {ArchString} && set > \"{newEnvPath}\"";
            cmd.Start();
            cmd.WaitForExit();

            var oldEnv = EnvReader.Load(oldEnvPath);
            VCEnvVariables = EnvReader.Load(newEnvPath);
            // Preprocess: cull old env variables
            foreach (var oldVar in oldEnv)
            {
                if (VCEnvVariables.ContainsKey(oldVar.Key) && VCEnvVariables[oldVar.Key] == oldEnv[oldVar.Key])
                    VCEnvVariables.Remove(oldVar.Key);
            }
            // Preprocess: cull user env variables
            var vcPaths = VCEnvVariables["Path"].Split(';').ToHashSet();
            vcPaths.ExceptWith(oldEnv["Path"].Split(';').ToHashSet());
            VCEnvVariables["Path"] = string.Join(";", vcPaths);
            // Enum all files and pick usable tools
            foreach (var path in vcPaths)
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    if (Path.GetFileName(file) == "cl.exe")
                        CLCCPath = file;
                    if (Path.GetFileName(file) == "link.exe")
                        LINKPath = file;
                }
            }

            ToolchainVersion = Version.Parse(VCEnvVariables["VSCMD_VER"]);
            CLCC = new CLCompiler(CLCCPath, BuildTempPath, VCEnvVariables);
            LINK = new LINK(LINKPath, BuildTempPath, VCEnvVariables);
        }
        
        private Version ToolchainVersion;
        public readonly int VSVersion;
        public readonly Architecture HostArch;
        public readonly Architecture TargetArch;

        public string? VCVarsAllBat { get; private set; }
        public string? VCVars64Bat { get; private set; }
        public Dictionary<string, string?> VCEnvVariables { get; private set; }
        public CLCompiler CLCC { get; private set; }
        public LINK LINK { get; private set; }
        public string CLCCPath { get; private set; }
        public string LINKPath { get; private set; }

        #region HelpersForTools
        public static string GetUniqueTempFileName(string File, string Hint, string Extension, IEnumerable<string> Args)
        {
            var SHA = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(String.Join("", Args)));
            return $"{Path.GetFileName(File)}.{Hint}.{Convert.ToHexString(SHA)}.{Extension}";
        }
        public static bool CheckPath(string P, bool MustExist) => Path.IsPathFullyQualified(P) && (!MustExist || Directory.Exists(P));
        public static bool CheckFile(string P, bool MustExist) => Path.IsPathFullyQualified(P) && (!MustExist || File.Exists(P));
        public static bool IsValidRT(string what) => ValidRuntimeArguments.Contains(what);
        private static readonly string[] ValidRuntimeArguments = ["MT", "MTd", "MD", "MDd"];
        #endregion
    }
}