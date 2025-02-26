namespace SB.Core
{
    public struct CompileResult
    {
        public string ObjectFile { get; }
        public string PDBFile { get; }
        public HashSet<string> IncludeFiles { get; }
        public HashSet<string> ImportModules { get; }
        public bool isRestored { get; }
    }

    public interface ICompiler
    {
        public Version Version { get; }
        public Task<CompileResult> Compile(IArgumentDriver Driver);
    }
}
