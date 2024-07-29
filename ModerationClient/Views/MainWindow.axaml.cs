using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModerationClient.Services;
using ModerationClient.ViewModels;

namespace ModerationClient.Views;

public partial class MainWindow : Window {
    //viewmodel
    private MainWindowViewModel? _viewModel { get; set; }

    public MainWindow(CommandLineConfiguration cfg, MainWindowViewModel dataContext, IHostApplicationLifetime appLifetime) {
        InitializeComponent();
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
                    if (_viewModel is null) {
                        Console.WriteLine("WARN: MainWindowViewModel is null, ignoring height/width change!");
                        return;
                    }

                    // Console.WriteLine("height/width changed");
                    _viewModel.Scale = _viewModel.Scale;
                    break;
                }
            }
        };
        DataContext = _viewModel = dataContext;
        _ = dataContext.AuthService.LoadProfileAsync();
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
        if (_viewModel is null) {
            Console.WriteLine("WARN: MainWindowViewModel is null, ignoring key press!");
            return;
        }

        // Console.WriteLine("MainWindow KeyDown: " + e.Key);
        if (e.Key == Key.Escape) {
            _viewModel.Scale = 1.0f;
        }
        else if (e.Key == Key.F1) {
            _viewModel.Scale -= 0.1f;
            if (_viewModel.Scale < 0.1f) {
                _viewModel.Scale = 0.1f;
            }
        }
        else if (e.Key == Key.F2) {
            _viewModel.Scale += 0.1f;
            if (_viewModel.Scale > 5.0f) {
                _viewModel.Scale = 5.0f;
            }
        }
    }
}