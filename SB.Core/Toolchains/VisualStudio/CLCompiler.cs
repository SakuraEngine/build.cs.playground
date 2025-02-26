using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

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
        public static string GetUniqueTempFileName(string File, string Hint, string Extension, IEnumerable<string> Args)
        {
            var SHA = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(String.Join("", Args)));
            return $"{Path.GetFileName(File)}.{Hint}.{Convert.ToHexString(SHA)}.{Extension}";
        }

        public CLCompiler(string ExePath, string TempPath, Dictionary<string, string?> Env)
        {
            VCEnvVariables = Env;
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

                var cxDepFilePath = Path.Combine(TempPath, GetUniqueTempFileName(SourceFile, "cxx.compile.deps", "json", AllArgsList));
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

                    depend.OutputFiles.AddRange(clDeps.Data.Includes);
                }, new List<string> { SourceFile }, AllArgsList);

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
        private readonly string TempPath;
    }
}
