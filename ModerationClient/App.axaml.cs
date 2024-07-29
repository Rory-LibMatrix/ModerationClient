using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LibMatrix.Services;
using MatrixUtils.Abstractions;
using MatrixUtils.Desktop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModerationClient.Services;
using ModerationClient.ViewModels;
using ModerationClient.Views;

namespace ModerationClient;

public partial class App : Application {
    /// <summary>
    /// Gets the current <see cref="App"/> instance in use
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services => Host.Services;

    public IHost Host { get; private set; }

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted() {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
        builder.Services.AddTransient<MainWindowViewModel>();
        ConfigureServices(builder.Services);
        // builder.Services.AddHostedService<HostedBackgroundService>();

        Host = builder.Build();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            // desktop.MainWindow = new MainWindow {
                // DataContext = Host.Services.GetRequiredService<MainWindowViewModel>()
            // };
            desktop.MainWindow = Host.Services.GetRequiredService<MainWindow>();
            desktop.Exit += (sender, args) => {
                Host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                Host.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
        await Host.StartAsync();
    }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IServiceProvider ConfigureServices(IServiceCollection services) {
        services.AddRoryLibMatrixServices(new() {
            AppName = "ModerationClient",
        });
        services.AddSingleton<CommandLineConfiguration>();
        services.AddSingleton<MatrixAuthenticationService>();
        services.AddSingleton<ModerationClientConfiguration>();

        services.AddSingleton<TieredStorageService>(x => {
                var cmdLine = x.GetRequiredService<CommandLineConfiguration>();
                return new TieredStorageService(
                    cacheStorageProvider: new FileStorageProvider(Directory.CreateTempSubdirectory($"modcli-{cmdLine.Profile}").FullName),
                    dataStorageProvider: new FileStorageProvider(Directory.CreateTempSubdirectory($"modcli-{cmdLine.Profile}").FullName)
                );
            }
        );

        // Register views
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginView>();
        services.AddTransient<ClientView>();
        // Register ViewModels
        services.AddTransient<ClientViewModel>();

        return services.BuildServiceProvider();
    }
}