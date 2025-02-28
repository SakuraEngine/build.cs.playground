namespace SB.Core
{
    public struct CompileResult
    {
        public string ObjectFile { get; init; }
        public string PDBFile { get; init; }
    }

    public interface ICompiler
    {
        public Version Version { get; }
        public CompileResult Compile(IArgumentDriver Driver);
    }
}
