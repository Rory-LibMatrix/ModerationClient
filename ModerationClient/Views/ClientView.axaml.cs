using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModerationClient.Views;

public partial class ClientView : UserControl {
    
    public ClientView() {
        InitializeComponent();
        
        // PropertyChanged += (_, e) => {
        //     switch (e.Property.Name) {
        //         case nameof(Width): {
        //             //make sure all columns fit
        //             var grid = this.LogicalChildren.OfType<Grid>().FirstOrDefault();
        //             if(grid is null) {
        //                 Console.WriteLine("Failed to find Grid in ClientView");
        //                 return;
        //             }
        //             Console.WriteLine($"ClientView width changed to {Width}");
        //             var columns = grid.ColumnDefinitions;
        //             break;
        //         }
        //     }
        // };
    }
    
    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}
