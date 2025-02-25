using System.Diagnostics;
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
                driver.Semantics.Add(Name, args);
                Assert.AreEqual(driver.CalculateArguments()[0], Result);
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

        }
    }
}