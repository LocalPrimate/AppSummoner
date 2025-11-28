using System.IO;
using System.Windows;
using System.Windows.Input;
using AppSummoner.Data;
using AppSummoner.Data.Models;
using Bio.Win32;

namespace AppSummoner;

public partial class EditHotkeyWindow : Window
{
    private HotkeyConfig? _originalHotkey;
    private VK? _capturedKey;
    private readonly bool _isCreateMode;
    private readonly FileInfo? _fileInfo;

    public HotkeyConfig? UpdatedHotkey { get; private set; }

    // Constructor for editing an existing hotkey
    public EditHotkeyWindow(HotkeyConfig hotkeyConfig)
    {
        InitializeComponent();
        _originalHotkey = hotkeyConfig;
        _isCreateMode = false;
        Title = "Edit Hotkey";
        LoadHotkeyData();
    }

    // Constructor for creating a new hotkey
    public EditHotkeyWindow(FileInfo fileInfo)
    {
        InitializeComponent();
        _fileInfo = fileInfo;
        _isCreateMode = true;
        Title = "Create New Hotkey";
        LoadNewHotkeyData();
    }

    private void LoadHotkeyData()
    {
        if (_originalHotkey == null) return;

        // Load application info
        FriendlyNameTextBox.Text = _originalHotkey.AppInfo.FriendlyName;
        FilePathTextBox.Text = _originalHotkey.AppInfo.FilePath;

        // Load modifiers
        if (_originalHotkey.Hotkey.Modifiers != null)
        {
            LeftControlCheckBox.IsChecked = _originalHotkey.Hotkey.Modifiers.LeftControl;
            RightControlCheckBox.IsChecked = _originalHotkey.Hotkey.Modifiers.RightControl;
            LeftShiftCheckBox.IsChecked = _originalHotkey.Hotkey.Modifiers.LeftShift;
            RightShiftCheckBox.IsChecked = _originalHotkey.Hotkey.Modifiers.RightShift;
            LeftAltCheckBox.IsChecked = _originalHotkey.Hotkey.Modifiers.LeftAlt;
            RightAltCheckBox.IsChecked = _originalHotkey.Hotkey.Modifiers.RightAlt;
        }

        // Load key
        _capturedKey = _originalHotkey.Hotkey.Keycode;
        KeyCaptureTextBox.Text = _capturedKey.ToString();
        KeyCaptureTextBox.Foreground = System.Windows.Media.Brushes.Black;

        // Load behaviors
        LaunchCheckBox.IsChecked = _originalHotkey.Behaviour.Launch;
        FocusCheckBox.IsChecked = _originalHotkey.Behaviour.Focus;
        DismissCheckBox.IsChecked = _originalHotkey.Behaviour.Dismiss;
        DismissTypeComboBox.SelectedIndex = _originalHotkey.Behaviour.DismissType == DismissType.MINIMIZE ? 0 : 1;
    }

    private void LoadNewHotkeyData()
    {
        if (_fileInfo == null) return;

        // Set default values for new hotkey
        FriendlyNameTextBox.Text = Path.GetFileNameWithoutExtension(_fileInfo.Name);
        FilePathTextBox.Text = _fileInfo.FullName;

        // Set default behaviors
        LaunchCheckBox.IsChecked = true;
        FocusCheckBox.IsChecked = true;
        DismissCheckBox.IsChecked = false;
        DismissTypeComboBox.SelectedIndex = 0;
    }

    private void KeyCaptureTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (_capturedKey == null)
        {
            KeyCaptureTextBox.Text = "";
        }
    }

    private void KeyCaptureTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        // Get the actual key (not modifier keys)
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore modifier-only keys
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftAlt || key == Key.RightAlt)
        {
            return;
        }

        // Convert WPF Key to VK
        _capturedKey = (VK)KeyInterop.VirtualKeyFromKey(key);
        KeyCaptureTextBox.Text = _capturedKey.ToString();
        KeyCaptureTextBox.Foreground = System.Windows.Media.Brushes.Black;
    }

    private void DismissCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        DismissTypePanel.IsEnabled = DismissCheckBox.IsChecked == true;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate key is selected
        if (_capturedKey == null)
        {
            MessageBox.Show(
                "You must select a key for the hotkey.\n\nClick the key capture box and press a key.",
                "Key Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Validate friendly name
        if (string.IsNullOrWhiteSpace(FriendlyNameTextBox.Text))
        {
            MessageBox.Show(
                "Please provide a friendly name for the application.",
                "Friendly Name Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Build hotkey config
        var modifiers = new ModifierKeyFlags
        {
            LeftControl = LeftControlCheckBox.IsChecked == true,
            RightControl = RightControlCheckBox.IsChecked == true,
            LeftShift = LeftShiftCheckBox.IsChecked == true,
            RightShift = RightShiftCheckBox.IsChecked == true,
            LeftAlt = LeftAltCheckBox.IsChecked == true,
            RightAlt = RightAltCheckBox.IsChecked == true
        };

        var hotkey = new Hotkey
        {
            Name = _isCreateMode ? null : _originalHotkey?.Hotkey.Name,
            Keycode = _capturedKey.Value,
            Modifiers = modifiers
        };

        // Get process name and file path based on mode
        string processName;
        string filePath;

        if (_isCreateMode && _fileInfo != null)
        {
            processName = Path.GetFileNameWithoutExtension(_fileInfo.Name);
            filePath = _fileInfo.FullName;
        }
        else if (_originalHotkey != null)
        {
            processName = _originalHotkey.AppInfo.ProcessName;
            filePath = _originalHotkey.AppInfo.FilePath;
        }
        else
        {
            MessageBox.Show(
                "Unable to determine application information.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var appInfo = new AppInfo
        {
            FriendlyName = FriendlyNameTextBox.Text.Trim(),
            ProcessName = processName,
            FilePath = filePath
        };

        var behaviour = new Behaviour
        {
            Launch = LaunchCheckBox.IsChecked == true,
            Focus = FocusCheckBox.IsChecked == true,
            Dismiss = DismissCheckBox.IsChecked == true,
            DismissType = DismissTypeComboBox.SelectedIndex == 0 ? DismissType.MINIMIZE : DismissType.HIDE
        };

        UpdatedHotkey = new HotkeyConfig
        {
            Hotkey = hotkey,
            AppInfo = appInfo,
            Behaviour = behaviour
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
