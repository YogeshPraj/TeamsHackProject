using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace TeamsHack
{
    public class Window
    {
        public IntPtr hWnd;
        public string title;
        public string executablePath;
        public uint pid;
        public List<Area> areas = new List<Area>();
        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set { _isChecked = value; }
        }

        public string DisplayText
        {
            get
            {
                return title + " [" + executablePath + "]";
            }
        }
    }

    public class DesktopWindow
    {
        static List<Window> WindowsList = new List<Window>();

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            ProcessQueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr handle, ref Rectangle screenRectangle);

        [DllImport("user32.dll")]
        protected static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        protected static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool EnumChildWindows(IntPtr parentHandle, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        private static extern bool QueryFullProcessImageName(IntPtr hprocess, int dwFlags, StringBuilder lpExeName, out int size);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        protected delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


        private static bool EnumWindowsList(IntPtr hWnd, IntPtr lParam)
        {
            int size = GetWindowTextLength(hWnd);
            Window w = new Window();
            w.hWnd = hWnd;
            if (size++ > 0 && IsWindowVisible(hWnd))
            {
                StringBuilder sb = new StringBuilder(size);
                GetWindowText(hWnd, sb, size);
                w.title = sb.ToString();
                if (sb.ToString() != "Program Manager" && sb.ToString() != "Windows Shell Experience Host")
                {
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    w.pid = pid;
                    var parent = GetParent(hWnd);
                    if (parent == IntPtr.Zero)
                    {
                        WindowsList.Add(w);
                    }
                    else
                    {
                        var proc = Process.GetProcessById((int)pid);
                        if (proc.MainWindowTitle == "")
                        {
                            WindowsList.Add(w);
                        }
                    }

                }
            }
            return true;
        }

        private static string GetExecutablePath(IntPtr hWnd, uint pid)
        {
            var buffer = new StringBuilder(1024);
            IntPtr hprocess = OpenProcess(ProcessAccessFlags.ProcessQueryLimitedInformation, false, (int)pid);
            if (hprocess != IntPtr.Zero)
            {
                try
                {
                    int size = buffer.Capacity;
                    if (QueryFullProcessImageName(hprocess, 0, buffer, out size))
                    {
                        var path = buffer.ToString();
                        if (path == @"C:\Windows\System32\ApplicationFrameHost.exe")
                        {
                            path = GetModernAppExecutablePath(hWnd, pid);
                            return path;
                        }
                        return buffer.ToString();
                    }
                }
                finally
                {
                    CloseHandle(hprocess);
                }
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static string GetModernAppExecutablePath(IntPtr hWnd, uint pid)
        {
            var children = GetChildWindows(hWnd);
            foreach (var childHwnd in children)
            {
                uint childPid = 0;
                GetWindowThreadProcessId(childHwnd, out childPid);
                if (childPid != pid)
                {
                    return GetExecutablePath(childHwnd, childPid);
                }
            }
            return "";
        }
        private static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                EnumChildWindows(parent, new EnumWindowsProc(EnumWindow), GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }
        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            return true;
        }

        public static List<Window> GetWindows()
        {
            List<Window> windows = new List<Window>();
            EnumDesktopWindows(IntPtr.Zero, new EnumWindowsProc(EnumWindowsList), IntPtr.Zero);
            foreach (var w in WindowsList)
            {
                if ((w.executablePath = GetExecutablePath(w.hWnd, w.pid)) != "")
                {
                    PopulateArea(new List<Window>() { w });
                    windows.Add(w);
                }
            }

            return windows;
        }

        public static void PopulateArea(IList<Window> windows)
        {
            foreach (var w in windows)
            {
                var screeRectangle = new Rectangle();
                int zorder = 0;
                GetWindowRect(w.hWnd, ref screeRectangle);
                GetWindowZOrder(w.hWnd, out zorder);

                w.areas.Add(new Area()
                {
                    X = screeRectangle.X,
                    Y = screeRectangle.Y,
                    Width = screeRectangle.Width,
                    Height = screeRectangle.Height,
                    ZOrder = zorder
                }) ;

            }

        }

        public static bool GetWindowZOrder(IntPtr hwnd, out int zOrder)
        {
            const uint GW_HWNDPREV = 3;
            const uint GW_HWNDLAST = 1;

            var lowestHwnd = GetWindow(hwnd, GW_HWNDLAST);

            var z = 0;
            var hwndTmp = lowestHwnd;
            while (hwndTmp != IntPtr.Zero)
            {
                if (hwnd == hwndTmp)
                {
                    zOrder = z;
                    return true;
                }

                hwndTmp = GetWindow(hwndTmp, GW_HWNDPREV);
                z++;
            }

            zOrder = int.MinValue;
            return false;
        }
    }
}
