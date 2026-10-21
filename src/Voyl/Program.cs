using System;
using System.Runtime.InteropServices;

namespace Voyl
{
    internal static class Program
    {
        private const string WindowClassName = "VoylWindowClass";
        private const string WindowTitle = "Voyl";
        private const int WindowWidth = 800;
        private const int WindowHeight = 450;

        private static IntPtr _hwnd = IntPtr.Zero;

        [STAThread]
        private static int Main()
        {
            Log.Start();

            if (!RegisterWindowClass())
            {
                Log.Write("ERROR: Failed to register window class");
                return 1;
            }

            _hwnd = CreateWindow();
            if (_hwnd == IntPtr.Zero)
            {
                Log.Write("ERROR: Failed to create window");
                return 1;
            }

            ShowWindow(_hwnd, 1); // SW_SHOWNORMAL = 1

            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }

            Log.Stop();
            return 0;
        }

        private static bool RegisterWindowClass()
        {
            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                style = 0,
                lpfnWndProc = &WindowProc,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = GetModuleHandleW(null),
                hIcon = IntPtr.Zero,
                hCursor = LoadCursorW(IntPtr.Zero, 32512), // IDC_ARROW
                hbrBackground = (IntPtr)(5), // COLOR_WINDOW + 1 = 5
                lpszMenuName = null,
                lpszClassName = WindowClassName,
                hIconSm = IntPtr.Zero
            };

            return RegisterClassExW(ref wndClass) != 0;
        }

        private static IntPtr CreateWindow()
        {
            return CreateWindowExW(
                0,
                WindowClassName,
                WindowTitle,
                0x80000000 | 0x00080000, // WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU
                100, 100, WindowWidth, WindowHeight,
                IntPtr.Zero,
                IntPtr.Zero,
                GetModuleHandleW(null),
                IntPtr.Zero);
        }

        [UnmanagedCallersOnly]
        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case 0x0010: // WM_CLOSE
                    DestroyWindow(hWnd);
                    PostQuitMessage(0);
                    return IntPtr.Zero;

                case 0x0112: // WM_SYSCOMMAND
                    if ((wParam.ToInt32() & 0xFFF0) == 0xF010) // SC_CLOSE
                    {
                        DestroyWindow(hWnd);
                        PostQuitMessage(0);
                        return IntPtr.Zero;
                    }
                    break;

                case 0x0100: // WM_KEYDOWN
                    if (wParam.ToInt32() == 0x1B) // VK_ESCAPE
                    {
                        DestroyWindow(hWnd);
                        PostQuitMessage(0);
                        return IntPtr.Zero;
                    }
                    break;

                case 0x0002: // WM_DESTROY
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }

            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        // Win32 structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEXW
        {
            public uint cbSize;
            public uint style;
            public delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr> lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string? lpszMenuName;
            public string? lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        // CsWin32 will generate these from NativeMethods.txt
        private static partial class NativeMethods
        {
            [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            public static partial ushort RegisterClassExW(ref WNDCLASSEXW wndClass);

            [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            public static partial IntPtr CreateWindowExW(
                uint dwExStyle,
                string lpClassName,
                string lpWindowName,
                uint dwStyle,
                int x,
                int y,
                int nWidth,
                int nHeight,
                IntPtr hWndParent,
                IntPtr hMenu,
                IntPtr hInstance,
                IntPtr lpParam);

            [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
            public static partial IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            [LibraryImport("user32.dll", SetLastError = true)]
            public static partial void PostQuitMessage(int nExitCode);

            [LibraryImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool DestroyWindow(IntPtr hWnd);

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool TranslateMessage(ref MSG lpMsg);

            [LibraryImport("user32.dll")]
            public static partial IntPtr DispatchMessageW(ref MSG lpMsg);

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
            public static partial IntPtr GetModuleHandleW(string? lpModuleName);

            [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
            public static partial IntPtr LoadCursorW(IntPtr hInstance, int lpCursorName);

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool PostMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        }

        private static class Win32
        {
            public static ushort RegisterClassExW(ref WNDCLASSEXW wndClass) => NativeMethods.RegisterClassExW(ref wndClass);
            public static IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam) => NativeMethods.CreateWindowExW(dwExStyle, lpClassName, lpWindowName, dwStyle, x, y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
            public static IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) => NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
            public static void PostQuitMessage(int nExitCode) => NativeMethods.PostQuitMessage(nExitCode);
            public static bool DestroyWindow(IntPtr hWnd) => NativeMethods.DestroyWindow(hWnd);
            public static bool GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax) => NativeMethods.GetMessageW(out lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);
            public static bool TranslateMessage(ref MSG lpMsg) => NativeMethods.TranslateMessage(ref lpMsg);
            public static IntPtr DispatchMessageW(ref MSG lpMsg) => NativeMethods.DispatchMessageW(ref lpMsg);
            public static bool ShowWindow(IntPtr hWnd, int nCmdShow) => NativeMethods.ShowWindow(hWnd, nCmdShow);
            public static IntPtr GetModuleHandleW(string? lpModuleName) => NativeMethods.GetModuleHandleW(lpModuleName);
            public static IntPtr LoadCursorW(IntPtr hInstance, int lpCursorName) => NativeMethods.LoadCursorW(hInstance, lpCursorName);
            public static bool PostMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) => NativeMethods.PostMessageW(hWnd, Msg, wParam, lParam);
        }
    }
}
