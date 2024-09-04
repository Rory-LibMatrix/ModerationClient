using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ArcaneLibs.Extensions;
using LibMatrix.Interfaces.Services;

namespace ModerationClient.Services;

public class FileStorageProvider : IStorageProvider {
    // private readonly ILogger<FileStorageProvider> _logger;
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true
    };
    
    private static readonly EnumerationOptions EnumOpts = new EnumerationOptions() {
        MatchType = MatchType.Simple,
        AttributesToSkip = FileAttributes.None,
        IgnoreInaccessible = false,
        RecurseSubdirectories = true
    };

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

    public async Task SaveObjectAsync<T>(string key, T value) {
        EnsureContainingDirectoryExists(GetFullPath(key));
        await using var fileStream = File.Create(GetFullPath(key));
        await JsonSerializer.SerializeAsync(fileStream, value, Options);
    }

    [RequiresUnreferencedCode("This API uses reflection to deserialize JSON")]
    public async Task<T?> LoadObjectAsync<T>(string key) {
        await using var fileStream = File.OpenRead(GetFullPath(key));
        return JsonSerializer.Deserialize<T>(fileStream);
    }

    public Task<bool> ObjectExistsAsync(string key) => Task.FromResult(File.Exists(GetFullPath(key)));

    public async Task<IEnumerable<string>> GetAllKeysAsync() {
        var sw = Stopwatch.StartNew();
        // var result = Directory.EnumerateFiles(TargetPath, "*", SearchOption.AllDirectories)
        var result = Directory.EnumerateFiles(TargetPath, "*", EnumOpts)
            .Select(s => s.Replace(TargetPath, "").TrimStart('/'));
        // Console.WriteLine($"GetAllKeysAsync got {result.Count()} results in {sw.ElapsedMilliseconds}ms");
        // Environment.Exit(0);
        return result;
    }

    public Task DeleteObjectAsync(string key) {
        File.Delete(GetFullPath(key));
        return Task.CompletedTask;
    }

    public async Task SaveStreamAsync(string key, Stream stream) {
        EnsureContainingDirectoryExists(GetFullPath(key));
        await using var fileStream = File.Create(GetFullPath(key));
        await stream.CopyToAsync(fileStream);
    }

    public Task<Stream?> LoadStreamAsync(string key) => Task.FromResult<Stream?>(File.Exists(GetFullPath(key)) ? File.OpenRead(GetFullPath(key)) : null);

    public Task CopyObjectAsync(string sourceKey, string destKey) {
        EnsureContainingDirectoryExists(GetFullPath(destKey));
        File.Copy(GetFullPath(sourceKey), GetFullPath(destKey));
        return Task.CompletedTask;
    }

    public Task MoveObjectAsync(string sourceKey, string destKey) {
        EnsureContainingDirectoryExists(GetFullPath(destKey));
        File.Move(GetFullPath(sourceKey), GetFullPath(destKey));
        return Task.CompletedTask;
    }

    private string GetFullPath(string key) => Path.Join(TargetPath, key);

    private void EnsureContainingDirectoryExists(string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());
    }
}