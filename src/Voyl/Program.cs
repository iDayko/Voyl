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

            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            // Get desktop hierarchy
            IntPtr desktop = GetDesktopWindow();
            IntPtr progman = FindWindow("Progman", "Program Manager");
            
            Log.Write($"DIAG: Desktop=0x{desktop.ToInt64():X}, Progman=0x{progman.ToInt64():X}");

            // SEELEN UI METHOD: Send 0x052C to Progman
            if (progman != IntPtr.Zero)
            {
                SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);
                System.Threading.Thread.Sleep(500);

                // Find the WorkerW that was created by 0x052C (this is the one BEHIND icons)
                IntPtr workerW = IntPtr.Zero;
                IntPtr targetWorkerW = IntPtr.Zero;
                
                do
                {
                    workerW = FindWindowEx(desktop, workerW, "WorkerW", null);
                    IntPtr defView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (defView != IntPtr.Zero)
                    {
                        targetWorkerW = workerW;
                        IntPtr sysListView32 = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
                        Log.Write($"DIAG: WorkerW=0x{targetWorkerW.ToInt64():X}, SHELLDLL_DefView=0x{defView.ToInt64():X}, SysListView32=0x{sysListView32.ToInt64():X}");
                        break;
                    }
                } while (workerW != IntPtr.Zero);

                if (targetWorkerW != IntPtr.Zero)
                {
                    // The WorkerW created by 0x052C is the one that contains the desktop icons
                    // We need to position Voyl BEHIND this WorkerW
                    // SetWindowPos with insertAfter=targetWorkerW places Voyl BEFORE targetWorkerW in Z-order
                    // To place BEHIND, we need to use the WorkerW's parent or a lower Z-order
                    
                    // Try: Set Voyl as child of desktop, positioned behind WorkerW
                    // First, ensure Voyl is child of desktop
                    SetParent(_hwnd, desktop);
                    
                    // Then position behind WorkerW using SetWindowPos
                    // HWND_BOTTOM = (IntPtr)(-1)
                    // But we want specifically behind WorkerW
                    // Try: insertAfter = GetWindow(WorkerW, GW_HWNDNEXT) to get what's behind WorkerW
                    
                    // Alternative: Use HWND_BOTTOM and rely on Z-order
                    bool posResult = SetWindowPos(_hwnd, (IntPtr)(-1), 0, 0, screenWidth, screenHeight, 0x0002 | 0x0001 | 0x0004);
                    Log.Write($"DIAG: SetWindowPos(HWND_BOTTOM) result={posResult}");
                    
                    // Then bring WorkerW to front to ensure proper ordering
                    SetWindowPos(targetWorkerW, (IntPtr)(-2), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0004); // HWND_NOTOPMOST
                }
            }
            else
            {
                Log.Write("DIAG: Progman not found");
            }

            // Make window transparent
            bool layeredResult = SetLayeredWindowAttributes(_hwnd, 0, 255, 0x00000001); // LWA_COLORKEY
            Log.Write($"DIAG: SetLayeredWindowAttributes(COLORKEY) result={layeredResult}");
            
            // Also try alpha
            layeredResult = SetLayeredWindowAttributes(_hwnd, 0, 0, 0x00000002); // LWA_ALPHA
            Log.Write($"DIAG: SetLayeredWindowAttributes(ALPHA=0) result={layeredResult}");

            // Register global hotkey
            bool hotkeyResult = RegisterHotKey(IntPtr.Zero, HOTKEY_ID, 0, 0x1B);
            Log.Write($"DIAG: RegisterHotKey result={hotkeyResult}");

            ShowWindow(_hwnd, 5);
            Log.Write($"DIAG: IsWindowVisible={IsWindowVisible(_hwnd)}");

            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0))
            {
                if (msg.message == 0x0312 && msg.wParam.ToInt32() == HOTKEY_ID)
                {
                    Log.Write("DIAG: Hotkey pressed (Escape)");
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
                hbrBackground = (IntPtr)1, // BLACK_BRUSH
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
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

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
