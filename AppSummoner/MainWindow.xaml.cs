using System.IO;
using System.Windows;
using System.Windows.Controls;
using AppSummoner.Data;
using AppSummoner.Data.Models;
using AppSummoner.Domain.Interfaces;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace AppSummoner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IFocusStealingSuppressor _focusStealingSuppressor;
    private readonly IConfigManager _configManager;

    public List<HotkeyConfig> Hotkeys { get; private set; }

    public MainWindow(IFocusStealingSuppressor focusStealingSuppressor, IConfigManager configManager)
    {
        InitializeComponent();
        _focusStealingSuppressor = focusStealingSuppressor;
        _configManager = configManager;

        // Load hotkeys
        Hotkeys = _configManager.GetHotkeys();
        DataContext = this;

        // Load and display blocked executables
        LoadBlockedExecutables();
    }

    private void AddRuneButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Title = "Select Application for Hotkey"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var fileInfo = new FileInfo(openFileDialog.FileName);
            var editWindow = new EditHotkeyWindow(fileInfo)
            {
                Owner = this
            };

            if (editWindow.ShowDialog() == true && editWindow.UpdatedHotkey != null)
            {
                _configManager.AddHotkey(editWindow.UpdatedHotkey);
                LoadHotkeys();

                MessageBox.Show(
                    $"Hotkey for '{editWindow.UpdatedHotkey.AppInfo.FriendlyName}' has been created successfully.",
                    "Hotkey Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    private void AddBlockedExecutable_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Title = "Select Executable to Block from Focus Stealing"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var fileInfo = new FileInfo(openFileDialog.FileName);
            var processName = Path.GetFileNameWithoutExtension(fileInfo.Name);

            var blockedExecutable = new BlockedExecutable
            {
                ProcessName = processName,
                FilePath = fileInfo.FullName
            };

            _focusStealingSuppressor.AddBlockedExecutable(blockedExecutable);
            LoadBlockedExecutables();

            MessageBox.Show($"Added {processName} to blocked list.\n\nFocus stealing from this application will now be suppressed.",
                "Blocked Executable Added",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void RemoveBlockedExecutable_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string processName)
        {
            var result = MessageBox.Show(
                $"Remove {processName} from blocked list?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _focusStealingSuppressor.RemoveBlockedExecutable(processName);
                LoadBlockedExecutables();
            }
        }
    }

    private void LoadBlockedExecutables()
    {
        var blockedExecutables = _focusStealingSuppressor.GetBlockedExecutables();
        BlockedExecutablesListBox.ItemsSource = blockedExecutables;
    }

    private void RemoveHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is HotkeyConfig hotkeyConfig)
        {
            var result = MessageBox.Show(
                $"Remove hotkey '{hotkeyConfig.Hotkey.Modifiers} + {hotkeyConfig.Hotkey.Keycode}' for {hotkeyConfig.AppInfo.FriendlyName}?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _configManager.RemoveHotkey(hotkeyConfig);
                LoadHotkeys();

                MessageBox.Show(
                    $"Hotkey for {hotkeyConfig.AppInfo.FriendlyName} has been removed.",
                    "Hotkey Removed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    private void EditHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is HotkeyConfig hotkeyConfig)
        {
            var editWindow = new EditHotkeyWindow(hotkeyConfig)
            {
                Owner = this
            };

            if (editWindow.ShowDialog() == true && editWindow.UpdatedHotkey != null)
            {
                _configManager.UpdateHotkey(hotkeyConfig, editWindow.UpdatedHotkey);
                LoadHotkeys();

                MessageBox.Show(
                    $"Hotkey for '{editWindow.UpdatedHotkey.AppInfo.FriendlyName}' has been updated successfully.",
                    "Hotkey Updated",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    private void LoadHotkeys()
    {
        Hotkeys = _configManager.GetHotkeys();
        HotkeysItemsControl.ItemsSource = null;
        HotkeysItemsControl.ItemsSource = Hotkeys;
    }
}


// TODO
//   1. Prompt user for shortcut creation
//   2. Prompt user for hotkey for shortcut
//   3. Prompt user for target application for shortcut
//   4. Store it all somewhere (AppData?)
//   5. Long term: introduce import and export
//   6. Move window handling login outside of MainWindow.xaml.cs

// TODO
//   1. Considerations:
//     a. When a Rune is invoked, on subsequent invocations validate whether the application fully started up or if it's still on it's way.
//     b. Decide: When a program is already running and bringing it to the foreground, should ALL windows be brought to the foreground, or just the main window? Should it be configurable?

// TODO: Create tests with mocks.
