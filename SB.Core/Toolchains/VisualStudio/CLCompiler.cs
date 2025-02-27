using System.Diagnostics;

namespace SB.Core
{
    using VS = VisualStudio;
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
        public CLCompiler(string ExePath, string TempPath, Dictionary<string, string?> Env)
        {
            VCEnvVariables = Env;
            // TODO: THIS IS WRONG! WE NEED TO PARSE FROM CL.exe! FIX IT.
            CLVersion = Version.Parse(VCEnvVariables["VCToolsVersion"]);
            this.ExePath = ExePath;
            this.TempPath = TempPath;

            if (!File.Exists(ExePath))
                throw new ArgumentException($"CLCompiler: ExePath: {ExePath} is not an existed absolute path!");
            if (!Path.IsPathFullyQualified(TempPath))
                throw new ArgumentException($"CLCompiler: TempPath: {TempPath} is not an valid absolute path!");
            if (!Directory.Exists(TempPath))
                throw new ArgumentException($"CLCompiler: TempPath: {TempPath} is not an existed absolute path!");
        }

        public Version Version => CLVersion;

        public async Task<CompileResult> Compile(IArgumentDriver Driver)
        {
            return await Task.Run(() =>
            {
                var AllArgsDict = Driver.CalculateArguments();
                var AllArgsList = AllArgsDict.Values.SelectMany(x => x).ToList();

                var SourceFile = AllArgsDict["Source"][0] as string;
                var ObjectFile = Driver.Arguments["Object"][0] as string;

                var cxDepFilePath = Path.Combine(TempPath, VS.GetUniqueTempFileName(SourceFile, "cxx.compile.deps", "json", AllArgsList));
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
                    var clDeps = Json.Deserialize<CLDependencies>(File.ReadAllText(clDepFilePath));

                    depend.ExternalFiles.AddRange(clDeps.Data.Includes);
                    depend.ExternalFiles.Add(ObjectFile);
                }, new List<string> { SourceFile }, AllArgsList);

                return new CompileResult
                {
                    ObjectFile = ObjectFile,
                    PDBFile = Driver.Arguments.TryGetValue("PDB", out var args) ? args[0] as string : ""
                };
            });
        }

        public readonly Dictionary<string, string?> VCEnvVariables;
        private readonly Version CLVersion;
        private readonly string ExePath;
        private readonly string TempPath;
    }
}
