using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArcaneLibs;
using ArcaneLibs.Extensions;
using Avalonia.Controls.Diagnostics;
using LibMatrix;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.Services;
using MatrixUtils.Desktop;
using Microsoft.Extensions.Logging;

namespace ModerationClient.Services;

public class MatrixAuthenticationService(ILogger<MatrixAuthenticationService> logger, HomeserverProviderService hsProvider, CommandLineConfiguration cfg) : NotifyPropertyChanged {
    private bool _isLoggedIn = false;
    public string Profile => cfg.Profile;
    public AuthenticatedHomeserverGeneric? Homeserver { get; private set; }

    public bool IsLoggedIn {
        get => _isLoggedIn;
        private set => SetField(ref _isLoggedIn, value);
    }

    public async Task LoadProfileAsync() {
        if (!File.Exists(Util.ExpandPath($"{cfg.ProfileDirectory}/login.json")!)) return;
        var loginJson = await File.ReadAllTextAsync(Util.ExpandPath($"{cfg.ProfileDirectory}/login.json")!);
        var login = JsonSerializer.Deserialize<LoginResponse>(loginJson);
        if (login is null) return;
        try {
            Homeserver = await hsProvider.GetAuthenticatedWithToken(login.Homeserver, login.AccessToken);
            IsLoggedIn = true;
        }
        catch (MatrixException e) {
            if (e is not { Error: MatrixException.ErrorCodes.M_UNKNOWN_TOKEN }) throw;
        }
    }

    public async Task LoginAsync(string username, string password) {
        Directory.CreateDirectory(Util.ExpandPath($"{cfg.ProfileDirectory}")!);
        var mxidParts = username.Split(':', 2);
        var res = await hsProvider.Login(mxidParts[1], username, password);
        await File.WriteAllTextAsync(Path.Combine(cfg.ProfileDirectory, "login.json"), res.ToJson());
        IsLoggedIn = true;

        // Console.WriteLine("Login result: " + res.ToJson());
    }

    public async Task LogoutAsync() {
        Directory.Delete(Util.ExpandPath($"{cfg.ProfileDirectory}")!, true);
        IsLoggedIn = false;
    }
}