using System.Windows;
using AppSummoner.Domain;
using AppSummoner.Domain.Handlers;
using AppSummoner.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AppSummoner;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    
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
        services.AddSingleton<MainWindow>();
    }

    private void AppStartup(object sender, StartupEventArgs e)
    {
        try
        {
            // Todo: Handle these nulls

            _serviceProvider.GetService<IHotkeyInputHandler>().Init();
            _serviceProvider.GetService<MainWindow>().Show();
        }
        catch (Exception exception)
        {
            // Todo: Generate crash logs and crash gracefully.

            Console.WriteLine(exception);
            throw;
        }
    }
}
