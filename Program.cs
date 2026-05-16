#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace YouTubeMusic
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);
        
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const string AppMutexName = "YouTubeMusicApp_SingleInstance";

        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, AppMutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    // Ищем окно по заголовку
                    IntPtr hWnd = FindWindow(null, "YouTube Music");
                    
                    if (hWnd != IntPtr.Zero)
                    {
                        // Показываем окно если скрыто
                        ShowWindow(hWnd, SW_SHOW);
                        if (IsIconic(hWnd))
                        {
                            ShowWindow(hWnd, SW_RESTORE);
                        }
                        SetForegroundWindow(hWnd);
                    }
                    else
                    {
                        // Если не нашли по заголовку - ищем процесс
                        Process current = Process.GetCurrentProcess();
                        foreach (Process p in Process.GetProcessesByName(current.ProcessName))
                        {
                            if (p.Id != current.Id && p.MainWindowHandle != IntPtr.Zero)
                            {
                                ShowWindow(p.MainWindowHandle, SW_SHOW);
                                SetForegroundWindow(p.MainWindowHandle);
                                break;
                            }
                        }
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}
