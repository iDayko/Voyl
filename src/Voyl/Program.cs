using System;
using System.Runtime.InteropServices;

namespace Voyl
{
    internal static unsafe class Program
    {
        private const string WindowClassName = "VoylWindowClass";
        private const string WindowTitle = "Voyl";
        private const int HOTKEY_ID = 1;

        private static IntPtr _hwnd = IntPtr.Zero;
        private static delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr> _windowProc = &WindowProc;

        [STAThread]
        private static int Main()
        {
            Log.Start();

            if (!RegisterWindowClass())
            {
                Log.Write("ERROR: Failed to register window class");
                Log.Stop();
                return 1;
            }

            _hwnd = CreateWindow();
            if (_hwnd == IntPtr.Zero)
            {
                Log.Write("ERROR: Failed to create window");
                Log.Stop();
                return 1;
            }

            Log.Write($"DIAG: Voyl HWND=0x{_hwnd.ToInt64():X}");

            // Positionner la fenêtre
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            
            // 1. MONTRER la fenêtre D'ABORD (nécessaire pour SetLayeredWindowAttributes)
            ShowWindow(_hwnd, 5); // SW_SHOWNA
            
            // 2. Rendre transparente
            bool layeredResult = SetLayeredWindowAttributes(_hwnd, 0, 0, 0x00000002); // LWA_ALPHA
            Log.Write($"DIAG: SetLayeredWindowAttributes(alpha=0) result={layeredResult}");
            
            // 3. Placer en bas du Z-order
            bool setPosResult = SetWindowPos(_hwnd, (IntPtr)1, 0, 0, screenWidth, screenHeight, 0x0000); // HWND_BOTTOM
            Log.Write($"DIAG: SetWindowPos(HWND_BOTTOM) result={setPosResult}");
            
            // 4. Enregistrer le hotkey Échap
            bool hotkeyResult = RegisterHotKey(_hwnd, HOTKEY_ID, 0, 0x1B);
            Log.Write($"DIAG: RegisterHotKey result={hotkeyResult}");

            bool isVisible = IsWindowVisible(_hwnd);
            Log.Write($"DIAG: IsWindowVisible={isVisible}");

            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }

            UnregisterHotKey(_hwnd, HOTKEY_ID);
            Log.Stop();
            return 0;
        }

        private static bool RegisterWindowClass()
        {
            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                style = 0,
                lpfnWndProc = _windowProc,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = GetModuleHandleW(null!),
                hIcon = IntPtr.Zero,
                hCursor = LoadCursorW(IntPtr.Zero, 32512),
                hbrBackground = (IntPtr)5, // Fond blanc (invisible avec alpha=0)
                lpszMenuName = null!,
                lpszClassName = WindowClassName,
                hIconSm = IntPtr.Zero
            };

            return RegisterClassExW(ref wndClass) != 0;
        }

        private static IntPtr CreateWindow()
        {
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            
            return CreateWindowExW(
                0x00000020 | 0x00000080, // WS_EX_LAYERED | WS_EX_TRANSPARENT
                WindowClassName,
                WindowTitle,
                0x80000000, // WS_POPUP
                0, 0, screenWidth, screenHeight,
                IntPtr.Zero,
                IntPtr.Zero,
                GetModuleHandleW(null!),
                IntPtr.Zero);
        }

        [UnmanagedCallersOnly(EntryPoint = "WindowProc")]
        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case 0x0010: // WM_CLOSE
                    DestroyWindow(hWnd);
                    PostQuitMessage(0);
                    return IntPtr.Zero;

                case 0x0112: // WM_SYSCOMMAND
                    if ((wParam.ToInt32() & 0xFFF0) == 0xF060) // SC_CLOSE
                    {
                        DestroyWindow(hWnd);
                        PostQuitMessage(0);
                        return IntPtr.Zero;
                    }
                    break;

                case 0x0312: // WM_HOTKEY
                    if (wParam.ToInt32() == HOTKEY_ID)
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
            public int ptX;
            public int ptY;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClassExW(ref WNDCLASSEXW wndClass);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessageW(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW(string? lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadCursorW(IntPtr hInstance, int lpCursorName);
    }
}
