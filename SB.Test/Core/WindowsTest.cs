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
            var vcvars = SB.Core.VisualStudio.FindVCVars();
            foreach (var vcvar in vcvars)
            {
                Assert.IsTrue(Path.Exists(vcvar));
            }
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
