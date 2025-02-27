﻿namespace SB.Core
{
    public struct LinkResult
    {
        public string TargetFile { get; init; }
        public string PDBFile { get; init; }
    }

    public interface ILinker
    {
        public Version Version { get; }
        public Task<LinkResult> Link(IArgumentDriver Driver);
    }
}
