using System;

namespace Voyl
{
    internal static unsafe class Program
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

            Win32.ShowWindow(_hwnd, 1); // SW_SHOWNORMAL = 1

            while (Win32.GetMessageW(out var msg, IntPtr.Zero, 0, 0))
            {
                Win32.TranslateMessage(ref msg);
                Win32.DispatchMessageW(ref msg);
            }

            Log.Stop();
            return 0;
        }

        private static bool RegisterWindowClass()
        {
            var wndClass = new Win32.WNDCLASSEXW
            {
                cbSize = (uint)sizeof(Win32.WNDCLASSEXW),
                style = 0,
                lpfnWndProc = &WindowProc,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = Win32.GetModuleHandleW(null),
                hIcon = IntPtr.Zero,
                hCursor = Win32.LoadCursorW(IntPtr.Zero, 32512), // IDC_ARROW
                hbrBackground = (IntPtr)(5), // COLOR_WINDOW + 1 = 5
                lpszMenuName = null,
                lpszClassName = WindowClassName,
                hIconSm = IntPtr.Zero
            };

            return Win32.RegisterClassExW(ref wndClass) != 0;
        }

        private static IntPtr CreateWindow()
        {
            return Win32.CreateWindowExW(
                0,
                WindowClassName,
                WindowTitle,
                0x80000000 | 0x00080000, // WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU
                100, 100, WindowWidth, WindowHeight,
                IntPtr.Zero,
                IntPtr.Zero,
                Win32.GetModuleHandleW(null),
                IntPtr.Zero);
        }

        [UnmanagedCallersOnly]
        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case 0x0010: // WM_CLOSE
                    Win32.DestroyWindow(hWnd);
                    Win32.PostQuitMessage(0);
                    return IntPtr.Zero;

                case 0x0112: // WM_SYSCOMMAND
                    if ((wParam.ToInt32() & 0xFFF0) == 0xF010) // SC_CLOSE
                    {
                        Win32.DestroyWindow(hWnd);
                        Win32.PostQuitMessage(0);
                        return IntPtr.Zero;
                    }
                    break;

                case 0x0100: // WM_KEYDOWN
                    if (wParam.ToInt32() == 0x1B) // VK_ESCAPE
                    {
                        Win32.DestroyWindow(hWnd);
                        Win32.PostQuitMessage(0);
                        return IntPtr.Zero;
                    }
                    break;

                case 0x0002: // WM_DESTROY
                    Win32.PostQuitMessage(0);
                    return IntPtr.Zero;
            }

            return Win32.DefWindowProcW(hWnd, msg, wParam, lParam);
        }
    }
}
