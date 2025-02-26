namespace SB.Core
{
    using DotNetArch = System.Runtime.InteropServices.Architecture;
    using DotnetRuntimeInfo = System.Runtime.InteropServices.RuntimeInformation;

    public class HostInformation
    {
        public static readonly Dictionary<DotNetArch, Architecture> archMap = new Dictionary<DotNetArch, Architecture>
        {
            { DotNetArch.X86, Architecture.X86 },
            { DotNetArch.X64, Architecture.X64 },
            { DotNetArch.Arm64, Architecture.ARM64 }
        };
        public static Architecture HostArch => archMap.TryGetValue(DotnetRuntimeInfo.OSArchitecture, out var arch) ? arch : throw new Exception($"Unsupported Platform Architecture: {DotnetRuntimeInfo.OSArchitecture}");
    }
}
