using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.DependencyInjection;
using ModerationClient.ViewModels;

namespace ModerationClient;

public class ViewLocator : IDataTemplate {
    public Control? Build(object? data) {
        try {
            if (data is null)
                return null;

            var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            Console.WriteLine($"ViewLocator: Locating {name} for {data.GetType().FullName}");
            var type = Type.GetType(name);
            Console.WriteLine($"ViewLocator: Got {type?.FullName ?? "null"}");

            if (type != null) {
                var control = (Control)App.Current.Services.GetRequiredService(type);
                Console.WriteLine($"ViewLocator: Created {control.GetType().FullName}");
                control.DataContext = data;
                return control;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }
        catch (Exception e) {
            Console.WriteLine($"ViewLocator: Error: {e}");
            return new TextBlock { Text = e.ToString(), Foreground = Avalonia.Media.Brushes.Red };
        }
    }

    public bool Match(object? data) {
        return data is ViewModelBase;
    }
}