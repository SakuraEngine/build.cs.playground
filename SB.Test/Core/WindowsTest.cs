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
                Path.Exists($"{drive}:/");
            }
        }        
        
        [TestMethod]
        public void TestFindVCVars()
        {
            var vcvars = SB.Core.VisualStudio.FindVCVars();
            foreach (var vcvar in vcvars)
            {
                Path.Exists(vcvar);
            }
        }
    }
}
