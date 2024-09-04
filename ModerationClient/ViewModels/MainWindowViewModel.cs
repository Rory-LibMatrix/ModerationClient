using System;
using Avalonia;
using ModerationClient.Services;

namespace ModerationClient.ViewModels;

public partial class MainWindowViewModel(MatrixAuthenticationService authService, CommandLineConfiguration cfg) : ViewModelBase {
    // public MainWindow? MainWindow { get; set; }

    private float _scale = 1.0f;
    private ViewModelBase? _currentViewModel = null;
    private Size _physicalSize = new Size(300, 220);

    public ViewModelBase? CurrentViewModel {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public CommandLineConfiguration CommandLineConfiguration { get; } = cfg;
    public MatrixAuthenticationService AuthService { get; } = authService;

    public float Scale {
        get => _scale;
        set {
            if (SetProperty(ref _scale, (float)Math.Round(value, 2))) {
                OnPropertyChanged(nameof(ChildTargetWidth));
                OnPropertyChanged(nameof(ChildTargetHeight));
            }
        }
    }

    public int ChildTargetWidth => (int)(PhysicalSize.Width / Scale);
    public int ChildTargetHeight => (int)(PhysicalSize.Height / Scale);

    public Size PhysicalSize {
        get => _physicalSize;
        set {
            if (SetProperty(ref _physicalSize, value)) {
                OnPropertyChanged(nameof(ChildTargetWidth));
                OnPropertyChanged(nameof(ChildTargetHeight));
            }
        }
    }
}