using System;
using System.Threading.Tasks;
using ModerationClient.Services;

namespace ModerationClient.ViewModels;

public partial class LoginViewModel(MatrixAuthenticationService authService) : ViewModelBase
{
    private Exception? _exception;
    public string Username { get; set; }
    public string Password { get; set; }

    public Exception? Exception {
        get => _exception;
        private set => SetProperty(ref _exception, value);
    }

    public async Task LoginAsync() {
        try {
            Exception = null;
            await authService.LoginAsync(Username, Password);
        } catch (Exception e) {
            Exception = e;
        }
    }
}