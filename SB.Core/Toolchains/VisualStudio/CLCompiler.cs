using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SB.Core
{
    public class CLCompiler : ICompiler
    {
        public CLCompiler(Dictionary<string, string?> Env)
        {
            VCEnvVariables = Env;
            CLVersion = Version.Parse(VCEnvVariables["VCToolsVersion"]);
        }

        public Version Version => CLVersion;

        public async Task<CompileResult> Compile(IArgumentDriver Driver) => new CompileResult();

        public readonly Dictionary<string, string?> VCEnvVariables;
        private readonly Version CLVersion; 
    }
}
