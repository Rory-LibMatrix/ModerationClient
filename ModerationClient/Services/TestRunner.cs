using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ModerationClient.Services;

public class TestRunner(CommandLineConfiguration.TestConfig testConfig, MatrixAuthenticationService mas) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        Console.WriteLine("TestRunner: Starting test runner");
        mas.PropertyChanged += (_, args) => {
            if (args.PropertyName == nameof(MatrixAuthenticationService.IsLoggedIn) && mas.IsLoggedIn) {
                Console.WriteLine("TestRunner: Logged in, starting test");
                _ = Run();
            }
        };
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        Console.WriteLine("TestRunner: Stopping test runner");
    }

    private async Task Run() {
        var hs = mas.Homeserver!;
        Console.WriteLine("TestRunner: Running test on homeserver " + hs);
        foreach (var mxid in testConfig.Mxids) {
            var room = await hs.CreateRoom(new() {
                Name = mxid,
                Invite = testConfig.Mxids
            });
            
            await room.SendMessageEventAsync(new("test"));

        }
    }
}