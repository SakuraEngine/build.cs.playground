﻿using System.Diagnostics;
using SB.Core;

namespace SB.Test
{
    [TestClass]
    public sealed class Test1
    {
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
        public void TestFindVCVars()
        {
            VisualStudio vs = new VisualStudio(VisualStudio.Version.V2022);
            vs.Initialize().Wait(10000);
            Assert.IsTrue(Path.Exists(vs.VCVars64Bat));
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
            var TestFunction = (string Name, object Value, string Result) => {
                var driver = new MSVCArgumentDriver() as IArgumentDriver;
                object[] args = { Value };
                driver.Arguments.Add(Name, args);

                var ArgumentsString = String.Join(" ", driver.CalculateArguments());

                if (Result != "")
                    Result += " ";
                Result += String.Join(" ", driver.RawArguments);

                Assert.AreEqual(ArgumentsString, Result);
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
            
            TestFunction("PDB", "C:/", "/FdC:/");

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

            TestFunction("Defines", new Dictionary<string, string?> { { "A", null }, { "B", "2" } }, "/DA /DB=2");
            TestFunction("IncludeDirs", new string[] { "C:/", "C:/" }, "/IC:/"); // Test Union

            TestFunction("RTTI", true, "/GR");
            TestFunction("RTTI", false, "/GR-");

            /*
            var driver = new MSVCArgumentDriver() as IArgumentDriver;
            driver.AddArgument("CppVersion", "20");
            driver.AddArgument("Exception", true);
            driver.AddArgument("RuntimeLibrary", "MD");
            driver.AddArgument("Arch", Architecture.X64);
            driver.AddArgument("SIMD", SIMDArchitecture.AVX);
            driver.AddArgument("PDBMode", PDBMode.Embed);
            driver.AddArgument("OptimizationLevel", OptimizationLevel.O0);
            driver.AddArgument("FpModel", FpModel.Fast);
            driver.AddArgument("Defines", new Dictionary<string, string?> {
                { "A", null }, { "B", "1" }, { "C", "B" }
            });
            driver.AddArgument("IncludeDirs", new string[] { "C:/ " });
            driver.AddArgument("RTTI", false);
            driver.AddArgument("Source", "C:/");
            var compile_commands = driver.CompileCommands("C:/");
            */
        }
    }
}