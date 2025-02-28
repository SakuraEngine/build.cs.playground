using System.Diagnostics;
using SB.Core;

namespace SB.Test
{
    [TestClass]
    public sealed class WindowsTest
    {
        VisualStudio vs = new VisualStudio(2022);
        
        public WindowsTest()
        {
            vs.Initialize().Wait(10000);
        }

        [TestMethod]
        public void TestListLogicDisks()
        {
            var drives = SB.Core.Windows.EnumLogicalDrives();
            foreach (var drive in drives)
            {
                Assert.IsTrue(Path.Exists($"{drive}:/"));
            }
        }        
        
        [TestMethod]
        public void TestToolset()
        {
            Assert.IsTrue(Path.Exists(vs.VCVars64Bat));
            Assert.IsTrue(vs.Compiler.Version.Build > 0);
            Assert.IsTrue(vs.Version.Build > 0);
        }

        [TestMethod]
        public void TestEnvironmentGetSet()
        {
            // System.Environment.GetEnvironmentVariables();
            System.Environment.SetEnvironmentVariable("Hello", "World");
            Assert.AreEqual(System.Environment.GetEnvironmentVariable("Hello"), "World");
        }

        [TestMethod]
        public void TestCompileArgDriver()
        {
            var TestFunction = (string Name, object Value, string Expected) => {
                var driver = new CLArgumentDriver() as IArgumentDriver;
                driver.Arguments.Add(Name, Value);

                var AllCalculatedVars = driver.CalculateArguments().Values.SelectMany(x => x).ToArray();
                var ArgumentsString = new HashSet<string>(AllCalculatedVars);

                var ExpectedArgs = new HashSet<string>(driver.RawArguments.Union(Expected.Split(" ")).ToArray());

                ArgumentsString.ExceptWith(ExpectedArgs);
                Assert.AreEqual(ArgumentsString.Count, 0);
            };

            TestFunction("Exception", true, "/EHsc");
            TestFunction("Exception", false, "/EHsc-");
            TestFunction("RuntimeLibrary", "MT", "/MT");
            TestFunction("RuntimeLibrary", "MTd", "/MTd");
            TestFunction("RuntimeLibrary", "MD", "/MD");
            TestFunction("RuntimeLibrary", "MDd", "/MDd");

            var TestCppVersion = (string version) =>
            {
                TestFunction("CppVersion", version, $"/std:c++{version}");
                TestFunction("CppVersion", $"c++{version}", $"/std:c++{version}");
                TestFunction("CppVersion", $"C++{version}", $"/std:c++{version}");
            };
            TestCppVersion("11");
            TestCppVersion("14");
            TestCppVersion("17");
            TestCppVersion("20");
            TestCppVersion("latest");

            TestFunction("Arch", Architecture.X86, ""); // for msvc arch setting is a null option

            TestFunction("SIMD", SIMDArchitecture.SSE2, "/arch:SSE2"); 
            TestFunction("SIMD", SIMDArchitecture.SSE4_2, "/arch:SSE4.2"); 
            TestFunction("SIMD", SIMDArchitecture.AVX, "/arch:AVX"); 
            TestFunction("SIMD", SIMDArchitecture.AVX512, "/arch:AVX512"); 
            TestFunction("SIMD", SIMDArchitecture.AVX10_1, "/arch:AVX10.1");

            TestFunction("PDBMode", PDBMode.Standalone, "/Zi");
            TestFunction("PDBMode", PDBMode.Embed, "/Z7");
            TestFunction("PDBMode", PDBMode.Disable, "");
            
            TestFunction("PDB", "C:/pdb.pdb", "/FdC:/pdb.pdb");

            TestFunction("WarningAsError", true, "/WX");
            TestFunction("WarningAsError", false, "");

            TestFunction("WarningLevel", MSVCWarningLevel.W0, "/W0");
            TestFunction("WarningLevel", MSVCWarningLevel.W1, "/W1");
            TestFunction("WarningLevel", MSVCWarningLevel.W2, "/W2");
            TestFunction("WarningLevel", MSVCWarningLevel.W3, "/W3");
            TestFunction("WarningLevel", MSVCWarningLevel.W4, "/W4");
            TestFunction("WarningLevel", MSVCWarningLevel.Wall, "/Wall");

            TestFunction("OptimizationLevel", OptimizationLevel.O0, "/Od");
            TestFunction("OptimizationLevel", OptimizationLevel.O1, "/O1");
            TestFunction("OptimizationLevel", OptimizationLevel.O2, "/O2");
            TestFunction("OptimizationLevel", OptimizationLevel.O3, "/O2");

            TestFunction("FpModel", FpModel.Fast, "/fp:fast");
            TestFunction("FpModel", FpModel.Strict, "/fp:strict");
            TestFunction("FpModel", FpModel.Precise, "/fp:precise");

            TestFunction("Defines", new ArgumentList<string> { "A", "B=2" }, "/DA /DB=2");
            TestFunction("IncludeDirs", new ArgumentList<string> { "C:/", "C:/", "D:/" }, "/IC:/ /IC:/ /ID:/"); 

            TestFunction("RTTI", true, "/GR");
            TestFunction("RTTI", false, "/GR-");
        }

        [TestMethod]
        public void TestLinkerArgDriver()
        {
            var TestFunction = (string Name, object Value, string Expected) => {
                var driver = new LINKArgumentDriver() as IArgumentDriver;
                driver.Arguments.Add(Name, Value);

                var AllCalculatedVars = driver.CalculateArguments().Values.SelectMany(x => x).ToArray();
                var ArgumentsString = new HashSet<string>(AllCalculatedVars);

                var ExpectedArgs = new HashSet<string>(driver.RawArguments.Union(Expected.Split(" ")).ToArray());

                ArgumentsString.ExceptWith(ExpectedArgs);
                Assert.AreEqual(ArgumentsString.Count, 0);
            };

            TestFunction("Arch", Architecture.X86, "/MACHINE:X86");
            TestFunction("Arch", Architecture.X64, "/MACHINE:X64");
            TestFunction("Arch", Architecture.ARM64, "/MACHINE:ARM64");

            TestFunction("PDBMode", PDBMode.Disable, "/DEBUG:NONE");
            TestFunction("PDBMode", PDBMode.Embed, "/DEBUG:FULL");
            TestFunction("PDBMode", PDBMode.Standalone, "/DEBUG:FULL");

            TestFunction("PDB", "C:/pdb.pdb", "/PDB:C:/pdb.pdb");

            TestFunction("RuntimeLibrary", "MT", "/NODEFAULTLIB:msvcrt.lib");
            TestFunction("RuntimeLibrary", "MTd", "/NODEFAULTLIB:msvcrt.lib");
            TestFunction("RuntimeLibrary", "MD", "");
            TestFunction("RuntimeLibrary", "MDd", "");

            TestFunction("TargetType", TargetType.Static, "/LIB");
            TestFunction("TargetType", TargetType.Dynamic, "/DLL");
            TestFunction("TargetType", TargetType.Executable, "");

            TestFunction("LinkDirs", new ArgumentList<string> { "C:/", "D:/" }, "/LIBPATH:C:/ /LIBPATH:D:/");

            TestFunction("Inputs", new ArgumentList<string> { "C:/a.o", "D:/b.o", "D:/e.lib" }, "C:/a.o D:/b.o D:/e.lib");
            TestFunction("Output", "C:/a.lib", "/OUT:C:/a.lib");
            TestFunction("Output", "C:/b.dll", "/OUT:C:/b.dll");
            TestFunction("Output", "C:/c.exe", "/OUT:C:/c.exe");

            TestFunction("WholeArchive", new ArgumentList<string> { "C:/a.lib", "D:/b.lib" }, "/WHOLEARCHIVE:C:/a.lib /WHOLEARCHIVE:D:/b.lib");
        }
    }
}