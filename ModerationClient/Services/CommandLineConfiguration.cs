using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using ArcaneLibs;
using ArcaneLibs.Extensions;

namespace ModerationClient.Services;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public record CommandLineConfiguration {
    public static CommandLineConfiguration FromProcessArgs() {
        // logger.LogInformation("Command line arguments: " + string.Join(", ", Environment.GetCommandLineArgs()));
        CommandLineConfiguration cfg = FromSerialised(Environment.GetCommandLineArgs());

        if (string.IsNullOrWhiteSpace(cfg.ProfileDirectory))
            cfg = cfg with {
                ProfileDirectory = cfg.IsTemporary
                    ? Directory.CreateTempSubdirectory("ModerationClient-tmp").FullName
                    : Util.ExpandPath($"$HOME/.local/share/ModerationClient/{cfg.Profile}")
            };

        // logger.LogInformation("Profile directory: " + cfg.ProfileDirectory);
        Directory.CreateDirectory(cfg.ProfileDirectory);
        if (!string.IsNullOrWhiteSpace(cfg.LoginData)) {
            File.WriteAllText(Path.Combine(cfg.ProfileDirectory, "login.json"), cfg.LoginData);
        }
        
        
        return cfg;
    }
    public string[] Serialise() {
        var current = FromProcessArgs();
        List<string> args = new();
        if (Profile != current.Profile) args.AddRange(["--profile", Profile]);
        if (IsTemporary) args.Add("--temporary");
        if (Math.Abs(Scale - 1f) > float.Epsilon) args.AddRange(["--scale", Scale.ToString()]);
        if (ProfileDirectory != current.ProfileDirectory) args.AddRange(["--profile-dir", ProfileDirectory]);
        if (!string.IsNullOrWhiteSpace(_loginData) && _loginData != current.LoginData) args.AddRange(["--login-data", _loginData!]);
        if (TestConfiguration is not null && TestConfiguration != current.TestConfiguration) args.AddRange(["--test-config", TestConfiguration!.ToJson()]);
        return args.ToArray();
    }
    public static CommandLineConfiguration FromSerialised(string[] args) {
        CommandLineConfiguration cfg = new();
        for (var i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--profile":
                case "-p":
                    cfg = cfg with { Profile = args[++i] };
                    break;
                case "--temporary":
                    cfg = cfg with { IsTemporary = true };
                    break;
                case "--profile-dir":
                    cfg = cfg with { ProfileDirectory = args[++i] };
                    break;
                case "--scale":
                    cfg = cfg with { Scale = float.Parse(args[++i]) };
                    break;
                case "--login-data":
                    cfg = cfg with { LoginData = args[++i] };
                    break;
                case "--test-config":
                    cfg = cfg with { testConfiguration = args[++i] };
                    break;
            }
        }

        return cfg;
    }

    private readonly string? _loginData;
    public string Profile { get; init; } = "default";
    public bool IsTemporary { get; init; }

    public string ProfileDirectory { get; init; }
    public float Scale { get; init; } = 1f;

    public string? LoginData {
        get => _loginData;
        init {
            Console.WriteLine("Setting login data: " + value);
            _loginData = value;
        }
    }

    private string? testConfiguration {
        get => TestConfiguration?.ToJson();
        init => TestConfiguration = value is null ? null : JsonSerializer.Deserialize<TestConfig>(value);
    }

    public TestConfig? TestConfiguration { get; init; }

    public class TestConfig {
        public List<string> Mxids { get; set; } = new();
    }
}