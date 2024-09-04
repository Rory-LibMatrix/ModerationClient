// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using FilesystemBenchmark;

BenchmarkRunner.Run<Benchmarks>();