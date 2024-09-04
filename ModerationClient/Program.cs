using Avalonia;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModerationClient;

internal sealed class Program {
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    // private static FileStream f = new("/dev/input/by-path/platform-pcspkr-event-spkr", FileMode.Open, FileAccess.Write, FileShare.Write, 24);
    public static void Beep(short freq, short duration) {
        // f.Write([..new byte[16], 0x12, 0x00, 0x02, 0x00, (byte)(freq & 0xFF), (byte)((freq >> 8) & 0xFF), 0x00, 0x00]);
        // if (duration > 0) {
            // Thread.Sleep(duration);
            // f.Write([..new byte[16], 0x12, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00]);
        // }
    }
}
