namespace SB.Core
{
    public struct CompileResult
    {
        public string ObjectFile { get; init; }
        public string PDBFile { get; init; }
        public HashSet<string> IncludeFiles { get; init; }
        public HashSet<string> ImportModules { get; init; }
        public bool isRestored { get; init; }
    }

    public interface ICompiler
    {
        public Version Version { get; }
        public Task<CompileResult> Compile(IArgumentDriver Driver);
    }
}
