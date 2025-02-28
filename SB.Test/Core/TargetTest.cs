using System.Diagnostics;
using SB.Core;

namespace SB.Test
{
    [TestClass]
    public sealed class TargetTest
    {
        [TestMethod]
        public void TestTargetArgs()
        {
            BuildSystem.Target("HelloWorld")
                .TargetType(TargetType.Executable)
                .Exception(true)
                .RTTI(true)
                .RuntimeLibrary("MD")
                .OptimizationLevel(OptimizationLevel.O0)
                .Defines("A")
                .Defines("B=A")
                .Defines("C=B", "D=C")
                .WarningLevel(MSVCWarningLevel.W0)
                .AddFiles("C:/*.cpp")
                .AddFiles("C:/c.cpp")
                .AddFiles("d.cpp")
                .AddFiles("./../../e.cpp");

            IArgumentDriver CLDriver = new CLArgumentDriver();
            IArgumentDriver LINKDriver = new LINKArgumentDriver();
            CLDriver.AddArguments(BuildSystem.GetTarget("HelloWorld").Arguments);
            LINKDriver.AddArguments(BuildSystem.GetTarget("HelloWorld").Arguments); 
            
            var CLArgs = CLDriver.CalculateArguments();
            var LINKArgs = LINKDriver.CalculateArguments();
        }
    }
}
