using System;
using ModerationClient.Services;
using ModerationClient.Views;

namespace ModerationClient.ViewModels;

public partial class MainWindowViewModel(MatrixAuthenticationService authService, CommandLineConfiguration cfg) : ViewModelBase {
    public MainWindow? MainWindow { get; set; }
    
    private float _scale = 1.0f;
    private ViewModelBase _currentViewModel = new LoginViewModel(authService);

    public ViewModelBase CurrentViewModel {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public CommandLineConfiguration CommandLineConfiguration { get; } = cfg;
    public MatrixAuthenticationService AuthService { get; } = authService;

    public float Scale {
        get => _scale;
        set {
            SetProperty(ref _scale, (float)Math.Round(value, 2));
            OnPropertyChanged(nameof(ChildTargetWidth));
            OnPropertyChanged(nameof(ChildTargetHeight));
        }
    }
    public int ChildTargetWidth => (int)(MainWindow?.Width / Scale ?? 1);
    public int ChildTargetHeight => (int)(MainWindow?.Height / Scale ?? 1);

    
}