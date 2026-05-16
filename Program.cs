using System;
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
        
        private const int SW_RESTORE = 9;
        private const string AppMutexName = "YouTubeMusicApp_SingleInstance";

        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, AppMutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    // Приложение уже запущено - найти и показать окно
                    IntPtr hWnd = FindExistingWindow();
                    if (hWnd != IntPtr.Zero)
                    {
                        if (IsIconic(hWnd))
                        {
                            ShowWindow(hWnd, SW_RESTORE);
                        }
                        SetForegroundWindow(hWnd);
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }

        private static IntPtr FindExistingWindow()
        {
            IntPtr hWnd = IntPtr.Zero;
            foreach (Form form in Application.OpenForms)
            {
                hWnd = form.Handle;
                break;
            }
            
            if (hWnd == IntPtr.Zero)
            {
                hWnd = NativeMethods.FindWindow(null, "YouTube Music");
            }
            
            return hWnd;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);
    }
}
