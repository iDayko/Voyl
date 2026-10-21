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

            // Positionner la fenêtre derrière le bureau
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            
            // Trouver la bonne hiérarchie : Progman -> (WorkerW) -> SHELLDLL_DefView -> SysListView32
            IntPtr desktop = GetDesktopWindow();
            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                progman = GetWindow(desktop, 5); // GW_CHILD
            }
            
            // Chercher SHELLDLL_DefView dans Progman ou ses WorkerW
            IntPtr shellDefView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            
            // Si non trouvé, énumérer tous les WorkerW pour trouver celui qui contient SHELLDLL_DefView
            if (shellDefView == IntPtr.Zero)
            {
                IntPtr workerW = IntPtr.Zero;
                do
                {
                    workerW = FindWindowEx(desktop, workerW, "WorkerW", null);
                    shellDefView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                }
                while (shellDefView == IntPtr.Zero && workerW != IntPtr.Zero);
            }
            
            // Maintenant chercher SysListView32 dans SHELLDLL_DefView
            IntPtr sysListView32 = IntPtr.Zero;
            if (shellDefView != IntPtr.Zero)
            {
                sysListView32 = FindWindowEx(shellDefView, IntPtr.Zero, "SysListView32", "FolderView");
                if (sysListView32 == IntPtr.Zero)
                {
                    sysListView32 = FindWindowEx(shellDefView, IntPtr.Zero, "SysListView32", null);
                }
            }
            
            Log.Write($"DIAG: Desktop=0x{desktop.ToInt64():X}, Progman=0x{progman.ToInt64():X}, SHELLDLL_DefView=0x{shellDefView.ToInt64():X}, SysListView32=0x{sysListView32.ToInt64():X}");
            
            // 1. MONTRER la fenêtre (nécessaire avant SetWindowPos)
            ShowWindow(_hwnd, 5); // SW_SHOWNA
            
            // 2. NE PAS utiliser SetParent (ca bloque les clics meme avec WS_EX_TRANSPARENT)
            //    Garder la fenetre comme enfant du Desktop (parent par defaut)
            //    et positionner avec SetWindowPos relative a SysListView32
            
            // 3. Placer DERRIERE SysListView32 dans le Z-order global
            uint flags = 0x0002 | 0x0001 | 0x0004; // SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE
            IntPtr insertAfter = sysListView32 != IntPtr.Zero ? sysListView32 : (IntPtr)1; // HWND_BOTTOM
            bool setPosResult = SetWindowPos(_hwnd, insertAfter, 0, 0, 0, 0, flags);
            Log.Write($"DIAG: SetWindowPos(insertAfter=0x{insertAfter.ToInt64():X}) result={setPosResult}");
            
            // 4. Verifier le parent actuel (doit etre Desktop=0x1000C)
            IntPtr currentParent = GetParent(_hwnd);
            Log.Write($"DIAG: Current Parent=0x{currentParent.ToInt64():X}");
            
            // 5. Verifier le style etendu
            int exStyle = GetWindowLong(_hwnd, -20); // GWL_EXSTYLE
            Log.Write($"DIAG: Extended Style=0x{exStyle:X}");
            
            // 3. Enregistrer le hotkey Échap (global, même si la fenêtre n'est pas active)
            bool hotkeyResult = RegisterHotKey(_hwnd, HOTKEY_ID, 0, 0x1B); // VK_ESCAPE
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
                hbrBackground = (IntPtr)4, // Fond noir (BLACK_BRUSH)
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
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW(string? lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadCursorW(IntPtr hInstance, int lpCursorName);
    }
}
