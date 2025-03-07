using System.Diagnostics;

namespace SB.Core
{
    using VS = VisualStudio;
    public class LINK : ILinker
    {
        public LINK(string ExePath, Dictionary<string, string?> Env)
        {
            VCEnvVariables = Env;
            MSVCVersion = Version.Parse(VCEnvVariables["VCToolsVersion"]);
            this.ExePath = ExePath;

            if (!File.Exists(ExePath))
                throw new ArgumentException($"LINK: ExePath: {ExePath} is not an existed absolute path!");
        }

        public LinkResult Link(IArgumentDriver Driver)
        {
            var AllArgsDict = Driver.CalculateArguments();
            var AllArgsList = AllArgsDict.Values.SelectMany(x => x).ToList();

            var InputFiles = Driver.Arguments["Inputs"] as ArgumentList<string>;
            var OutputFile = Driver.Arguments["Output"] as string;
            var cxDepFilePath = Driver.Arguments["DependFile"] as string;
            Depend.OnChanged(cxDepFilePath, (Depend depend) =>
            {
                Process linker = new Process
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
                    linker.StartInfo.Environment.Add(kvp.Key, kvp.Value);
                }
                linker.Start();
                linker.WaitForExit();

                depend.ExternalFiles.AddRange(OutputFile);
            }, new List<string>(InputFiles), AllArgsList);

            return new LinkResult
            {
                TargetFile = AllArgsDict["Output"][0],
                PDBFile = Driver.Arguments.TryGetValue("PDB", out var args) ? args as string : ""
            };
        }

        public Version Version => MSVCVersion;

        public readonly Dictionary<string, string?> VCEnvVariables;
        private readonly Version MSVCVersion;
        private readonly string ExePath;
    }
}
