using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ArcaneLibs;
using ArcaneLibs.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatrixUtils.Desktop;

public class ModerationClientConfiguration
{
    private ILogger<ModerationClientConfiguration> _logger;

    [RequiresUnreferencedCode("Uses reflection binding")]
    public ModerationClientConfiguration(ILogger<ModerationClientConfiguration> logger, IConfiguration config, HostBuilderContext host)
    {
        _logger = logger;
        logger.LogInformation("Loading configuration for environment: {}...", host.HostingEnvironment.EnvironmentName);
        config.GetSection("ModerationClient").Bind(this);
        DataStoragePath = Util.ExpandPath(DataStoragePath);
        CacheStoragePath = Util.ExpandPath(CacheStoragePath);
    }

    public string? DataStoragePath { get; set; } = "";
    public string? CacheStoragePath { get; set; } = "";
    public string? SentryDsn { get; set; }
}