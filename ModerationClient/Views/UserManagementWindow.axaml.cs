using System;
using ArcaneLibs.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using LibMatrix.Homeservers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModerationClient.Services;
using ModerationClient.ViewModels;

namespace ModerationClient.Views;

public partial class UserManagementWindow : Window {
    private readonly CommandLineConfiguration _cfg;
    private readonly MatrixAuthenticationService _auth;

    public UserManagementWindow(CommandLineConfiguration cfg, MainWindowViewModel dataContext, IHostApplicationLifetime appLifetime,
        UserManagementViewModel userManagementViewModel, MatrixAuthenticationService auth) {
        _cfg = cfg;
        _auth = auth;
        InitializeComponent();
        DataContext = dataContext;
        dataContext.CurrentViewModel = userManagementViewModel;
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
                case nameof(ClientSize): {
                    if (DataContext is not MainWindowViewModel viewModel) {
                        Console.WriteLine("WARN: MainWindowViewModel is null, ignoring ClientSize change!");
                        return;
                    }

                    viewModel.PhysicalSize = new Size(ClientSize.Width, ClientSize.Height - TopPanel.Bounds.Height);
                    break;
                }
            }
        };

        TopPanel.PropertyChanged += (_, args) => {
            if (args.Property.Name == nameof(TopPanel.Bounds)) {
                if (DataContext is not MainWindowViewModel viewModel) {
                    Console.WriteLine("WARN: MainWindowViewModel is null, ignoring TopPanel.Bounds change!");
                    return;
                }

                viewModel.PhysicalSize = new Size(ClientSize.Width, ClientSize.Height - TopPanel.Bounds.Height);
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
            if (viewModel.CurrentViewModel is ClientViewModel clientViewModel) {
                Console.WriteLine("QuickSwitcher invoked");
            }
            else Console.WriteLine("WARN: CurrentViewModel is not ClientViewModel, ignoring Quick Switcher");
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void PuppetButtonClicked(object? sender, RoutedEventArgs e) {
        if (e.Source is not Button button) {
            Console.WriteLine("WARN: Source is not Button, ignoring PuppetButtonClicked!");
            return;
        }

        if (button.Tag is not User user) {
            Console.WriteLine("WARN: Tag is not User, ignoring PuppetButtonClicked!");
            return;
        }

        if (_auth.Homeserver is not AuthenticatedHomeserverSynapse synapse) {
            Console.WriteLine("WARN: Homeserver is not Synapse, ignoring PuppetButtonClicked!");
            return;
        }

        var puppet = await synapse.Admin.LoginUserAsync(user.Name, TimeSpan.FromMinutes(5));

        System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName,
            (_cfg with { IsTemporary = true, LoginData = puppet.ToJson() }).Serialise());
    }
}