using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace PremiumLoader
{
    public partial class MainWindow : Window
    {
        private readonly LoadingService _loadingService;
        private bool _isLoading = false;
        private DispatcherTimer? _statusDotTimer;
        private DispatcherTimer? _processWatchTimer;
        private DispatcherTimer? _cmdMonitorTimer;
        private bool _gameDetected = false;
        private bool _injectionStarted = false;

        public MainWindow()
        {
            InitializeComponent();
            _loadingService = new LoadingService();
            
            // Initialize UI components first
            InitializeUI();
            
            // Set up window to be invisible but still trigger events
            // Keep the proper styling from XAML but move off-screen
            this.Left = -10000; // Move off-screen
            this.Top = -10000;
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Minimized;
            
            this.Loaded += OnWindowLoaded;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window loaded event fired, starting Steam check...");
            
            // Keep window hidden during Steam check
            this.Visibility = Visibility.Hidden;
            
            try
            {
                await CheckAndCloseSteamBeforeLoader();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnWindowLoaded: {ex.Message}");
                MessageBox.Show($"Error during startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeUI()
        {
            // Set initial status (for when loader is shown)
            UpdateStatus("Ready", Colors.LimeGreen, "System ready for loading");
            
            // Start status dot animation (for when loader is shown)
            StartStatusDotAnimation();
        }

        private async Task CheckAndCloseSteamBeforeLoader()
        {
            try
            {
                Console.WriteLine("Checking for Steam processes before showing loader...");
                var steamProcesses = Process.GetProcessesByName("steam").ToList();
                
                if (steamProcesses.Any())
                {
                    Console.WriteLine($"Found {steamProcesses.Count} Steam process(es). Closing them...");
                    
                    // Kill all Steam processes
                    foreach (var process in steamProcesses)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error killing Steam process: {ex.Message}");
                        }
                    }
                    
                    // Wait 2 seconds to be sure Steam is closed
                    Console.WriteLine("Waiting 2 seconds for Steam to fully close...");
                    await Task.Delay(2000);
                    
                    // Verify Steam is actually closed
                    var remainingProcesses = Process.GetProcessesByName("steam");
                    if (remainingProcesses.Length > 0)
                    {
                        Console.WriteLine($"Warning: {remainingProcesses.Length} Steam process(es) still running");
                    }
                    else
                    {
                        Console.WriteLine("Steam processes closed successfully.");
                    }
                    
                    // Show message that Steam is closed
                    var result = MessageBox.Show(
                        "Steam has been closed for security reasons.\n\nThis ensures the loader can operate safely without interference.\n\nClick OK to show the CS2 Loader.",
                        "Steam Closed - Security Measure",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    Console.WriteLine("No Steam processes found.");
                    
                    // Still show message for consistency
                    var result = MessageBox.Show(
                        "Steam check completed.\n\nClick OK to show the CS2 Loader.",
                        "CS2 Loader Ready",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                // NOW show the loader after user clicks OK
                await ShowLoader();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during Steam check: {ex.Message}");
                var result = MessageBox.Show(
                    $"Error during initialization: {ex.Message}\n\nClick OK to continue anyway.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                await ShowLoader();
            }
        }

        private async Task ShowLoader()
        {
            Console.WriteLine("Showing CS2 Loader interface...");
            
            // Restore proper window positioning and visibility
            // Don't change AllowsTransparency or WindowStyle as they're set in XAML
            this.Left = (SystemParameters.PrimaryScreenWidth - 1200) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - 800) / 2;
            this.WindowState = WindowState.Normal;
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            this.Activate();
            this.Focus();
            
            // Animate the loader entrance
            AnimateWindowEntry();
            
            Console.WriteLine("CS2 Loader is now ready for use.");
        }

        private async Task PerformLoading()
        {
            _isLoading = true;
            
            try
            {
                // Phase 1: Initial loading with lazy delay
                await InitialLoadingPhase();
                
                // Phase 2: Hide main UI and show progress bar
                await HideMainUIAndShowProgress();
                
                // Phase 3: Start Steam and show CS2 page
                await StartSteamWithCS2();
                
                // Phase 4: Wait for user to start game
                await WaitForGameStart();
                
                // Phase 5: Wait for lobby time and inject
                await WaitAndInject();
                
                // Phase 6: Close application
                await CloseApplication();
            }
            catch (Exception ex)
            {
                UpdateStatus("Error", Colors.Red, $"Loading error: {ex.Message}");
                PlayErrorFeedback();
                _isLoading = false;
                LoadButton.IsEnabled = true;
                LoadButton.Content = "LOAD NOW";
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async Task InitialLoadingPhase()
        {
            Console.WriteLine("=== Phase 1: Initial Loading ===");
            
            // Disable button and show progress
            LoadButton.IsEnabled = false;
            LoadButton.Content = "LOADING...";
            ProgressBar.Visibility = Visibility.Visible;
            
            UpdateStatus("Loading", Colors.Orange, "Preparing advanced loading system...");
            
            // Random delay between 5-10 seconds
            var random = new Random();
            int delay = random.Next(5000, 10001); // 5-10 seconds
            Console.WriteLine($"Lazy loading for {delay}ms...");
            
            // Simulate different loading phases
            UpdateStatus("Analyzing", Colors.Orange, "Analyzing system configuration...");
            await Task.Delay(delay / 4);
            
            UpdateStatus("Preparing", Colors.Orange, "Preparing security bypass modules...");
            await Task.Delay(delay / 4);
            
            UpdateStatus("Configuring", Colors.Orange, "Configuring injection parameters...");
            await Task.Delay(delay / 4);
            
            UpdateStatus("Initializing", Colors.Orange, "Initializing loader subsystems...");
            await Task.Delay(delay / 4);
        }

        private async Task HideMainUIAndShowProgress()
        {
            Console.WriteLine("=== Phase 2: Hiding Main UI ===");
            
            // Hide the entire window
            this.Visibility = Visibility.Hidden;
            
            // Show standalone progress bar in a new minimal window
            await ShowStandaloneProgressBar();
        }

        private async Task ShowStandaloneProgressBar()
        {
            Console.WriteLine("Showing standalone progress bar...");
            
            // Create a new minimal window for progress bar only
            var progressWindow = new Window
            {
                Title = "CS2 Loader",
                Width = 400,
                Height = 150,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true,
                ShowInTaskbar = false
            };
            
            var progressBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 22, 27, 34)),
                CornerRadius = new CornerRadius(16),
                //Padding = new Thickness(40, 30),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 10,
                    BlurRadius = 30,
                    Opacity = 0.8
                }
            };
            
            var progressStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var progressText = new TextBlock
            {
                Text = "Initializing Steam Integration...",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            
            var progressBarCustom = new ProgressBar
            {
                Width = 300,
                Height = 8,
                Background = new SolidColorBrush(Color.FromArgb(255, 48, 54, 61)),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 88, 166, 255)),
                IsIndeterminate = true
            };
            
            progressStack.Children.Add(progressText);
            progressStack.Children.Add(progressBarCustom);
            progressBorder.Child = progressStack;
            progressWindow.Content = progressBorder;
            
            // Show the progress window
            progressWindow.Show();
            
            // Random duration for progress
            var random = new Random();
            int progressDuration = random.Next(8000, 15001); // 8-15 seconds
            Console.WriteLine($"Progress duration: {progressDuration}ms");
            
            await Task.Delay(progressDuration);
            
            // Close progress window
            progressWindow.Close();
        }

        private async Task StartSteamWithCS2()
        {
            Console.WriteLine("=== Phase 3: Starting Steam with CS2 ===");
            
            try
            {
                // Find Steam installation
                string steamPath = GetSteamPath();
                if (string.IsNullOrEmpty(steamPath))
                {
                    throw new Exception("Steam installation not found");
                }
                
                Console.WriteLine($"Found Steam at: {steamPath}");
                
                // Start Steam with CS2 library page
                var steamProcess = new ProcessStartInfo
                {
                    FileName = steamPath,
                    Arguments = "steam://nav/games/details/730",
                    UseShellExecute = true
                };
                
                Process.Start(steamProcess);
                Console.WriteLine("Steam started with CS2 page");
                
                // Wait longer for Steam to fully load and show the CS2 page
                Console.WriteLine("Waiting for Steam to fully initialize...");
                await Task.Delay(8000); // Increased wait time
                
                Console.WriteLine("Steam should now be showing the CS2 page");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting Steam: {ex.Message}");
                throw;
            }
        }

        private string GetSteamPath()
        {
            try
            {
                // Try registry first
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    var steamPath = key?.GetValue("SteamExe") as string;
                    if (!string.IsNullOrEmpty(steamPath) && File.Exists(steamPath))
                    {
                        return steamPath;
                    }
                }
                
                // Try common locations
                string[] commonPaths = {
                    @"C:\Program Files (x86)\Steam\steam.exe",
                    @"C:\Program Files\Steam\steam.exe",
                    @"D:\Steam\steam.exe",
                    @"E:\Steam\steam.exe"
                };
                
                foreach (string path in commonPaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task WaitForGameStart()
        {
            Console.WriteLine("=== Phase 4: Waiting for Game Start ===");
            
            MessageBox.Show(
                "Steam has been opened with the CS2 library page.\n\nPlease start Counter-Strike 2 now.\n\nThe loader will automatically detect when the game starts.",
                "Start Counter-Strike 2",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            // Start monitoring for CS2 process
            _gameDetected = false;
            _processWatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            
            _processWatchTimer.Tick += async (s, e) =>
            {
                var cs2Processes = Process.GetProcessesByName("cs2");
                if (cs2Processes.Length > 0 && !_gameDetected && !_injectionStarted)
                {
                    Console.WriteLine("CS2 process detected! Waiting for main window...");
                    
                    // Stop timer immediately to prevent multiple detections
                    _processWatchTimer?.Stop();
                    _gameDetected = true;
                    _injectionStarted = true;
                    
                    // Wait for the main window to appear
                    bool windowFound = false;
                    int windowWaitAttempts = 0;
                    
                    while (!windowFound && windowWaitAttempts < 30) // Wait up to 60 seconds
                    {
                        await Task.Delay(2000);
                        windowWaitAttempts++;
                        
                        var processes = Process.GetProcessesByName("cs2");
                        foreach (var proc in processes)
                        {
                            if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                            {
                                windowFound = true;
                                Console.WriteLine($"CS2 window found: '{proc.MainWindowTitle}'");
                                break;
                            }
                        }
                        
                        if (!windowFound)
                        {
                            Console.WriteLine($"Waiting for CS2 window... attempt {windowWaitAttempts}/30");
                        }
                    }
                    
                    if (windowFound)
                    {
                        Console.WriteLine("CS2 window detected! Playing beep sound...");
                        
                        // Play real beep sound
                        PlayBeepSound();
                        
                        // Continue to next phase
                        await WaitAndInject();
                    }
                    else
                    {
                        Console.WriteLine("CS2 process found but window not detected, resetting detection...");
                        _gameDetected = false;
                        _injectionStarted = false;
                        _processWatchTimer?.Start();
                    }
                }
            };
            
            _processWatchTimer.Start();
            Console.WriteLine("Monitoring for CS2 process...");
            
            // Prevent method from continuing until game is detected
            while (!_gameDetected)
            {
                await Task.Delay(500);
            }
        }

        private void PlayBeepSound()
        {
            try
            {
                // Single beep - simple and clean
                Task.Run(() =>
                {
                    Console.Beep(1000, 300); // 1000 Hz for 300ms
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not play beep: {ex.Message}");
                // Fallback to system sound
                try
                {
                    System.Media.SystemSounds.Asterisk.Play();
                }
                catch { }
            }
        }

        private async Task WaitAndInject()
        {
            Console.WriteLine("=== Phase 5: Waiting for Lobby and Injecting ===");
            
            // Random wait time between 40 seconds and 1 minute
            var random = new Random();
            int waitTime = random.Next(40000, 60001); // 40-60 seconds
            Console.WriteLine($"Waiting {waitTime}ms for user to reach lobby...");
            
            await Task.Delay(waitTime);
            
            Console.WriteLine("Performing injection...");
            
            try
            {
                // Get the DLL path
                string dllPath = GetDllPath();
                
                if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                {
                    throw new Exception("DLL file not found");
                }
                
                // Start monitoring for post-injection issues BEFORE injection
                StartPostInjectionMonitoring();
                
                // Perform the actual injection
                bool success = await Task.Run(() => _loadingService.LoadDll(dllPath));
                
                if (success)
                {
                    Console.WriteLine("Injection successful!");
                    
                    // Play single success beep
                    try
                    {
                        Task.Run(() =>
                        {
                            Console.Beep(1200, 200); // Single high success tone
                        });
                    }
                    catch 
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                    }
                    
                    // Wait a bit for post-injection monitoring to handle issues
                    Console.WriteLine("Monitoring for post-injection issues...");
                    await Task.Delay(5000); // Give 5 seconds for issues to appear
                    
                    // Don't show message box, just close
                    await CloseApplication();
                }
                else
                {
                    throw new Exception("Injection failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Injection error: {ex.Message}");
                MessageBox.Show(
                    $"Loading failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StartPostInjectionMonitoring()
        {
            Console.WriteLine("Starting post-injection monitoring...");
            
            // Monitor for command windows and browser redirects
            _cmdMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Check every 500ms
            };
            
            _cmdMonitorTimer.Tick += async (s, e) =>
            {
                await HandlePostInjectionIssues();
            };
            
            _cmdMonitorTimer.Start();
            
            // Stop monitoring after 10 seconds
            Task.Run(async () =>
            {
                await Task.Delay(10000);
                _cmdMonitorTimer?.Stop();
                Console.WriteLine("Post-injection monitoring stopped.");
            });
        }

        private async Task HandlePostInjectionIssues()
        {
            try
            {
                // Handle command window with "t.me/panhauzer" title
                await HandleCommandWindow();
                
                // Handle browser redirect attempts
                await HandleBrowserRedirect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling post-injection issues: {ex.Message}");
            }
        }

        private async Task HandleCommandWindow()
        {
            try
            {
                // Find command windows with the specific title
                var cmdProcesses = Process.GetProcessesByName("cmd")
                    .Where(p => !p.HasExited && p.MainWindowHandle != IntPtr.Zero)
                    .ToList();

                foreach (var process in cmdProcesses)
                {
                    string windowTitle = process.MainWindowTitle?.ToLower() ?? "";
                    
                    if (windowTitle.Contains("t.me/panhauzer"))
                    {
                        Console.WriteLine($"Found unwanted CMD window: '{process.MainWindowTitle}'");
                        
                        // Rename the window title
                        SetWindowText(process.MainWindowHandle, "t.me/enclavecircle");
                        Console.WriteLine("Renamed CMD window to 't.me/enclavecircle'");
                        
                        // Wait 3 seconds then close it
                        await Task.Delay(3000);
                        
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                                Console.WriteLine("Closed unwanted CMD window");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error closing CMD window: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling command window: {ex.Message}");
            }
        }

        private async Task HandleBrowserRedirect()
        {
            try
            {
                // Monitor for browser processes that might be opening the unwanted URL
                var browserProcessNames = new[] { "chrome", "firefox", "msedge", "opera", "brave", "iexplore" };
                
                foreach (string browserName in browserProcessNames)
                {
                    var browserProcesses = Process.GetProcessesByName(browserName)
                        .Where(p => !p.HasExited)
                        .ToList();

                    foreach (var process in browserProcesses)
                    {
                        // Check if this is a newly created browser process (likely for redirect)
                        if (process.StartTime > DateTime.Now.AddSeconds(-10)) // Started in last 10 seconds
                        {
                            string windowTitle = process.MainWindowTitle?.ToLower() ?? "";
                            
                            // Check if it's trying to open the unwanted URL
                            if (windowTitle.Contains("t.me/panhauzer") || 
                                windowTitle.Contains("panhauzer") ||
                                process.ProcessName.Contains("temp") || // Temporary browser instances
                                process.MainWindowTitle == "" || // New empty browser window
                                windowTitle.Contains("new tab"))
                            {
                                Console.WriteLine($"Found suspicious browser process: {process.ProcessName} - '{process.MainWindowTitle}'");
                                
                                try
                                {
                                    // Close the browser process before it can load the page
                                    process.Kill();
                                    Console.WriteLine($"Closed suspicious browser process: {process.ProcessName}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error closing browser process: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                // Also check for any newly created processes that might be URL handlers
                var allProcesses = Process.GetProcesses()
                    .Where(p => !p.HasExited && p.StartTime > DateTime.Now.AddSeconds(-5))
                    .ToList();

                foreach (var process in allProcesses)
                {
                    try
                    {
                        string processName = process.ProcessName?.ToLower() ?? "";
                        string windowTitle = process.MainWindowTitle?.ToLower() ?? "";
                        
                        // Check for URL handler processes or command line arguments containing the unwanted URL
                        if (processName.Contains("url") || 
                            processName.Contains("link") ||
                            processName.Contains("telegram") ||
                            windowTitle.Contains("t.me/panhauzer") ||
                            windowTitle.Contains("panhauzer"))
                        {
                            Console.WriteLine($"Found potential redirect process: {process.ProcessName} - '{process.MainWindowTitle}'");
                            
                            try
                            {
                                process.Kill();
                                Console.WriteLine($"Closed potential redirect process: {process.ProcessName}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error closing redirect process: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip processes we can't access
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling browser redirect: {ex.Message}");
            }
        }

        // Win32 API to change window title
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetWindowText(IntPtr hwnd, string lpString);

        private async Task CloseApplication()
        {
            Console.WriteLine("=== Phase 6: Closing Application ===");
            
            // Small delay to ensure beep is heard
            await Task.Delay(500);
            
            Console.WriteLine("🚪 Closing CS2 Loader...");
            
            // Close application immediately after fixes are complete
            Application.Current.Shutdown();
        }

        private void AnimateWindowEntry()
        {
            // Fade in the entire window
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(800))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(OpacityProperty, fadeIn);

            // Scale in the game card with better easing
            var scaleInStoryboard = new Storyboard();
            
            var scaleXAnimation = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(1000))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };
            
            var scaleYAnimation = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(1000))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };
            
            Storyboard.SetTarget(scaleXAnimation, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            Storyboard.SetTarget(scaleYAnimation, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            scaleInStoryboard.Children.Add(scaleXAnimation);
            scaleInStoryboard.Children.Add(scaleYAnimation);
            
            scaleInStoryboard.Begin();
        }

        private void StartStatusDotAnimation()
        {
            _statusDotTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000)
            };
            _statusDotTimer.Tick += (s, e) =>
            {
                var pulseStoryboard = new Storyboard();
                
                var pulseAnimation = new DoubleAnimationUsingKeyFrames();
                pulseAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
                pulseAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600))));
                
                Storyboard.SetTarget(pulseAnimation, StatusDotScale);
                Storyboard.SetTargetProperty(pulseAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
                
                var pulseAnimationY = pulseAnimation.Clone();
                Storyboard.SetTarget(pulseAnimationY, StatusDotScale);
                Storyboard.SetTargetProperty(pulseAnimationY, new PropertyPath(ScaleTransform.ScaleYProperty));
                
                pulseStoryboard.Children.Add(pulseAnimation);
                pulseStoryboard.Children.Add(pulseAnimationY);
                pulseStoryboard.Begin();
            };
            _statusDotTimer.Start();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Smooth minimize animation
            var minimizeStoryboard = new Storyboard();
            var minimizeAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            Storyboard.SetTarget(minimizeAnimation, this);
            Storyboard.SetTargetProperty(minimizeAnimation, new PropertyPath(OpacityProperty));
            minimizeStoryboard.Children.Add(minimizeAnimation);
            
            minimizeStoryboard.Completed += (s, args) => this.WindowState = WindowState.Minimized;
            minimizeStoryboard.Begin();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Smooth close animation with scale
            var closeStoryboard = new Storyboard();
            
            var fadeAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var scaleAnimation = new DoubleAnimation(1, 0.9, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            Storyboard.SetTarget(fadeAnimation, this);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
            
            Storyboard.SetTarget(scaleAnimation, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            var scaleAnimationY = scaleAnimation.Clone();
            Storyboard.SetTarget(scaleAnimationY, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            closeStoryboard.Children.Add(fadeAnimation);
            closeStoryboard.Children.Add(scaleAnimation);
            closeStoryboard.Children.Add(scaleAnimationY);
            
            closeStoryboard.Completed += (s, args) => this.Close();
            closeStoryboard.Begin();
        }

        private void GameCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isLoading) return;

            var hoverStoryboard = new Storyboard();
            
            var scaleAnimation = new DoubleAnimation
            {
                To = 1.02,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleAnimationY = scaleAnimation.Clone();
            
            Storyboard.SetTarget(scaleAnimation, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            Storyboard.SetTarget(scaleAnimationY, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            hoverStoryboard.Children.Add(scaleAnimation);
            hoverStoryboard.Children.Add(scaleAnimationY);
            hoverStoryboard.Begin();
        }

        private void GameCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isLoading) return;

            var leaveStoryboard = new Storyboard();
            
            var scaleAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleAnimationY = scaleAnimation.Clone();
            
            Storyboard.SetTarget(scaleAnimation, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            Storyboard.SetTarget(scaleAnimationY, CardScaleTransform);
            Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            leaveStoryboard.Children.Add(scaleAnimation);
            leaveStoryboard.Children.Add(scaleAnimationY);
            leaveStoryboard.Begin();
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            await PerformLoading();
        }

        private void UpdateStatus(string status, Color indicatorColor, string message)
        {
            // Smooth status transition animation
            var statusStoryboard = new Storyboard();
            
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            Storyboard.SetTarget(fadeOut, StatusIndicator);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            
            statusStoryboard.Children.Add(fadeOut);
            statusStoryboard.Completed += (s, e) =>
            {
                // Update content
                StatusText.Text = status.ToUpper();
                StatusMessage.Text = message;
                StatusDot.Fill = new SolidColorBrush(indicatorColor);
                StatusIndicator.Background = new SolidColorBrush(indicatorColor);
                StatusBarDot.Fill = new SolidColorBrush(indicatorColor);
                
                // Fade back in
                var fadeInStoryboard = new Storyboard();
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                Storyboard.SetTarget(fadeIn, StatusIndicator);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
                fadeInStoryboard.Children.Add(fadeIn);
                fadeInStoryboard.Begin();
            };
            statusStoryboard.Begin();
        }

        private void PlaySuccessFeedback()
        {
            try
            {
                // Play system success sound
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { /* Ignore sound errors */ }
        }

        private void PlayErrorFeedback()
        {
            try
            {
                // Play system error sound
                System.Media.SystemSounds.Hand.Play();
            }
            catch { /* Ignore sound errors */ }
        }

        private string? GetDllPath()
        {
            try
            {
                Console.WriteLine("=== DLL Search Debug ===");
                
                // Priority 1: Embedded resource
                Console.WriteLine("Checking embedded resource...");
                string? embeddedPath = ExtractEmbeddedDll();
                if (!string.IsNullOrEmpty(embeddedPath))
                {
                    Console.WriteLine($"Found embedded DLL at: {embeddedPath}");
                    return embeddedPath;
                }
                Console.WriteLine("No embedded DLL found.");

                // Priority 2: Relative to executable
                string relativePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loader.dll");
                Console.WriteLine($"Checking relative path: {relativePath}");
                if (File.Exists(relativePath))
                {
                    Console.WriteLine("Found relative DLL.");
                    return relativePath;
                }

                // Priority 3: Common locations
                string[] commonPaths = {
                    @".\dlls\loader.dll",
                    @".\resources\loader.dll",
                    @".\Resources\loader.dll",
                    @"C:\CS2Loader\loader.dll"
                };

                Console.WriteLine("Checking common paths...");
                foreach (string path in commonPaths)
                {
                    Console.WriteLine($"  Checking: {Path.GetFullPath(path)}");
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"  Found: {path}");
                        return path;
                    }
                }

                Console.WriteLine("No DLL found in any location.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDllPath: {ex.Message}");
                return null;
            }
        }

        private string? ExtractEmbeddedDll()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Console.WriteLine($"Assembly: {assembly.FullName}");
                
                // List all embedded resources for debugging
                string[] resourceNames = assembly.GetManifestResourceNames();
                Console.WriteLine("Available embedded resources:");
                foreach (string name in resourceNames)
                {
                    Console.WriteLine($"  - {name}");
                }

                // The correct resource name is "Loader.Resources.loader.dll" based on your output
                string correctResourceName = "Loader.Resources.loader.dll";
                
                Console.WriteLine($"Using correct resource name: {correctResourceName}");
                using (Stream? stream = assembly.GetManifestResourceStream(correctResourceName))
                {
                    if (stream != null)
                    {
                        Console.WriteLine($"Found embedded resource: {correctResourceName}");
                        string tempPath = Path.Combine(Path.GetTempPath(), $"cs2_loader_{Guid.NewGuid().ToString("N")[..8]}.dll");
                        Console.WriteLine($"Extracting to: {tempPath}");
                        
                        using (FileStream fileStream = File.Create(tempPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                        
                        Console.WriteLine($"Extracted {new FileInfo(tempPath).Length} bytes");
                        return tempPath;
                    }
                    else
                    {
                        Console.WriteLine("Embedded resource stream is null");
                    }
                }

                Console.WriteLine("No embedded DLL resource found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting embedded DLL: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _statusDotTimer?.Stop();
            _processWatchTimer?.Stop();
            _cmdMonitorTimer?.Stop();
            base.OnClosing(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // Restore opacity when minimized
                this.Opacity = 1;
            }
            base.OnStateChanged(e);
        }
    }
}