namespace SB.Core
{
    public interface IToolchain
    {
        public Task<bool> Initialize();
        public ICompiler Compiler();
        public IArchiver Archiver();
        public ILinker Linker();
    }
}
