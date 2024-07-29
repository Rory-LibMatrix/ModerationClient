using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ArcaneLibs;
using Microsoft.Extensions.Logging;

namespace ModerationClient.Services;

public class CommandLineConfiguration {
    public CommandLineConfiguration(ILogger<CommandLineConfiguration> logger) {
        var args = Environment.GetCommandLineArgs();
        logger.LogInformation("Command line arguments: " + string.Join(", ", args));
        for (var i = 0; i < args.Length; i++) {
            logger.LogInformation("Processing argument: " + args[i]);
            switch (args[i]) {
                case "--profile":
                case "-p":
                    if (args.Length <= i + 1 || args[i + 1].StartsWith("-")) {
                        throw new ArgumentException("No profile specified");
                    }

                    Profile = args[++i];
                    logger.LogInformation("Set profile to: " + Profile);
                    break;
                case "--temporary":
                    IsTemporary = true;
                    logger.LogInformation("Using temporary profile");
                    break;
                case "--profile-dir":
                    ProfileDirectory = args[++i];
                    break;
                case "--scale":
                    Scale = float.Parse(args[++i]);
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(ProfileDirectory))
            ProfileDirectory = IsTemporary
                ? Directory.CreateTempSubdirectory("ModerationClient-tmp").FullName
                : Util.ExpandPath($"$HOME/.local/share/ModerationClient/{Profile}");

        logger.LogInformation("Profile directory: " + ProfileDirectory);
        Directory.CreateDirectory(ProfileDirectory);
    }

    public string Profile { get; private set; } = "default";
    public bool IsTemporary { get; private set; }

    public string ProfileDirectory { get; private set; }
    public float Scale { get; private set; } = 1f;
}