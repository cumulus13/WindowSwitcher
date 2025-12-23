// File: MainWindow.xaml.cs
// Author: Hadi Cahyadi <cumulus13@gmail.com>
// Date: 2025-12-23
// Description: 
// License: MIT

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WindowSwitcher.Models;
using WindowSwitcher.Services;
using System.Windows.Controls;

namespace WindowSwitcher;

public partial class MainWindow : Window
{
    private readonly WindowManagerService _windowManager;
    private readonly ConfigService _configService;
    private readonly HotkeyService _hotkeyService;
    private ObservableCollection<WindowInfo> _allWindows;
    private ObservableCollection<WindowInfo> _filteredWindows;
    private AppSettings _settings = null!;
    
    // NEW: Track display mode
    private bool _followCursorMode = true;

    public MainWindow()
    {
        InitializeComponent();
        
        _windowManager = new WindowManagerService();
        _configService = new ConfigService();
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
    {
        // Refresh window list
        RefreshWindows();
        
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

    private void RefreshWindows()
    {
        _settings = _configService.LoadSettings();
        var windows = _windowManager.GetAllWindows(_settings.ShowIcons);
        
        _allWindows.Clear();
        _filteredWindows.Clear();
        
        var count = 0;
        foreach (var window in windows)
        {
            if (count >= _settings.MaxListShow)
                break;
                
            _allWindows.Add(window);
            _filteredWindows.Add(window);
            count++;
        }
    }

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

    private void FilterWindows(string searchText)
    {
        _filteredWindows.Clear();
        
        var query = searchText.ToLowerInvariant();
        var count = 0;
        
        foreach (var window in _allWindows)
        {
            if (count >= _settings.MaxListShow)
                break;
                
            if (string.IsNullOrWhiteSpace(query) ||
                window.Title.ToLowerInvariant().Contains(query) ||
                window.ProcessName.ToLowerInvariant().Contains(query))
            {
                _filteredWindows.Add(window);
                count++;
            }
        }
        
        // Auto-select first item
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

    protected override void OnClosing(CancelEventArgs e)
    {
        _hotkeyService.Dispose();
        base.OnClosing(e);
    }
}
