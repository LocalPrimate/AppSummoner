using System.Windows;
using AppSummoner.Domain;
using AppSummoner.Domain.Handlers;
using AppSummoner.Domain.Interfaces;
using AppSummoner.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppSummoner;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    // Keep strong references to prevent garbage collection of delegates used in unmanaged hooks
    private IHotkeyInputHandler? _hotkeyInputHandler;
    private IFocusStealingSuppressor? _focusStealingSuppressor;

    public App()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigManager, ConfigManager>();
        services.AddSingleton<IHotkeyInputHandler, HotkeyInputHandler>();
        services.AddSingleton<IFocusStealingSuppressor, FocusStealingSuppressor>();
        services.AddSingleton<MainWindow>();
    }

    private void AppStartup(object sender, StartupEventArgs e)
    {
        try
        {
            // Store services in fields to keep strong references and prevent GC of unmanaged delegates
            _hotkeyInputHandler = _serviceProvider.GetService<IHotkeyInputHandler>();
            _focusStealingSuppressor = _serviceProvider.GetService<IFocusStealingSuppressor>();

            _hotkeyInputHandler?.Init();
            _focusStealingSuppressor?.Start();
            _serviceProvider.GetService<MainWindow>()?.Show();
        }
        catch (Exception exception)
        {
            // Todo: Generate crash logs and crash gracefully.

            Console.WriteLine(exception);
            throw;
        }
    }
}
