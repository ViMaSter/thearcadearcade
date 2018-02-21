using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace thearcadearcade.Helper
{

    static class WinAPI
    {
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int MEM_COMMIT = 0x00001000;
        public const int PAGE_READWRITE = 0x04;
        public const int PROCESS_WM_READ = 0x0010;
        public const uint MF_BYPOSITION = 0x400;
        public const uint MF_REMOVE = 0x1000;
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int WS_CHILD = 0x40000000; //child window
        public const int WS_BORDER = 0x00800000; //window with border
        public const int WS_DLGFRAME = 0x00400000; //window with double border but no title
        public const int WS_THICKFRAME = 0x00040000; //window with double border but no title
        public const int WS_MINIMIZE = 0x20000000; //window with double border but no title
        public const int WS_CAPTION = WS_BORDER | WS_DLGFRAME; //window with a title bar 
        public const int WS_SYSMENU = 0x00080000; //window menu  
        public const int WS_EX_CLIENTEDGE = 0x00000200; //window menu  
        public const int WS_EX_DLGMODALFRAME = 0x00000001; //window menu  
        public const int WS_EX_WINDOWEDGE = 0x00000100; //window menu  
        public const int WS_EX_STATICEDGE = 0x00020000; //window menu  
        public const int SWP_FRAMECHANGED = 0x0020; //window menu  
        public const int SWP_NOMOVE = 0x0002; //window menu  
        public const int SWP_NOSIZE = 0x0001; //window menu  
        public const int SWP_NOZORDER = 0x0004; //window menu  
        public const int SWP_NOOWNERZORDER = 0x0200; //window menu  

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        [DllImport("dwmapi.dll", SetLastError = true)]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        //Sets a window to be a child window of another window
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern int SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        //Sets window attributes
        public static int SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32());
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern int GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static int GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool SetMenu(IntPtr hWnd, IntPtr hMenu = new IntPtr());

        [DllImport("user32.dll")]
        static extern bool DrawMenuBar(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWindow, IntPtr hWindowInserAfter, int x, int y, int cx, int cy, uint uFlags);

        public static void WindowsReStyle(IntPtr windowHandle)
        {
            IntPtr pFoundWindow = windowHandle;
            int style = GetWindowLongPtr(pFoundWindow, GWL_STYLE);
            int exStyle = GetWindowLongPtr(pFoundWindow, GWL_EXSTYLE);
            int error = 0;

            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZE | WS_SYSMENU);
            SetWindowLongPtr(pFoundWindow, GWL_STYLE, new IntPtr(style));

            error = Marshal.GetLastWin32Error();
            exStyle &= ~(WS_EX_CLIENTEDGE | WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_STATICEDGE);
            SetWindowLongPtr(pFoundWindow, GWL_EXSTYLE, new IntPtr(exStyle));

            error = Marshal.GetLastWin32Error();
            style = GetWindowLongPtr(pFoundWindow, GWL_STYLE);
            error = Marshal.GetLastWin32Error();

            // Negative margins have special meaning to DwmExtendFrameIntoClientArea.
            // Negative margins create the "sheet of glass" effect, where the client area
            // is rendered as a solid surface with no window border.
            MARGINS margins = new Helper.WinAPI.MARGINS();
            margins.bottomHeight = 10;
            margins.leftWidth    = 10;
            margins.rightWidth   = 10;
            margins.topHeight    = 10;
            DwmExtendFrameIntoClientArea(windowHandle, ref margins);

            error = Marshal.GetLastWin32Error();

            // hide menu but don't actually remove it, so ROM loading is still possible in nestopia
            SetMenu(windowHandle);

            // force a redraw
            DrawMenuBar(windowHandle);

            Rect resolution = new Rect( 0, 0, 256, 240 );
            Rect currentResolution = SystemParameters.WorkArea;
            Rect maxResolution = resolution;
            do
            {
                maxResolution.Scale(2, 2);
                if (maxResolution.Width > currentResolution.Width || maxResolution.Height > currentResolution.Height)
                {
                    maxResolution.Scale(0.5, 0.5);
                    break;
                }
            } while (true);

            SetWindowPos(windowHandle, new IntPtr(), (int)((currentResolution.Width - maxResolution.Width) / 2), (int)((currentResolution.Height - maxResolution.Height) / 2), (int)maxResolution.Width, (int)maxResolution.Height, SWP_FRAMECHANGED | SWP_NOZORDER | SWP_NOOWNERZORDER);
            error = Marshal.GetLastWin32Error();
        }

        // REQUIRED STRUCTS
        public struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public int AllocationProtect;
            public uint RegionSize;
            public int State;
            public int Protect;
            public int lType;
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public UIntPtr minimumApplicationAddress;
            public UIntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }
    }
}
