using System;
using System.Diagnostics;
using System.Text.Json;

namespace SB.Core
{
    public struct CLDependencies
    {
        public struct DepData
        {
            public string Source { get; set; }
            public string ProvidedModule { get; set; }
            public string[] Includes { get; set; }
            public string[] ImportedModules { get; set; }
            public string[] ImportedHeaderUnits { get; set; }
        }
        public Version Version { get; set; }
        public DepData Data { get; set; }
    }

    public class CLCompiler : ICompiler
    {
        public CLCompiler(string Path, Dictionary<string, string?> Env)
        {
            VCEnvVariables = Env;
            CLVersion = Version.Parse(VCEnvVariables["VCToolsVersion"]);
            ExePath = Path;
        }

        public Version Version => CLVersion;

        public async Task<CompileResult> Compile(IArgumentDriver Driver)
        {
            return await Task.Run(() =>
            {
                var AllArgs = Driver.CalculateArguments();

                Process compiler = new Process();
                compiler.StartInfo.FileName = ExePath;
                compiler.StartInfo.RedirectStandardInput = false;
                compiler.StartInfo.CreateNoWindow = false;
                compiler.StartInfo.UseShellExecute = false;
                compiler.StartInfo.Arguments = String.Join(" ", AllArgs.Values.SelectMany(x => x).ToArray());
                foreach (var kvp in VCEnvVariables)
                {
                    compiler.StartInfo.Environment.Add(kvp.Key, kvp.Value);
                }
                compiler.Start();
                compiler.WaitForExit();

                string clDepFile = File.ReadAllText(Driver.Arguments["SourceDependencies"][0] as string);
                var clDependencies = JsonSerializer.Deserialize<CLDependencies>(clDepFile);
                return new CompileResult
                {
                    ObjectFile = AllArgs["Source"][0],
                    IncludeFiles = new HashSet<string>(clDependencies.Data.Includes),
                    PDBFile = Driver.Arguments.TryGetValue("PDB", out var args) ? args[0] as string : "",
                    ImportModules = new HashSet<string>(clDependencies.Data.ImportedModules),
                    isRestored = false
                };
            });
        }

        public readonly Dictionary<string, string?> VCEnvVariables;
        private readonly Version CLVersion;
        private readonly string ExePath;
    }
}
