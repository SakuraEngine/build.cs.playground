using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SB.Core
{
    using VS = VisualStudio;
    public struct CLDependenciesData
    {
        public string Source { get; set; }
        public string ProvidedModule { get; set; }
        public string[] Includes { get; set; }
        public string[] ImportedModules { get; set; }
        public string[] ImportedHeaderUnits { get; set; }
    }

    public struct CLDependencies
    {
        public Version Version { get; set; }
        public CLDependenciesData Data { get; set; }
    }

    public class CLCompiler : ICompiler
    {
        public CLCompiler(string ExePath, Dictionary<string, string?> Env)
        {
            VCEnvVariables = Env;
            this.ExePath = ExePath;

            if (!File.Exists(ExePath))
                throw new ArgumentException($"CLCompiler: ExePath: {ExePath} is not an existed absolute path!");

            this.CLVersionTask = Task.Run(() =>
            {
                Process compiler = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ExePath,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                compiler.Start();
                compiler.WaitForExit();
                // FUCK YOU MICROSOFT THIS IS WEIRD
                var Output = compiler.StandardError.ReadToEnd();
                Regex pattern = new Regex(@"\d+(\.\d+)+");
                CLVersion = Version.Parse(pattern.Match(Output).Value);
                Log.Information("CL.exe version ... {CLVersion}", CLVersion);
                return this.CLVersion;
            });
        }

        public CompileResult Compile(IArgumentDriver Driver)
        {
            var CompilerArgsDict = Driver.CalculateArguments();
            var CompilerArgsList = CompilerArgsDict.Values.SelectMany(x => x).ToList();
            var DependArgsList = CompilerArgsList.ToList();
            DependArgsList.Add($"ENV:VCToolsVersion={VCEnvVariables["VCToolsVersion"]}");
            DependArgsList.Add($"ENV:WindowsSDKVersion={VCEnvVariables["WindowsSDKVersion"]}");
            DependArgsList.Add($"ENV:WindowsSDKLibVersion={VCEnvVariables["WindowsSDKLibVersion"]}");
            DependArgsList.Add($"ENV:UCRTVersion={VCEnvVariables["UCRTVersion"]}");

            var SourceFile = Driver.Arguments["Source"] as string;
            var ObjectFile = Driver.Arguments["Object"] as string;
            var cxDepFilePath = Driver.Arguments["DependFile"] as string;
            Depend.OnChanged(cxDepFilePath, (Depend depend) =>
            {
                Process compiler = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ExePath,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = false,
                        UseShellExecute = false,
                        Arguments = String.Join(" ", CompilerArgsList)
                    }
                };
                foreach (var kvp in VCEnvVariables)
                {
                    compiler.StartInfo.Environment.Add(kvp.Key, kvp.Value);
                }
                compiler.Start();
                compiler.WaitForExit();

                // var ErrorInfo = compiler.StandardError.ReadToEnd();
                // FUCK YOU MICROSOFT THIS IS WEIRD
                var OutputInfo = compiler.StandardOutput.ReadToEnd();
                if (OutputInfo.Contains("fatal error"))
                {
                    throw new TaskFatalError($"CL.exe: {OutputInfo.Replace("\n", "")}");
                }
                else
                {
                    var clDepFilePath = Driver.Arguments["SourceDependencies"] as string;
                    var clDeps = Json.Deserialize<CLDependencies>(File.ReadAllText(clDepFilePath));
                    depend.ExternalFiles.AddRange(clDeps.Data.Includes);
                }

                depend.ExternalFiles.Add(ObjectFile);
            }, new List<string> { SourceFile }, DependArgsList);

            return new CompileResult
            {
                ObjectFile = ObjectFile,
                PDBFile = Driver.Arguments.TryGetValue("PDB", out var args) ? args as string : ""
            };
        }

        public Version Version
        {
            get
            {
                if (!CLVersionTask.IsCompleted)
                    CLVersionTask.Wait();
                return CLVersion;
            }
        }

        public readonly Dictionary<string, string?> VCEnvVariables;
        private readonly Task<Version> CLVersionTask;
        private Version CLVersion;
        private readonly string ExePath;
    }
}
