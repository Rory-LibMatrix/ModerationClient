using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArcaneLibs.Collections;
using LibMatrix.Helpers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;
using ModerationClient.Services;

namespace ModerationClient.ViewModels;

public partial class ClientViewModel : ViewModelBase
{
    public ClientViewModel(ILogger<ClientViewModel> logger, MatrixAuthenticationService authService) {
        this.logger = logger;
        this.authService = authService;
        _ = Task.Run(Run);
    }
    
    private readonly ILogger<ClientViewModel> logger;
    private readonly MatrixAuthenticationService authService;
    
    private Exception? _exception;

    public Exception? Exception {
        get => _exception;
        private set => SetProperty(ref _exception, value);
    }

    public ObservableCollection<SpaceNode> DisplayedSpaces { get; } = [
        new SpaceNode { Name = "Root", Children = [
            new SpaceNode { Name = "Child 1" },
            new SpaceNode { Name = "Child 2" },
            new SpaceNode { Name = "Child 3" }
        ] },
        new SpaceNode { Name = "Root 2", Children = [
            new SpaceNode { Name = "Child 4" },
            new SpaceNode { Name = "Child 5" },
            new SpaceNode { Name = "Child 6" }
        ] }
    ];

    public async Task Run() {
        var sh = new SyncStateResolver(authService.Homeserver, logger);
        // var res = await sh.SyncAsync();
        while (true) {
            var res = await sh.ContinueAsync();
            Console.WriteLine("mow");
        }
    }

    private void ApplySpaceChanges(SyncResponse newSync) {
        List<string> handledRoomIds = [];
       
    }
}

public class SpaceNode {
    public string Name { get; set; }
    public ObservableCollection<SpaceNode> Children { get; set;  } = [];
}