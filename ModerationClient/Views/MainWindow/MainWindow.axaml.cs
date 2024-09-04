using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcaneLibs.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using LibMatrix.Responses;
using LibMatrix.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModerationClient.Services;
using ModerationClient.ViewModels;

#if DEBUG
using Avalonia.Diagnostics;
#endif

namespace ModerationClient.Views.MainWindow;

public partial class MainWindow : Window {
    private readonly CommandLineConfiguration _cfg;

    public MainWindow(CommandLineConfiguration cfg, MainWindowViewModel dataContext, IHostApplicationLifetime appLifetime) {
        _cfg = cfg;
        InitializeComponent();
        DataContext = dataContext;
        _ = dataContext.AuthService.LoadProfileAsync().ContinueWith(x => {
            if (x.IsFaulted) {
                Console.WriteLine("Failed to load profile: " + x.Exception);
            }

            dataContext.CurrentViewModel = dataContext.AuthService.IsLoggedIn
                ? App.Current.Host.Services.GetRequiredService<ClientViewModel>()
                : new LoginViewModel(dataContext.AuthService);

            if (!dataContext.AuthService.IsLoggedIn) {
                dataContext.AuthService.PropertyChanged += (sender, args) => {
                    if (args.PropertyName == nameof(MatrixAuthenticationService.IsLoggedIn)) {
                        if (dataContext.AuthService.IsLoggedIn) {
                            // dataContext.CurrentViewModel = new ClientViewModel(dataContext.AuthService);
                            dataContext.CurrentViewModel = App.Current.Host.Services.GetRequiredService<ClientViewModel>();
                            // var window = App.Current.Host.Services.GetRequiredService<UserManagementWindow>();
                            // window.Show();
                        }
                        else {
                            dataContext.CurrentViewModel = new LoginViewModel(dataContext.AuthService);
                        }
                    }
                };
            }
        });
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

                    // viewModel.PhysicalSize = new Size(ClientSize.Width, ClientSize.Height - TopPanel.Bounds.Height);
                    viewModel.PhysicalSize = new Size(ClientSize.Width, ClientSize.Height);
                    break;
                }
            }
        };

        // TopPanel.PropertyChanged += (_, args) => {
        //     if (args.Property.Name == nameof(Visual.Bounds)) {
        //         if (DataContext is not MainWindowViewModel viewModel) {
        //             Console.WriteLine("WARN: MainWindowViewModel is null, ignoring TopPanel.Bounds change!");
        //             return;
        //         }
        //
        //         viewModel.PhysicalSize = new Size(ClientSize.Width, ClientSize.Height - TopPanel.Bounds.Height);
        //     }
        // };

        dataContext.Scale = cfg.Scale;
        Width *= cfg.Scale;
        Height *= cfg.Scale;

        appLifetime.ApplicationStopping.Register(() => {
            Console.WriteLine("ApplicationStopping triggered");
            Close();
        });
    }

    protected override void OnKeyDown(KeyEventArgs e) => OnKeyDown(this, e).ContinueWith(t => {
        if (t.IsFaulted) {
            Console.WriteLine("OnKeyDown faulted: " + t.Exception);
        }
    });

    private async Task OnKeyDown(object? _, KeyEventArgs e) {
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
        else if (e.Key == Key.Pause) {
            if (viewModel.CurrentViewModel is ClientViewModel clientViewModel) {
                clientViewModel.Paused = !clientViewModel.Paused;
            }
        }
        else if (e.KeyModifiers == KeyModifiers.Control) {
            if (e.Key == Key.K) {
                if (viewModel.CurrentViewModel is ClientViewModel clientViewModel) {
                    Console.WriteLine("QuickSwitcher invoked");
                }
                else Console.WriteLine("WARN: CurrentViewModel is not ClientViewModel, ignoring Quick Switcher");
            }
            else if (e.Key == Key.U) {
                Console.WriteLine("UserManagementWindow invoked");
                var window = App.Current.Host.Services.GetRequiredService<UserManagementWindow>();
                window.Show();
            }
            else if (e.Key == Key.F5) {
                Console.WriteLine("Launching new process");
                System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName, Environment.GetCommandLineArgs());
            }
            else if (e.Key == Key.F9) { }
            else if (e.Key == Key.D) {
                List<LoginResponse> mxids = new();
                var hsps = App.Current.Services.GetRequiredService<HomeserverProviderService>();
                var rhs = await hsps.GetRemoteHomeserver("matrixunittests.rory.gay", enableServer: false);
                for (int i = 0; i < 64; i++) {
                    Console.WriteLine("Debugging invoked");
                    var main = await rhs.RegisterAsync(Guid.NewGuid().ToString(), "password");
                    mxids.Add(main);
                    Console.WriteLine($"Registered: {main.UserId} {main.AccessToken} - {mxids.Count}");
                }

                foreach (var mxid in mxids) {
                    Console.WriteLine("Launching new process: ");
                    var args = (_cfg with {
                        Profile = "mut-" + mxid.UserId,
                        ProfileDirectory = null,
                        LoginData = mxid.ToJson(),
                        TestConfiguration = new() {
                            Mxids = mxids.Select(x => x.UserId).ToList()
                        }
                    }).Serialise();
                    Console.WriteLine(string.Join(' ', args));
                    System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName, args);
                }
            }
        }
    }
}