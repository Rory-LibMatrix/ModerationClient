using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LibMatrix.Services;
using MatrixUtils.Desktop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModerationClient.Services;
using ModerationClient.ViewModels;
using ModerationClient.Views;
using ModerationClient.Views.MainWindow;

namespace ModerationClient;

public partial class App : Application {
    public new static App Current => Application.Current as App ?? throw new InvalidOperationException("Application.Current is null");
    public IServiceProvider Services => Host.Services;

    public IHost Host { get; private set; }

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted() {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
        ConfigureServices(builder.Services);

        Host = builder.Build();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = Host.Services.GetRequiredService<MainWindow>();
            desktop.Exit += (sender, args) => {
                Host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                Host.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
        await Host.StartAsync();
    }

    private static IServiceProvider ConfigureServices(IServiceCollection services) {
        var cfg = CommandLineConfiguration.FromProcessArgs();
        services.AddRoryLibMatrixServices(new() {
            AppName = "ModerationClient",
        });
        services.AddSingleton<CommandLineConfiguration>(cfg);
        services.AddSingleton<MatrixAuthenticationService>();
        services.AddSingleton<ModerationClientConfiguration>();

        services.AddSingleton<TieredStorageService>(s => {
                var cmdLine = s.GetRequiredService<CommandLineConfiguration>();
                return new TieredStorageService(
                    cacheStorageProvider: new FileStorageProvider(Directory.CreateTempSubdirectory($"modcli-{cmdLine.Profile}").FullName),
                    dataStorageProvider: new FileStorageProvider(Directory.CreateTempSubdirectory($"modcli-{cmdLine.Profile}").FullName)
                );
            }
        );

        // Register windows
        services.AddSingleton<MainWindow>();
        services.AddTransient<UserManagementWindow>();
        
        // Register views
        services.AddTransient<LoginView>();
        services.AddTransient<ClientView>();
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ClientViewModel>();
        services.AddTransient<UserManagementViewModel>();

        if (cfg.TestConfiguration is not null) {
            services.AddSingleton(cfg.TestConfiguration);
            services.AddHostedService<TestRunner>();
        }

        return services.BuildServiceProvider();
    }
}