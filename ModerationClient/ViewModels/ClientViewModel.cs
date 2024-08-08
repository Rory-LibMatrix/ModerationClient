using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcaneLibs.Collections;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Helpers;
using LibMatrix.Responses;
using MatrixUtils.Abstractions;
using Microsoft.Extensions.Logging;
using ModerationClient.Services;

namespace ModerationClient.ViewModels;

public partial class ClientViewModel : ViewModelBase {
    public ClientViewModel(ILogger<ClientViewModel> logger, MatrixAuthenticationService authService, CommandLineConfiguration cfg) {
        _logger = logger;
        _authService = authService;
        _cfg = cfg;
        DisplayedSpaces.Add(_allRoomsNode = new AllRoomsSpaceNode(this));
        _ = Task.Run(Run);
    }

    private readonly ILogger<ClientViewModel> _logger;
    private readonly MatrixAuthenticationService _authService;
    private readonly CommandLineConfiguration _cfg;
    private SpaceNode? _currentSpace;
    private readonly SpaceNode _allRoomsNode;
    private string _status = "Loading...";
    public ObservableCollection<SpaceNode> DisplayedSpaces { get; } = [];
    public ObservableDictionary<string, RoomNode> AllRooms { get; } = new();

    public SpaceNode CurrentSpace {
        get => _currentSpace ?? _allRoomsNode;
        set => SetProperty(ref _currentSpace, value);
    }

    public string Status {
        get => _status + " " + DateTime.Now;
        set => SetProperty(ref _status, value);
    }

    public async Task Run() {
        Status = "Interrupted.";
        return;
        Status = "Doing initial sync...";
        var sh = new SyncStateResolver(_authService.Homeserver, _logger, storageProvider: new FileStorageProvider(Path.Combine(_cfg.ProfileDirectory, "syncCache")));
        // var res = await sh.SyncAsync();
        //await sh.OptimiseStore();
        while (true) {
            // Status = "Syncing...";
            var res = await sh.ContinueAsync();
            Status = $"Processing sync... {res.next.NextBatch}";
            await ApplySpaceChanges(res.next);
            //OnPropertyChanged(nameof(CurrentSpace));
            //OnPropertyChanged(nameof(CurrentSpace.ChildRooms));
            // Console.WriteLine($"mow A={AllRooms.Count}|D={DisplayedSpaces.Count}");
            // for (int i = 0; i < GC.MaxGeneration; i++) {
            // GC.Collect(i, GCCollectionMode.Forced, blocking: true);
            // GC.WaitForPendingFinalizers();
            // }
            Status = "Syncing...";
        }
    }

    private async Task ApplySpaceChanges(SyncResponse newSync) {
        List<Task> tasks = [];
        foreach (var room in newSync.Rooms?.Join ?? []) {
            if (!AllRooms.ContainsKey(room.Key)) {
                AllRooms.Add(room.Key, new RoomNode { Name = "Loading..." });
            }

            if (room.Value.State?.Events is not null) {
                var nameEvent = room.Value.State!.Events!.FirstOrDefault(x => x.Type == "m.room.name" && x.StateKey == "");
                AllRooms[room.Key].Name = (nameEvent?.TypedContent as RoomNameEventContent)?.Name ?? "";
                if (string.IsNullOrWhiteSpace(AllRooms[room.Key].Name)) {
                    AllRooms[room.Key].Name = "Loading...";
                    tasks.Add(_authService.Homeserver!.GetRoom(room.Key).GetNameOrFallbackAsync().ContinueWith(r => AllRooms[room.Key].Name = r.Result));
                }
            }
        }
        
        await Task.WhenAll(tasks);

        return;

        List<string> handledRoomIds = [];
        var spaces = newSync.Rooms?.Join?
            .Where(x => x.Value.State?.Events is not null)
            .Where(x => x.Value.State!.Events!.Any(y => y.Type == "m.room.create" && (y.TypedContent as RoomCreateEventContent)!.Type == "m.space"))
            .ToList();
        Console.WriteLine("spaces: " + spaces.Count);
        var nonRootSpaces = spaces
            .Where(x => spaces.Any(x => x.Value.State!.Events!.Any(y => y.Type == "m.space.child" && y.StateKey == x.Key)))
            .ToDictionary();

        var rootSpaces = spaces
            .Where(x => !nonRootSpaces.ContainsKey(x.Key))
            .ToDictionary();
        // var rootSpaces = spaces
        // .Where(x=>!spaces.Any(x=>x.Value.State!.Events!.Any(y=>y.Type == "m.space.child" && y.StateKey == x.Key)))
        // .ToList();

        foreach (var (roomId, room) in rootSpaces) {
            var space = new SpaceNode { Name = (room.State!.Events!.First(x => x.Type == "m.room.name")!.TypedContent as RoomNameEventContent).Name };
            DisplayedSpaces.Add(space);
            handledRoomIds.Add(roomId);
        }
    }
}

public class SpaceNode : RoomNode {
    public ObservableCollection<SpaceNode> ChildSpaces { get; set; } = [];
    public ObservableCollection<RoomNode> ChildRooms { get; set; } = [];
}

public class RoomNode {
    public string Name { get; set; }
}

// implementation details
public class AllRoomsSpaceNode : SpaceNode {
    public AllRoomsSpaceNode(ClientViewModel vm) {
        Name = "All rooms";
        vm.AllRooms.CollectionChanged += (_, args) => {
            switch (args.Action) {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace: {
                    foreach (var room in args.NewItems?.Cast<KeyValuePair<string, RoomNode>>() ?? []) ChildRooms.Add(room.Value);
                    foreach (var room in args.OldItems?.Cast<KeyValuePair<string, RoomNode>>() ?? []) ChildRooms.Remove(room.Value);
                    break;
                }

                case NotifyCollectionChangedAction.Reset: {
                    ChildSpaces.Clear();
                    ChildRooms.Clear();
                    break;
                }
            }
        };
    }
}