using System.Collections.Concurrent;
using System.Security.Cryptography;
using ArcaneLibs.Extensions;
using BenchmarkDotNet.Attributes;

namespace FilesystemBenchmark;

public class Benchmarks {
    private string testPath = "/home/Rory/.local/share/ModerationClient/default/syncCache";

    private EnumerationOptions enumOpts = new EnumerationOptions() {
        MatchType = MatchType.Simple,
        AttributesToSkip = FileAttributes.None,
        IgnoreInaccessible = false,
        RecurseSubdirectories = true
    };

    [Benchmark]
    public void GetFilesMatching() {
        _ = Directory.GetFiles(testPath, "*.*", SearchOption.AllDirectories).Count();
    }
    
    [Benchmark]
    public void EnumerateFilesMatching() {
        _ = Directory.EnumerateFiles(testPath, "*.*", SearchOption.AllDirectories).Count();
    }
    
    [Benchmark]
    public void GetFilesMatchingSingleStar() {
        _ = Directory.GetFiles(testPath, "*", SearchOption.AllDirectories).Count();
    }
    
    [Benchmark]
    public void EnumerateFilesMatchingSingleStar() {
        _ = Directory.EnumerateFiles(testPath, "*", SearchOption.AllDirectories).Count();
    }
    
    [Benchmark]
    public void GetFilesMatchingSingleStarSimple() {
        _ = Directory.GetFiles(testPath, "*", new EnumerationOptions() {
            MatchType = MatchType.Simple,
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
            RecurseSubdirectories = true
        }).Count();
    }
    
    [Benchmark]
    public void EnumerateFilesMatchingSingleStarSimple() {
        _ = Directory.EnumerateFiles(testPath, "*", new EnumerationOptions() {
            MatchType = MatchType.Simple,
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
            RecurseSubdirectories = true
        }).Count();
    }
    
    [Benchmark]
    public void GetFilesMatchingSingleStarSimpleCached() {
        _ = Directory.GetFiles(testPath, "*", enumOpts).Count();
    }
    
    [Benchmark]
    public void EnumerateFilesMatchingSingleStarSimpleCached() {
        _ = Directory.EnumerateFiles(testPath, "*", enumOpts).Count();
    }
    
    // [Benchmark]
    // public void GetFilesRecursiveFunc() {
    //     GetFilesRecursive(testPath);
    // }
    //
    // [Benchmark]
    // public void GetFilesRecursiveParallelFunc() {
    //     GetFilesRecursiveParallel(testPath);
    // }
    //
    // [Benchmark]
    // public void GetFilesRecursiveEntriesFunc() {
    //     GetFilesRecursiveEntries(testPath);
    // }
    //
    // [Benchmark]
    // public void GetFilesRecursiveAsyncFunc() {
    //     GetFilesRecursiveAsync(testPath).ToBlockingEnumerable();
    // }
    

    private List<string> GetFilesRecursive(string path) {
        var result = new List<string>();
        foreach (var dir in Directory.GetDirectories(path)) {
            result.AddRange(GetFilesRecursive(dir));
        }

        result.AddRange(Directory.GetFiles(path));
        return result;
    }

    private List<string> GetFilesRecursiveEntries(string path) {
        var result = new List<string>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(path)) {
            if (Directory.Exists(entry)) {
                result.AddRange(GetFilesRecursiveEntries(entry));
            }
            else {
                result.Add(entry);
            }
        }

        return result;
    }

    private List<string> GetFilesRecursiveParallel(string path) {
        var result = new ConcurrentBag<string>();
        Directory.GetDirectories(path).AsParallel().ForAll(dir => {
            GetFilesRecursiveParallel(dir).ForEach(result.Add);
        });

        Directory.GetFiles(path).AsParallel().ForAll(result.Add);
        return result.ToList();
    }
    
    private async IAsyncEnumerable<string> GetFilesRecursiveAsync(string path) {
        foreach (var dir in Directory.GetDirectories(path)) {
            foreach (var file in GetFilesRecursiveAsync(dir).ToBlockingEnumerable()) {
                yield return file;
            }
        }

        foreach (var file in Directory.GetFiles(path)) {
            yield return file;
        }
    }
}