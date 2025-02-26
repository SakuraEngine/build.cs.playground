using System;
using System.Collections.Immutable;
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
                var AllArgsDict = Driver.CalculateArguments();
                var AllArgsList = AllArgsDict.Values.SelectMany(x => x).ToList();
                var cxDepFilePath = "D:/SakuraEngine/SimpleCXX/main.cxx.compile.deps.json";

                var Files = new List<string> { AllArgsDict["Source"][0] as string };
                Depend.OnChanged(cxDepFilePath, (Depend depend) =>
                {
                    Process compiler = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ExePath,
                            RedirectStandardInput = false,
                            CreateNoWindow = false,
                            UseShellExecute = false,
                            Arguments = String.Join(" ", AllArgsList)
                        }
                    };
                    foreach (var kvp in VCEnvVariables)
                    {
                        compiler.StartInfo.Environment.Add(kvp.Key, kvp.Value);
                    }
                    compiler.Start();
                    compiler.WaitForExit();

                    var clDepFilePath = Driver.Arguments["SourceDependencies"][0] as string;
                    var clDeps = JsonSerializer.Deserialize<CLDependencies>(File.ReadAllText(clDepFilePath));

                    depend.OutputFiles.AddRange(clDeps.Data.Includes);
                }, Files, AllArgsList);

                return new CompileResult
                {
                    ObjectFile = AllArgsDict["Source"][0],
                    PDBFile = Driver.Arguments.TryGetValue("PDB", out var args) ? args[0] as string : ""
                };
            });
        }

        public readonly Dictionary<string, string?> VCEnvVariables;
        private readonly Version CLVersion;
        private readonly string ExePath;
    }
}
