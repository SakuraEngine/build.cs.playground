using System.Runtime.InteropServices;

namespace SB.Core
{
    public static partial class Windows
    {
        [DllImport("kernel32", ExactSpelling = true)]
        public static extern uint GetLogicalDriveStringsW(uint nBufferLength, IntPtr lpBuffer);

        public static unsafe List<string> EnumLogicalDrives()
        {
            var Disks = new List<string>();
            var strLen = GetLogicalDriveStringsW(0, IntPtr.Zero);
            var DisksBuffer = new char[strLen];
            fixed (char* ptr = DisksBuffer)
                GetLogicalDriveStringsW(strLen, (IntPtr)ptr);
            Disks = new string(DisksBuffer).Replace(":\\", "").Split('\0').ToList();
            Disks.RemoveAll(x => string.IsNullOrEmpty(x));
            return Disks;
        }
    }
}
