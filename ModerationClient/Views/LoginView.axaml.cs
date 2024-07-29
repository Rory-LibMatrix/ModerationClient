using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using ModerationClient.Services;
using ModerationClient.ViewModels;

namespace ModerationClient.Views;

public partial class LoginView : UserControl {
    private MatrixAuthenticationService AuthService { get; set; }
    
    public LoginView() {
        InitializeComponent();
    }
    
    private void InitializeComponent() {
        Console.WriteLine("LoginWindow loaded");

        AvaloniaXamlLoader.Load(this);
        Console.WriteLine("LoginWindow loaded 2");
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Login(object? sender, RoutedEventArgs e) {
        Console.WriteLine("Login????");
        // await AuthService.LoginAsync(Username, Password);
        await ((LoginViewModel)DataContext).LoginAsync();
    }
}
