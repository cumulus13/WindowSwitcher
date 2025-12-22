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
            
            // Apply theme
            ApplyTheme(_settings.DarkTheme);
            
            // Register global hotkey
            _hotkeyService.RegisterHotkey(
                _settings.HotkeyModifier,
                _settings.HotkeyKey,
                () => Dispatcher.Invoke(ToggleWindow)
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

    // private void ApplyTheme(bool isDark)
    // {
    //     if (isDark)
    //     {
    //         // Dark theme colors
    //         var border = (System.Windows.Controls.Border)((System.Windows.Controls.Grid)Content).Parent;
    //         border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
    //         border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
            
    //         var searchBorder = FindName("SearchBox") as System.Windows.Controls.TextBox;
    //         if (searchBorder?.Parent is System.Windows.Controls.Border sb)
    //         {
    //             sb.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40));
    //             sb.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
    //             searchBorder.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
    //         }
            
    //         ResultsList.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
    //         ResultsList.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
    //     }
    // }

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