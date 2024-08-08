using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
using LibMatrix.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace MatrixUtils.Abstractions;

public class FileStorageProvider : IStorageProvider {
    private readonly ILogger<FileStorageProvider> _logger;

    public string TargetPath { get; }

    /// <summary>
    /// Creates a new instance of <see cref="FileStorageProvider" />.
    /// </summary>
    /// <param name="targetPath"></param>
    public FileStorageProvider(string targetPath) {
        // new Logger<FileStorageProvider>(new LoggerFactory()).LogInformation("test");
        Console.WriteLine($"Initialised FileStorageProvider with path {targetPath}");
        TargetPath = targetPath;
        if (!Directory.Exists(targetPath)) {
            Directory.CreateDirectory(targetPath);
        }
    }

    public async Task SaveObjectAsync<T>(string key, T value) => await File.WriteAllTextAsync(Path.Join(TargetPath, key), value?.ToJson());

    [RequiresUnreferencedCode("This API uses reflection to deserialize JSON")]
    public async Task<T?> LoadObjectAsync<T>(string key) => JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Join(TargetPath, key)));

    public Task<bool> ObjectExistsAsync(string key) => Task.FromResult(File.Exists(Path.Join(TargetPath, key)));

    public Task<List<string>> GetAllKeysAsync() => Task.FromResult(Directory.GetFiles(TargetPath).Select(Path.GetFileName).ToList());

    public Task DeleteObjectAsync(string key) {
        File.Delete(Path.Join(TargetPath, key));
        return Task.CompletedTask;
    }

    public async Task SaveStreamAsync(string key, Stream stream) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Join(TargetPath, key)) ?? throw new InvalidOperationException());
        await using var fileStream = File.Create(Path.Join(TargetPath, key));
        await stream.CopyToAsync(fileStream);
    }

    public Task<Stream?> LoadStreamAsync(string key) => Task.FromResult<Stream?>(File.Exists(Path.Join(TargetPath, key)) ? File.OpenRead(Path.Join(TargetPath, key)) : null);
}
