using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModerationClient.Services;
using ModerationClient.ViewModels;

namespace ModerationClient.Views;

public partial class MainWindow : Window {
    public MainWindow(CommandLineConfiguration cfg, MainWindowViewModel dataContext, IHostApplicationLifetime appLifetime) {
        InitializeComponent();
        DataContext = dataContext;
        _ = dataContext.AuthService.LoadProfileAsync();
        Console.WriteLine("mainwnd");
#if DEBUG
        this.AttachDevTools(new DevToolsOptions() {
            ShowAsChildWindow = true,
            LaunchView = DevToolsViewKind.LogicalTree,
        });
#endif
        PropertyChanged += (sender, args) => {
            // Console.WriteLine($"MainWindow PropertyChanged: {args.Property.Name} ({args.OldValue} -> {args.NewValue})");
            switch (args.Property.Name) {
                case nameof(Height):
                case nameof(Width): {
                    if (DataContext is not MainWindowViewModel viewModel) {
                        Console.WriteLine("WARN: MainWindowViewModel is null, ignoring height/width change!");
                        return;
                    }

                    // Console.WriteLine("height/width changed");
                    viewModel.Scale = viewModel.Scale;
                    break;
                }
            }
        };
        dataContext.AuthService.PropertyChanged += (sender, args) => {
            if (args.PropertyName == nameof(MatrixAuthenticationService.IsLoggedIn)) {
                if (dataContext.AuthService.IsLoggedIn) {
                    // dataContext.CurrentViewModel = new ClientViewModel(dataContext.AuthService);
                    dataContext.CurrentViewModel = App.Current.Host.Services.GetRequiredService<ClientViewModel>();
                }
                else {
                    dataContext.CurrentViewModel = new LoginViewModel(dataContext.AuthService);
                }
            }
        };
        dataContext.MainWindow = this;
        dataContext.Scale = cfg.Scale;
        Width *= cfg.Scale;
        Height *= cfg.Scale;

        appLifetime.ApplicationStopping.Register(() => {
            Console.WriteLine("ApplicationStopping triggered");
            Close();
        });
    }

    protected override void OnKeyDown(KeyEventArgs e) => OnKeyDown(this, e);

    private void OnKeyDown(object? _, KeyEventArgs e) {
        if (DataContext is not MainWindowViewModel viewModel) {
            Console.WriteLine($"WARN: DataContext is {DataContext?.GetType().Name ?? "null"}, ignoring key press!");
            return;
        }

        // Console.WriteLine("MainWindow KeyDown: " + e.Key);
        if (e.Key == Key.Escape) {
            viewModel.Scale = 1.0f;
        }
        else if (e.Key == Key.F1) {
            viewModel.Scale -= 0.1f;
            if (viewModel.Scale < 0.1f) {
                viewModel.Scale = 0.1f;
            }
        }
        else if (e.Key == Key.F2) {
            viewModel.Scale += 0.1f;
            if (viewModel.Scale > 5.0f) {
                viewModel.Scale = 5.0f;
            }
        }
        else if (e.Key == Key.K && e.KeyModifiers == KeyModifiers.Control) {
            if(viewModel.CurrentViewModel is ClientViewModel clientViewModel) {
                Console.WriteLine("QuickSwitcher invoked");
            }
            else Console.WriteLine("WARN: CurrentViewModel is not ClientViewModel, ignoring Quick Switcher");
        }
    }
}