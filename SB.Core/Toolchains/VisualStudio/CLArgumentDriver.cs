using System.IO;

namespace SB.Core
{
    using ArgumentName = string;
    using VS = VisualStudio;
    public class CLArgumentDriver : IArgumentDriver
    {
        [TargetSetter] public string Exception(bool Enable) => Enable ? "/EHsc" : "/EHsc-";

        [TargetSetter] public string RuntimeLibrary(string what) => VS.IsValidRT(what) ? $"/{what}" : throw new ArgumentException($"Invalid argument \"{what}\" for MSVC RuntimeLibrary!");

        [TargetSetter] public string CppVersion(string what) => cppVersionMap.TryGetValue(what.Replace("c++", "").Replace("C++", ""), out var r) ? r : throw new ArgumentException($"Invalid argument \"{what}\" for CppVersion!");
        public static readonly Dictionary<string, string> cppVersionMap = new Dictionary<string, string> { { "11", "/std:c++11" }, { "14", "/std:c++14" }, { "17", "/std:c++17" }, { "20", "/std:c++20" }, { "23", "/std:c++23" }, { "latest", "/std:c++latest" } };

        [TargetSetter] public string SIMD(SIMDArchitecture simd) => $"/arch:{simd}".Replace("_", ".");

        [TargetSetter] public string WarningLevel(MSVCWarningLevel level) => $"/{level}";

        [TargetSetter] public string WarningAsError(bool v) => v ? "/WX" : "";

        [TargetSetter] public string OptimizationLevel(SB.Core.OptimizationLevel opt) => $"/{opt}".Replace("/O3", "/O2").Replace("/O0", "/Od");

        // for clang it's -ffp-model=[precise|fast|strict]
        [TargetSetter] public string FpModel(FpModel v) => $"/fp:{v}".ToLowerInvariant();

        [TargetSetter] public string[] Defines(ArgumentList<string> defines) => defines.Select(define => $"/D{define}").ToArray();

        [TargetSetter] public string[]? IncludeDirs(ArgumentList<string> dirs) => dirs.All(x => VS.CheckPath(x, true) ? true : throw new ArgumentException($"Invalid include dir {x}!")) ? dirs.Select(dir => $"/I{dir}").ToArray() : null;

        [TargetSetter] public string RTTI(bool v) => v ? "/GR" : "/GR-";

        [TargetSetter] public string Source(string path) => VS.CheckFile(path, true) ? $"{path}" : throw new ArgumentException($"Source value {path} is not an existed absolute path!");

        public string Arch(Architecture arch) => archMap.TryGetValue(arch, out var r) ? r : throw new ArgumentException($"Invalid architecture \"{arch}\" for MSVC CL.exe!");
        static readonly Dictionary<Architecture, string> archMap = new Dictionary<Architecture, string> { { Architecture.X86, "" }, { Architecture.X64, "" }, { Architecture.ARM64, "" } };

        public string Object(string path) => VS.CheckFile(path, false) ? $"/Fo{path}" : throw new ArgumentException($"Object value {path} is not a valid absolute path!");

        public string PDBMode(PDBMode mode) => (mode == SB.Core.PDBMode.Standalone) ? "/Zi" : (mode == SB.Core.PDBMode.Embed) ? "/Z7" : "";

        public string PDB(string path) => VS.CheckFile(path, false) ? $"/Fd{path}" : throw new ArgumentException($"PDB value {path} is not a valid absolute path!");

        public string SourceDependencies(string path) => VS.CheckFile(path, false) ? $"/sourceDependencies {path}" : throw new ArgumentException($"SourceDependencies value {path} is not a valid absolute path!");

        public Dictionary<ArgumentName, object?> Arguments { get; } = new Dictionary<ArgumentName, object?>();
        public HashSet<string> RawArguments { get; } = new HashSet<string> { "/c", "/nologo", "/cgthreads4", "/FC" };
        // /c: dont link while compiling, https://learn.microsoft.com/zh-cn/cpp/build/reference/c-compile-without-linking?view=msvc-170
        // /logo: dont show info to output stream, https://learn.microsoft.com/zh-cn/cpp/build/reference/nologo-suppress-startup-banner-c-cpp?view=msvc-170
        // /cgthreads4: multi-thread count for CL to use to codegen, https://learn.microsoft.com/zh-cn/cpp/build/reference/cgthreads-code-generation-threads?view=msvc-170
        // /O: https://learn.microsoft.com/en-us/cpp/build/reference/ox-full-optimization?view=msvc-170
        // DumpDependencies: https://learn.microsoft.com/en-us/cpp/build/reference/sourcedependencies?view=msvc-170
    }
}