﻿using System.IO;

namespace SB.Core
{
    using ArgumentName = string;
    using VS = VisualStudio;
    public class LINKArgumentDriver : IArgumentDriver
    {
        [Argument] public string Arch(Architecture arch) => archMap.TryGetValue(arch, out var r) ? r : throw new ArgumentException($"Invalid architecture \"{arch}\" for LINK.exe!");
        static readonly Dictionary<Architecture, string> archMap = new Dictionary<Architecture, string> { { Architecture.X86, "/MACHINE:X86" }, { Architecture.X64, "/MACHINE:X64" }, { Architecture.ARM64, "/MACHINE:ARM64" } };

        [Argument] public string PDBMode(PDBMode mode) => (mode == Core.PDBMode.Disable) ? "/DEBUG:NONE" : "/DEBUG:FULL";
        
        [Argument] public string PDB(string path) => VS.CheckPath(path, false) ? $"/PDB:{path}" : throw new ArgumentException($"PDB value {path} is not a valid absolute path!");

        [Argument] public string RuntimeLibrary(string what) => VS.IsValidRT(what) ? what.StartsWith("MT") ? "/NODEFAULTLIB:msvcrt.lib" : "" : throw new ArgumentException($"Invalid argument \"{what}\" for MSVC RuntimeLibrary!");

        [Argument] public string TargetType(TargetType type) => typeMap.TryGetValue(type, out var t) ? t : throw new ArgumentException($"Invalid target type \"{type}\" for MSVC!");
        static readonly Dictionary<TargetType, string> typeMap = new Dictionary<TargetType, string> { { Core.TargetType.Static, "/LIB" }, { Core.TargetType.Dynamic, "/DLL" }, { Core.TargetType.Executable, "" } };

        [Argument] public string[]? LinkDirs(string[] dirs) => dirs.All(x => VS.CheckPath(x, false) ? true : throw new ArgumentException($"Invalid link dir {x}!")) ? dirs.Select(dir => $"/LIBPATH:{dir}").ToArray() : null;

        [Argument] public string[]? Inputs(string[] inputs) => inputs;

        [Argument] public string Output(string output) => VS.CheckFile(output, false) ? $"/OUT:{output}" : throw new ArgumentException($"Invalid output file path {output}!");

        [Argument] public string[]? WholeArchive(string[] libs) => libs.Select(lib => $"/WHOLEARCHIVE:{lib}").ToArray();

        public Dictionary<ArgumentName, object?[]?> Arguments { get; } = new Dictionary<ArgumentName, object?[]?>();
        public HashSet<string> RawArguments { get; } = new HashSet<string> { "/NOLOGO" };
    }
}