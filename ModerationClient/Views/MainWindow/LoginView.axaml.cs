using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ModerationClient.ViewModels;

namespace ModerationClient.Views;

public partial class LoginView : UserControl {
    private LoginViewModel? ViewModel => DataContext as LoginViewModel;
    public LoginView() {
        InitializeComponent();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Login(object? _, RoutedEventArgs __) {
        await (DataContext as LoginViewModel ?? throw new InvalidCastException("LoginView did not receive LoginViewModel?")).LoginAsync();
    }
}
