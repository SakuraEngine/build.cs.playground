namespace SB.Core
{
    public interface IToolchain
    {
        public Task<bool> Initialize();
        public Version Version { get; }
        public ICompiler Compiler { get; }
        public IArchiver Archiver { get; }
        public ILinker Linker { get; }
        public string BuildTempPath { get;  }
    }
}
