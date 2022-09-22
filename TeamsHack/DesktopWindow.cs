using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace TeamsHack
{
    public class Window
    {
        public IntPtr hWnd;
        public string title;
        public string executablePath;
        public uint pid;
        public Area area;

        public void SetArea(Rect rect, int z)
        {
            area = Area.FromRectangle(new Rectangle(rect.Left, rect.Bottom, rect.Right - rect.Left, rect.Bottom - rect.Top));
            area.Z = z;
        }

        public override string ToString()
        {
            return $"{title}  [{executablePath}]";
        }
    }

    public struct Rect
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
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
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

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
            int i = 0;
            foreach (var w in WindowsList)
            {
                if ((w.executablePath = GetExecutablePath(w.hWnd, w.pid)) != "")
                {
                    var rect = new Rect();
                    GetWindowRect(w.hWnd, ref rect);
                    w.SetArea(rect, i--);
                    windows.Add(w);
                }
            }
            

            return windows;
        }
    }
}
