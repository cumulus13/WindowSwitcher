// File: MainWindow.xaml.cs
// Author: Hadi Cahyadi <cumulus13@gmail.com>
// Date: 2025-12-23
// Description: 
// License: MIT

using System;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WindowSwitcher.Models;
using WindowSwitcher.Services;
// using WindowSwitcher.Helpers;
using System.Windows.Controls;
using System.Text.RegularExpressions;
// using System.Threading.Tasks;
// using System.Linq;

namespace WindowSwitcher;

public partial class MainWindow : Window
{
    private bool _debugMode = false;
    private string _logFile = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", 
        "WindowSwitcher_Debug.txt"
    );
    private readonly WindowManagerService _windowManager;
    // private readonly WindowCacheService _windowCache;
    private readonly ConfigService _configService;
    private readonly HotkeyService _hotkeyService;
    private ObservableCollection<WindowInfo> _allWindows;
    private ObservableCollection<WindowInfo> _filteredWindows;
    private AppSettings _settings = null!;
    
    // Track display mode
    private bool _followCursorMode = true;

    private void Log(string message)
    {
        if (!_debugMode) return;
        
        try
        {
            File.AppendAllText(_logFile, $"{DateTime.Now:HH:mm:ss} - {message}\n");
        }
        catch { }
    }

    public MainWindow()
    {
        InitializeComponent();
        
        _windowManager = new WindowManagerService();
        _configService = new ConfigService();
        _settings = _configService.LoadSettings();
        
        // _windowCache = new WindowCacheService(
        //     _windowManager, 
        //     _settings.CacheSeconds, 
        //     _settings.AutoRefreshSeconds
        // );

        _hotkeyService = new HotkeyService(this);
        
        _allWindows = new ObservableCollection<WindowInfo>();
        _filteredWindows = new ObservableCollection<WindowInfo>();
        
        ResultsList.ItemsSource = _filteredWindows;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Load settings
            _settings = _configService.LoadSettings();
            _followCursorMode = _settings.FollowCursor;
            
            // Apply theme
            ApplyTheme(_settings.DarkTheme);
            
            // Register main hotkey
            _hotkeyService.RegisterHotkey(
                _settings.HotkeyModifier,
                _settings.HotkeyKey,
                () => Dispatcher.Invoke(ToggleWindow),
                hotkeyId: 9000
            );
            
            // Register toggle mode hotkey
            _hotkeyService.RegisterHotkey(
                _settings.ToggleModeModifier,
                _settings.ToggleModeKey,
                () => Dispatcher.Invoke(ToggleDisplayMode),
                hotkeyId: 9001
            );
            
            // Hide window after hotkey is registered
            Hide();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ToggleDisplayMode()
    {
        _followCursorMode = !_followCursorMode;
        
        string mode = _followCursorMode ? "Follow Cursor" : "Primary Monitor";
        
        // Show temporary notification window
        var notif = new Window
        {
            Width = 300,
            Height = 100,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Child = new TextBlock
            {
                Text = $"Display Mode: {mode}",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            }
        };
        
        notif.Content = border;
        notif.Show();
        
        // Auto-close after 1.5 seconds
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        timer.Tick += (s, args) =>
        {
            timer.Stop();
            notif.Close();
        };
        timer.Start();
    }

    private void ToggleWindow()
    {
        if (IsVisible)
        {
            HideWindow();
        }
        else
        {
            ShowWindow();
        }
    }

    private void ShowWindow()
    // private async void ShowWindow()
    {
        // Refresh window list
        RefreshWindows();
        // await RefreshWindowsAsync();
        
        // Clear search
        SearchBox.Clear();
        
        // Position BEFORE showing
        PositionWindowBeforeShow();
        
        // Show and focus
        Show();
        Activate();
        SearchBox.Focus();
        
        // Select first item
        if (ResultsList.Items.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
        }
    }

    private void PositionWindowBeforeShow()
    {
        Rect workArea;
        
        if (_followCursorMode)
        {
            workArea = MonitorService.GetCursorMonitorWorkArea();
        }
        else
        {
            workArea = MonitorService.GetPrimaryMonitorWorkArea();
        }
        
        // Get DPI scale
        var dpiScale = VisualTreeHelper.GetDpi(this);
        double scaleX = dpiScale.DpiScaleX;
        double scaleY = dpiScale.DpiScaleY;
        
        // Calculate center position with DPI scaling
        double windowWidth = this.Width * scaleX;
        double windowHeight = this.Height * scaleY;
        
        this.Left = (workArea.Left + (workArea.Width - windowWidth) / 2) / scaleX;
        this.Top = (workArea.Top + (workArea.Height - windowHeight) / 2) / scaleY;
    }

    private void HideWindow()
    {
        Hide();
    }

    // private async Task LoadIconsAsync()
    // {
    //     // Take a snapshot of the CURRENT window list
    //     var windows = _allWindows.ToList();

    //     foreach (var window in windows)
    //     {
    //         // Skip if you already have an invalid icon or handle
    //         if (window?.Handle == IntPtr.Zero || window?.Icon != null)
    //             continue;

    //         // Extract icon in THREAD POOL (not UI thread)
    //         var icon = await Task.Run(() =>
    //         {
    //             try
    //             {
    //                 return IconExtractor.ExtractIcon(window.Handle, (uint)window.ProcessId);
    //             }
    //             catch
    //             {
    //                 return (ImageSource?)null;
    //             }
    //         });

    //         // Now, since we are async method with await,
    //         // context returns to UI thread → safe property update
    //         if (window?.Icon != null) window.Icon = icon;
        
    //     }
    // }

    // private void RefreshWindows()
    // {
    //     _settings = _configService.LoadSettings();
        
    //     // Step 1: Get a list of windows WITHOUT icons — QUICK!
    //     var windows = _windowManager.GetAllWindows(false); // <-- false = jangan load ikon

    //     // Step 2: Clear the UI list
    //     _allWindows.Clear();
    //     _filteredWindows.Clear();

    //     // Step 3: Fill the UI list with windows without icons
    //     foreach (var window in windows)
    //     {
    //         _allWindows.Add(window);
    //         _filteredWindows.Add(window);
    //     }

    //     // Step 4: IF settings ask to show icons, start loading icons Asynchronously & SAFELY
    //     if (_settings.ShowIcons)
    //     {
    //         // Run in the background, but update the UI properly
    //         Task.Run(() =>
    //         {
    //             foreach (var item in _allWindows.ToList()) // ToList() for secure snapshots
    //             {
    //                 if (item?.Handle == IntPtr.Zero)
    //                     continue;

    //                 ImageSource? icon = null;

    //                 try
    //                 {
    //                     // Extract in background thread — only use values, don't touch the UI!
    //                     icon = IconExtractor.ExtractIcon(item.Handle, (uint)item.ProcessId);
    //                 }
    //                 catch
    //                 {
    //                     // Ignore the error
    //                 }

    //                 // SEND UPDATES TO UI THREAD SECURELY
    //                 Dispatcher.InvokeAsync(() =>
    //                 {
    //                     // Look for items that are still valid in the UI list
    //                     var existing = _allWindows.FirstOrDefault(w => w.Handle == item.Handle);
    //                     if (existing != null)
    //                     {
    //                         existing.Icon = icon; // INotifyPropertyChanged wajib!
    //                     }
    //                 }, System.Windows.Threading.DispatcherPriority.Background);
    //             }
    //         });
    //     }
    // }

    private void RefreshWindows()
    {
        _settings = _configService.LoadSettings();
        var windows = _windowManager.GetAllWindows(_settings.ShowIcons);
        // var windows = _windowCache.GetWindows(false);
        
        _allWindows.Clear();
        _filteredWindows.Clear();
        
        var count = 0;
        foreach (var window in windows)
        {
            // if (count >= _settings.MaxListShow)
            //     break;
                
            _allWindows.Add(window);
            _filteredWindows.Add(window);
            count++;
        }
    }

    // private void RefreshWindows()
    // {
    //     _settings = _configService.LoadSettings();
        
    //     // Load WITHOUT icons first (FAST!)
    //     var windows = _windowManager.GetAllWindows(false);
        
    //     _allWindows.Clear();
    //     _filteredWindows.Clear();
        
    //     foreach (var window in windows)
    //     {
    //         _allWindows.Add(window);
    //         _filteredWindows.Add(window);
    //     }
        
    //     // THEN load icons in background (OPTIONAL)
    //     // if (_settings.ShowIcons)
    //     // {
    //     //     Task.Run(() => LoadIconsAsync());
    //     // }

    //     if (_settings.ShowIcons)
    //     {
    //         _ = LoadIconsAsync(); // Jalankan async, tapi aman
    //     }
    // }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        FilterWindows(SearchBox.Text);
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Handle arrow keys in search box
        if (e.Key == Key.Down || e.Key == Key.Up)
        {
            // Prevent textbox from handling these keys
            e.Handled = true;
            
            // Trigger the window's key handler
            Window_KeyDown(this, e);
        }
    }

    private static string ConvertWildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$";
    }

    // private void FilterWindows(string searchText)
    // {
    //     _filteredWindows.Clear();
        
    //     var query = searchText.ToLowerInvariant();
    //     var count = 0;
        
    //     Console.WriteLine($"Filtering windows with query: {query}");
    //     Console.WriteLine($"Total windows to filter: {_allWindows.Count}");
        

    //     foreach (var window in _allWindows)
    //     {           
    //         if (_debugMode)
    //         {
    //             Log($"Filter: {window.Title} ({window.ProcessName})");
    //         }
    //         // var title = window.Title ?? string.Empty;
             
    //         if (count >= _settings.MaxListShow){
    //             Log($"break: {count}: {_settings.MaxListShow}");
    //             break;
    //         }
                
    //         // if (string.IsNullOrWhiteSpace(query) ||
    //         //     window.Title.ToLowerInvariant().Contains(query) ||
    //         //     window.ProcessName.ToLowerInvariant().Contains(query))
    //         if (
    //             // string.IsNullOrWhiteSpace(query) || 
    //             window.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
    //             // window.ProcessName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || 
    //             Regex.IsMatch(window.Title, ConvertWildcardToRegex(searchText), RegexOptions.IgnoreCase) //|| 
    //             // window.Title.ToLowerInvariant().Contains(query) || 
    //             // window.ProcessName.ToLowerInvariant().Contains(query)
    //             )

    //         {
    //             _filteredWindows.Add(window);
    //             count++;
    //         } else {
    //             if (searchText.Contains('*')) 
    //             {
    //                 string regexPattern = ConvertWildcardToRegex(searchText);
    //                 if (Regex.IsMatch(window.Title, regexPattern, RegexOptions.IgnoreCase))
    //                 {
    //                     _filteredWindows.Add(window);
    //                     count++;
    //                 }
    //             }
    //         }
    //     }

    //     if (_debugMode)
    //     {
    //         Console.WriteLine($"Filtered windows count: {_filteredWindows.Count}");
    //         Log($"==== Filtered windows count: {_filteredWindows.Count} ==== ");

    //         Console.WriteLine($"ResultsList.Items.Count: {ResultsList.Items.Count}");
    //         Log($"==== ResultsList.Items.Count: {ResultsList.Items.Count} ==== ");
    //     }

    //     // Auto-select first item
    //     if (ResultsList.Items.Count > 0)
    //     {
    //         ResultsList.SelectedIndex = 0;
    //     }
    // }

    private void FilterWindows(string searchText)
    {
        _filteredWindows.Clear();
        
        var query = searchText.Trim();
        var count = 0;
        
        foreach (var window in _allWindows)
        {
            if (count >= _settings.MaxListShow)
                break;
            
            bool match = false;
            
            // Empty query = show all
            if (string.IsNullOrWhiteSpace(query))
            {
                match = true;
            }
            // Wildcard pattern
            else if (query.Contains('*'))
            {
                var regex = ConvertWildcardToRegex(query);
                match = Regex.IsMatch(window.Title, regex, RegexOptions.IgnoreCase);
            }
            // Simple contains search
            else
            {
                match = window.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        window.ProcessName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            
            if (match)
            {
                _filteredWindows.Add(window);
                count++;
            }
        }
        
        if (ResultsList.Items.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                HideWindow();
                e.Handled = true;
                break;
                
            case Key.Enter:
                ActivateSelectedWindow();
                e.Handled = true;
                break;
                
            case Key.Down:
                if (ResultsList.SelectedIndex < ResultsList.Items.Count - 1)
                {
                    ResultsList.SelectedIndex++;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                e.Handled = true;
                break;
                
            case Key.Up:
                if (ResultsList.SelectedIndex > 0)
                {
                    ResultsList.SelectedIndex--;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                e.Handled = true;
                break;
        }
    }

    private void ApplyTheme(bool isDark)
    {
        if (!isDark) return;

        // Root border
        RootBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

        // ListBox
        ResultsList.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        ResultsList.Foreground = Brushes.White;

        // SearchBox
        SearchBox.Foreground = Brushes.White;
        if (SearchBox.Parent is Border searchBorder)
        {
            searchBorder.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            searchBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        }
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ActivateSelectedWindow();
    }

    private void ActivateSelectedWindow()
    {
        if (ResultsList.SelectedItem is WindowInfo windowInfo)
        {
            _windowManager.BringWindowToFront(windowInfo.Handle);
            HideWindow();
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Auto-hide when losing focus
        HideWindow();
    }

    // private async Task RefreshWindowsAsync()
    // {
    //     _settings = _configService.LoadSettings();
        
    //     // Use cache instead of direct call!
    //     var windows = await _windowCache.GetWindowsAsync(_settings.ShowIcons);
        
    //     _allWindows.Clear();
    //     _filteredWindows.Clear();
        
    //     foreach (var window in windows)
    //     {
    //         _allWindows.Add(window);
    //         _filteredWindows.Add(window);
    //     }
    // }

    // private async Task RefreshWindowsAsync()
    // {
    //     _settings = _configService.LoadSettings();
        
    //     // Get windows in background thread
    //     var windows = await _windowCache.GetWindowsAsync(_settings.ShowIcons);
        
    //     // Update UI on UI thread!
    //     await Dispatcher.InvokeAsync(() =>
    //     {
    //         _allWindows.Clear();
    //         _filteredWindows.Clear();
            
    //         foreach (var window in windows)
    //         {
    //             _allWindows.Add(window);
    //             _filteredWindows.Add(window);
    //         }
    //     });
    // }

    protected override void OnClosing(CancelEventArgs e)
    {
        _hotkeyService.Dispose();
        // _windowCache.Dispose();
        base.OnClosing(e);
    }
}
