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
            Log.Write("START: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

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

            // Get screen dimensions
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            // SEELEN UI METHOD: Send 0x052C to Progman to create a WorkerW behind icons
            IntPtr desktop = GetDesktopWindow();
            IntPtr progman = FindWindow("Progman", "Program Manager");
            
            Log.Write($"DIAG: Desktop=0x{desktop.ToInt64():X}, Progman=0x{progman.ToInt64():X}");

            if (progman != IntPtr.Zero)
            {
                // Send 0x052C message to Progman - this creates a WorkerW behind the icons
                IntPtr resultMsg = SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);
                Log.Write($"DIAG: SendMessage(0x052C) result=0x{resultMsg.ToInt64():X}");
                System.Threading.Thread.Sleep(500); // Wait for WorkerW creation

                // Find the WorkerW that contains SHELLDLL_DefView
                IntPtr workerW = IntPtr.Zero;
                IntPtr targetWorkerW = IntPtr.Zero;
                IntPtr foundDefView = IntPtr.Zero;
                IntPtr foundSysListView32 = IntPtr.Zero;
                
                do
                {
                    workerW = FindWindowEx(desktop, workerW, "WorkerW", null);
                    if (workerW != IntPtr.Zero)
                    {
                        IntPtr defView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (defView != IntPtr.Zero)
                        {
                            targetWorkerW = workerW;
                            foundDefView = defView;
                            foundSysListView32 = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
                            break;
                        }
                    }
                } while (workerW != IntPtr.Zero);

                Log.Write($"DIAG: WorkerW=0x{targetWorkerW.ToInt64():X}, SHELLDLL_DefView=0x{foundDefView.ToInt64():X}, SysListView32=0x{foundSysListView32.ToInt64():X}");

                if (targetWorkerW != IntPtr.Zero)
                {
                    // Position Voyl BEHIND this WorkerW (between wallpaper and icons)
                    // insertAfter = targetWorkerW means: place Voyl IMMEDIATELY BEFORE targetWorkerW in Z-order
                    // So: Wallpaper -> Voyl -> WorkerW -> Icons
                    bool posResult = SetWindowPos(_hwnd, targetWorkerW, 0, 0, screenWidth, screenHeight, 0x0002 | 0x0001 | 0x0004);
                    Log.Write($"DIAG: SetWindowPos(insertAfter=WorkerW=0x{targetWorkerW.ToInt64():X}) result={posResult}");
                }
                else
                {
                    Log.Write("DIAG: WorkerW with SHELLDLL_DefView not found, trying Progman");
                    bool posResult = SetWindowPos(_hwnd, progman, 0, 0, screenWidth, screenHeight, 0x0002 | 0x0001 | 0x0004);
                    Log.Write($"DIAG: SetWindowPos(insertAfter=Progman=0x{progman.ToInt64():X}) result={posResult}");
                }
            }
            else
            {
                Log.Write("DIAG: Progman not found, positioning at HWND_BOTTOM");
                bool posResult = SetWindowPos(_hwnd, (IntPtr)(-1), 0, 0, screenWidth, screenHeight, 0x0002 | 0x0001 | 0x0004);
                Log.Write($"DIAG: SetWindowPos(HWND_BOTTOM) result={posResult}");
            }

            // Make window completely transparent (click-through)
            // LWA_ALPHA = 0x00000002
            bool layeredResult = SetLayeredWindowAttributes(_hwnd, 0, 0, 0x00000002);
            Log.Write($"DIAG: SetLayeredWindowAttributes(alpha=0) result={layeredResult}");

            // Verify extended style
            int exStyle = GetWindowLong(_hwnd, -20); // GWL_EXSTYLE
            Log.Write($"DIAG: Extended Style after setup=0x{exStyle:X}");

            // Register global hotkey for Escape (works from any window)
            bool hotkeyResult = RegisterHotKey(IntPtr.Zero, HOTKEY_ID, 0, 0x1B); // VK_ESCAPE
            Log.Write($"DIAG: RegisterHotKey(result={hotkeyResult}, hWnd=IntPtr.Zero, id={HOTKEY_ID}, vk=0x1B)");

            // Show window
            ShowWindow(_hwnd, 5); // SW_SHOWNA
            bool isVisible = IsWindowVisible(_hwnd);
            Log.Write($"DIAG: IsWindowVisible={isVisible}");

            // Get current parent
            IntPtr parent = GetParent(_hwnd);
            Log.Write($"DIAG: Parent HWND=0x{parent.ToInt64():X}");

            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0))
            {
                if (msg.message == 0x0312 && msg.wParam.ToInt32() == HOTKEY_ID) // WM_HOTKEY
                {
                    Log.Write("DIAG: Hotkey pressed (Escape) - closing");
                    DestroyWindow(_hwnd);
                    PostQuitMessage(0);
                    break;
                }
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }

            UnregisterHotKey(_hwnd, HOTKEY_ID);
            Log.Write("STOP: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
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
                hbrBackground = (IntPtr)1, // BLACK_BRUSH for visual verification
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

            // WS_EX_LAYERED (0x80000) | WS_EX_TRANSPARENT (0x20)
            // NO WS_EX_TOPMOST - we want it behind icons, not in front
            return CreateWindowExW(
                0x00080000 | 0x00000020, // WS_EX_LAYERED | WS_EX_TRANSPARENT
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
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW(string? lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadCursorW(IntPtr hInstance, int lpCursorName);
    }
}
