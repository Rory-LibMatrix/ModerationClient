using System;
using ArcaneLibs;

namespace ModerationClient.Services;

public class StatusBarService : NotifyPropertyChanged {
    private string _statusText = "Ready";
    private bool _isBusy;

    public string StatusText {
        get => _statusText + " " + DateTime.Now.ToString("u")[..^1];
        set => SetField(ref _statusText, value);
    }

    public bool IsBusy {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }
}