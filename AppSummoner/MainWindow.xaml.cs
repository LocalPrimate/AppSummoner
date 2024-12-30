using System.IO;
using System.Windows;
using AppSummoner.Domain.Handlers;
using Bio;
using Bio.Win32;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace AppSummoner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // FOR TESTING
    private FileInfo? _tempFileInfo = null;

    public MainWindow() 
    {
        InitializeComponent();
        
        // Just initializing it
        // Console.WriteLine(HotkeyInputHandler.Instance.ToString());
    }

    private void AddRuneButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _tempFileInfo = new FileInfo(openFileDialog.FileName);
            TextBlock_RuneAppName.Text = $"""
                                          Name: {Path.GetFileNameWithoutExtension(_tempFileInfo.Name)}
                                          Path: {_tempFileInfo.FullName}
                                          """;
        }
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
