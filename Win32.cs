using System;
using System.Runtime.InteropServices;

namespace ScreenCast
{
    public static class Win32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;        // Specifies the size, in bytes, of the structure.
                                        // The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).
            public int flags;         // Specifies the cursor state. This parameter can be one of the following values:
                                        //    0             The cursor is hidden.
                                        //    CURSOR_SHOWING    The cursor is showing.
                                        //    CURSOR_SUPPRESSED    (Windows 8 and above.) The cursor is suppressed. This flag indicates that the system is not drawing the cursor because the user is providing input through touch or pen instead of the mouse.
            public IntPtr hCursor;          // Handle to the cursor.
            public POINT ptScreenPos;       // A POINT structure that receives the screen coordinates of the cursor.
        }

        /// <summary>Must initialize cbSize</summary>
        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(ref CURSORINFO pci);

        public enum CURSOR_FLAG : int
        {
            CURSOR_HIDDEN =     0x00000000,
            CURSOR_SHOWING =    0x00000001,
            CURSOR_SUPPRESSED = 0x00000002,
        }
        //private const int CURSOR_SHOWING = 0x00000001;
        //private const int CURSOR_SUPPRESSED = 0x00000002;


        [DllImport("user32.dll")]
        public static extern bool DrawIconEx(
            IntPtr hdc, int xLeft, int yTop, IntPtr hIcon,
           int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);
    }
}
