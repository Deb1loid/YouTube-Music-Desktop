#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Text.RegularExpressions; // Добавлено для Regex

namespace YouTubeMusic
{
    public partial class Form1 : Form
    {
        private WebView2? webView;
        private Panel? titleBar;
        private Label? titleLabel;
        private Button? closeButton;
        private Button? minimizeButton;
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        
        // Хранилище для загруженных DLL
        private static Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        
        private static string? _tempFolder;
        private static string? _loaderPath;

        public Form1()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            ExtractWebView2Loader();
            InitializeForm();
            CreateTrayIcon();
            CreateTitleBar();
            InitializeWebView();
        }
        
        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (args.Name == null) return null;
            
            string assemblyName = new AssemblyName(args.Name).Name ?? string.Empty;
            
            if (string.IsNullOrEmpty(assemblyName)) return null;
            
            if (_loadedAssemblies.ContainsKey(assemblyName))
                return _loadedAssemblies[assemblyName];
            
            string resourceName = $"YouTubeMusic.{assemblyName}.dll";
            
            using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                
                byte[] assemblyData = new byte[stream.Length];
                int offset = 0;
                while (offset < assemblyData.Length)
                {
                    int bytesRead = stream.Read(assemblyData, offset, assemblyData.Length - offset);
                    if (bytesRead <= 0) break;
                    offset += bytesRead;
                }
                
                Assembly assembly = Assembly.Load(assemblyData);
                _loadedAssemblies[assemblyName] = assembly;
                return assembly;
            }
        }
        
        private void ExtractWebView2Loader()
        {
            try
            {
                _tempFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "YouTubeMusic",
                    "WebView2Loader"
                );
                
                if (!Directory.Exists(_tempFolder))
                    Directory.CreateDirectory(_tempFolder);
                
                _loaderPath = Path.Combine(_tempFolder, "WebView2Loader.dll");
                
                using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeMusic.WebView2Loader.dll"))
                {
                    if (stream == null)
                        return;
                    
                    byte[] data = new byte[stream.Length];
                    int offset = 0;
                    while (offset < data.Length)
                    {
                        int bytesRead = stream.Read(data, offset, data.Length - offset);
                        if (bytesRead <= 0) break;
                        offset += bytesRead;
                    }
                    File.WriteAllBytes(_loaderPath, data);
                }
                
                SetDllDirectory(_tempFolder);
                LoadLibrary(_loaderPath);
            }
            catch
            {
                // Пропускаем ошибки извлечения
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int value = DWMWCP_ROUND;
            DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int));
        }

        private void InitializeForm()
        {
            this.Text = "YouTube Music";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(0, 0, 0);
            this.MinimumSize = new Size(800, 600);
            
            this.FormClosing += Form1_FormClosing;
            
            try
            {
                using var stream = GetEmbeddedResource("ico.ico");
                if (stream != null)
                {
                    this.Icon = new Icon(stream);
                }
            }
            catch { }
        }

        private void CreateTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("Открыть YouTube Music");
            showItem.Click += (s, e) => ShowFromTray();
            showItem.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            
            var separator = new ToolStripSeparator();
            
            var exitItem = new ToolStripMenuItem("Закрыть");
            exitItem.Click += (s, e) => ExitApplication();
            
            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(separator);
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon
            {
                Text = "YouTube Music",
                ContextMenuStrip = trayMenu,
                Visible = false
            };

            try
            {
                using var stream = GetEmbeddedResource("ico.ico");
                if (stream != null)
                {
                    trayIcon.Icon = new Icon(stream);
                }
            }
            catch { }

            trayIcon.DoubleClick += (s, e) => ShowFromTray();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
            }
        }

        private void HideToTray()
        {
            this.Hide();
            if (trayIcon != null)
            {
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(3000, "YouTube Music", "Приложение свернуто в трей", ToolTipIcon.Info);
            }
        }

        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }
        }

        private void ExitApplication()
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            
            this.FormClosing -= Form1_FormClosing;
            Application.Exit();
        }

        private void CreateTitleBar()
        {
            titleBar = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(0, 0, 0)
            };

            titleLabel = new Label
            {
                Text = "YouTube Music (открыто в браузере по умолчанию)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(12, 6),
                AutoSize = true
            };

            titleLabel.ContextMenuStrip = new ContextMenuStrip();
            
            var openInBrowserItem = new ToolStripMenuItem("Открыть в браузере");
            openInBrowserItem.Click += (s, e) =>
            {
                if (webView?.CoreWebView2 != null)
                {
                    OpenInDefaultBrowser(webView.CoreWebView2.Source);
                }
                else
                {
                    MessageBox.Show("WebView еще не инициализирован", "Информация", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            titleLabel.ContextMenuStrip.Items.Add(openInBrowserItem);
            
            var separatorMenu = new ToolStripSeparator();
            titleLabel.ContextMenuStrip.Items.Add(separatorMenu);
            
            var exitMenuItem = new ToolStripMenuItem("Выйти");
            exitMenuItem.Click += (s, e) => ExitApplication();
            titleLabel.ContextMenuStrip.Items.Add(exitMenuItem);

            minimizeButton = CreateImageButton("minimize.png", this.Width - 60, 3, 22, 22);
            minimizeButton.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            closeButton = CreateImageButton("close.png", this.Width - 32, 3, 22, 22);
            closeButton.Click += (s, e) => this.Close();
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(232, 17, 35);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.Transparent;

            titleBar.Controls.AddRange(new Control[] 
            { 
                titleLabel, minimizeButton, closeButton 
            });

            titleBar.MouseDown += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            
            titleLabel.MouseDown += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            this.Controls.Add(titleBar);
        }

        private Button CreateImageButton(string resourceName, int x, int y, int width, int height)
        {
            Button button = new Button
            {
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                Size = new Size(width, height),
                Location = new Point(x, y),
                Text = "",
                ImageAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            try
            {
                using var stream = GetEmbeddedResource(resourceName);
                if (stream != null)
                {
                    button.Image = new Bitmap(stream);
                    button.Image = new Bitmap(button.Image, new Size(14, 14));
                }
                else
                {
                    button.Text = GetFallbackSymbol(resourceName);
                    button.ForeColor = Color.White;
                    button.Font = new Font("Segoe UI", 8);
                    button.TextAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch
            {
                button.Text = GetFallbackSymbol(resourceName);
                button.ForeColor = Color.White;
                button.Font = new Font("Segoe UI", 8);
                button.TextAlign = ContentAlignment.MiddleCenter;
            }

            button.MouseEnter += (s, e) => 
            {
                if (button != closeButton)
                    button.BackColor = Color.FromArgb(60, 60, 60);
            };
            button.MouseLeave += (s, e) => 
            {
                if (button != closeButton)
                    button.BackColor = Color.Transparent;
            };

            return button;
        }

        private Stream? GetEmbeddedResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"YouTubeMusic.{name}";
            return assembly.GetManifestResourceStream(resourceName);
        }

        private string GetFallbackSymbol(string resourceName)
        {
            string filename = Path.GetFileNameWithoutExtension(resourceName).ToLower();
            return filename switch
            {
                "minimize" => "─",
                "close" => "✕",
                _ => "●"
            };
        }

        /// <summary>
        /// Получить имя браузера по умолчанию (без цифр и лишних символов)
        /// </summary>
        private string GetDefaultBrowserName()
        {
            try
            {
                string httpPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
                string? progId = Registry.GetValue(httpPath, "Progid", null)?.ToString();
                
                if (string.IsNullOrEmpty(progId))
                    return "браузере по умолчанию";
                
                // Убираем все цифры и лишние символы
                string cleanProgId = Regex.Replace(progId, @"[^a-zA-Z.]", "");
                
                if (cleanProgId.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                    return "Chrome";
                if (cleanProgId.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                    return "Firefox";
                if (cleanProgId.Contains("Edge", StringComparison.OrdinalIgnoreCase))
                    return "Edge";
                if (cleanProgId.Contains("Opera", StringComparison.OrdinalIgnoreCase))
                    return "Opera";
                if (cleanProgId.Contains("Brave", StringComparison.OrdinalIgnoreCase))
                    return "Brave";
                if (cleanProgId.Contains("Vivaldi", StringComparison.OrdinalIgnoreCase))
                    return "Vivaldi";
                if (cleanProgId.Contains("Yandex", StringComparison.OrdinalIgnoreCase))
                    return "Яндекс Браузер";
                if (cleanProgId.Contains("IE", StringComparison.OrdinalIgnoreCase))
                    return "Internet Explorer";
                
                return "браузере по умолчанию";
            }
            catch
            {
                return "браузере по умолчанию";
            }
        }

        private async void InitializeWebView()
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YouTubeMusic",
                "WebView2"
            );

            try
            {
                var env = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder
                );

                webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };
                
                this.Controls.Add(webView);
                webView.BringToFront();
                
                await webView.EnsureCoreWebView2Async(env);
                
                webView.CoreWebView2.NewWindowRequested += WebView_NewWindowRequested;
                webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
                
                webView.CoreWebView2.Navigate("https://music.youtube.com");
                
                webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        if (titleLabel != null)
                        {
                            string title = webView?.CoreWebView2?.DocumentTitle ?? "YouTube Music";
                            string browserName = GetDefaultBrowserName();
                            titleLabel.Text = $"{title} (открыто в {browserName})";
                        }
                    }));
                };
            }
            catch (Exception ex)
            {
                var result = MessageBox.Show(
                    $"Для работы программы требуется WebView2 Runtime.\n\nСкачать и установить сейчас?\n\nОшибка: {ex.Message}", 
                    "YouTube Music", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Error);
                    
                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                }
                Application.Exit();
            }
        }

        private void OpenInDefaultBrowser(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть браузер: {ex.Message}",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void WebView_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            OpenInDefaultBrowser(e.Uri);
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!e.Uri.Contains("music.youtube.com") && 
                !e.Uri.Contains("youtube.com") &&
                !e.Uri.Contains("accounts.google.com") &&
                !e.Uri.Contains("google.com"))
            {
                e.Cancel = true;
                OpenInDefaultBrowser(e.Uri);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (minimizeButton != null && closeButton != null)
            {
                minimizeButton.Location = new Point(this.Width - 60, 3);
                closeButton.Location = new Point(this.Width - 32, 3);
            }
        }
    }
}
