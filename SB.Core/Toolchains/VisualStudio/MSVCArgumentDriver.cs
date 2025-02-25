using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SB.Core
{
    public class MSVCArgumentDriver : IArgumentDriver
    {
        [Argument] public string Exception(bool Enable) => Enable ? "/EHsc" : "/EHsc-";

        [Argument] public string RuntimeLibrary(string what) => validRuntimeArguments.Contains(what) ? $"/{what}" : throw new ArgumentException($"Invalid argument \"{what}\" for MSVC RuntimeLibrary!");
        static readonly string[] validRuntimeArguments = ["MT", "MTd", "MD", "MDd"];

        [Argument] public string CppVersion(int what) => CppVersion(what.ToString());
        [Argument] public string CppVersion(string what) => cppVersionMap.TryGetValue(what.Replace("c++", "").Replace("C++", ""), out var r) ? r : throw new ArgumentException($"Invalid argument \"{what}\" for CppVersion!");
        static readonly Dictionary<string, string> cppVersionMap = new Dictionary<string, string> { { "11", "/std:c++11" }, { "14", "/std:c++14" }, { "17", "/std:c++17" }, { "20", "/std:c++20" }, { "23", "/std:c++23" }, { "latest", "/std:c++latest" } };

        [Argument] public string Arch(Architecture arch) => archMap.TryGetValue(arch, out var r) ? r : throw new ArgumentException($"Invalid architecture \"{arch}\" for MSVC!");
        static readonly Dictionary<Architecture, string> archMap = new Dictionary<Architecture, string> { { Architecture.X86, "" }, { Architecture.X64, "" }, { Architecture.ARM64, "" } };

        public Dictionary<string, object?[]?> Semantics { get; } = new Dictionary<string, object?[]?>();
        public HashSet<string> RawArguments { get; } = new HashSet<string> { "/c" };
    }
}