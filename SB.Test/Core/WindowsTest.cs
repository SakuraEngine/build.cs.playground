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
    }
}

