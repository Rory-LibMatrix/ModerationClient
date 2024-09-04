using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ArcaneLibs;
using ArcaneLibs.Collections;
using LibMatrix;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Helpers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;
using ModerationClient.Models.SpaceTreeNodes;
using ModerationClient.Services;

namespace ModerationClient.ViewModels;

public partial class ClientViewModel : ViewModelBase {
    public ClientViewModel(ILogger<ClientViewModel> logger, MatrixAuthenticationService authService, CommandLineConfiguration cfg) {
        _logger = logger;
        _authService = authService;
        _cfg = cfg;
        DisplayedSpaces.Add(_allRoomsNode = new AllRoomsSpaceNode(this));
        DisplayedSpaces.Add(DirectMessages = new SpaceNode(false) { Name = "Direct messages" });
        _ = Task.Run(Run).ContinueWith(x => {
            if (x.IsFaulted) {
                Status = "Critical error running client view model: " + x.Exception?.Message;
                _logger.LogError(x.Exception, "Error running client view model.");
            }
        });
    }

    private readonly ILogger<ClientViewModel> _logger;
    private readonly MatrixAuthenticationService _authService;
    private readonly CommandLineConfiguration _cfg;
    private SpaceNode? _currentSpace;
    private readonly SpaceNode _allRoomsNode;
    private string _status = "Loading...";
    public ObservableCollection<SpaceNode> DisplayedSpaces { get; } = [];
    public ObservableDictionary<string, RoomNode> AllRooms { get; } = new();
    public SpaceNode DirectMessages { get; }

    public bool Paused { get; set; } = false;

    public SpaceNode CurrentSpace {
        get => _currentSpace ?? _allRoomsNode;
        set => SetProperty(ref _currentSpace, value);
    }

    public string Status {
        get => _status + " " + DateTime.Now;
        set => SetProperty(ref _status, value);
    }

    public async Task Run() {
        Console.WriteLine("Running client view model loop...");
        ArgumentNullException.ThrowIfNull(_authService.Homeserver, nameof(_authService.Homeserver));
        // var sh = new SyncStateResolver(_authService.Homeserver, _logger, storageProvider: new FileStorageProvider(Path.Combine(_cfg.ProfileDirectory, "syncCache")));
        var store = new FileStorageProvider(Path.Combine(_cfg.ProfileDirectory, "syncCache"));
        Console.WriteLine($"Sync store at {store.TargetPath}");

        var sh = new SyncHelper(_authService.Homeserver, _logger, storageProvider: store) {
            // MinimumDelay = TimeSpan.FromSeconds(1)
        };
        Console.WriteLine("Sync helper created.");

        //optimise - we create a new scope here to make ssr go out of scope
        // if((await sh.GetUnoptimisedStoreCount()) > 1000)
        {
            Console.WriteLine("RUN - Optimising sync store...");
            Status = "Optimising sync store, please wait...";
            var ssr = new SyncStateResolver(_authService.Homeserver, _logger, storageProvider: store);
            Console.WriteLine("Created sync state resolver...");
            Status = "Optimising sync store, please wait... Creating new snapshot...";
            await ssr.OptimiseStore();
            Status = "Optimising sync store, please wait... Deleting old intermediate snapshots...";
            await ssr.RemoveOldSnapshots();
        }

        var unoptimised = await sh.GetUnoptimisedStoreCount(); // this is slow, so we cache
        Status = "Doing initial sync...";
        await foreach (var res in sh.EnumerateSyncAsync()) {
            Program.Beep((short)250, 0);
            Status = $"Processing sync... {res.NextBatch}";
            await ApplySyncChanges(res);

            Program.Beep(0, 0);
            if (Paused) {
                Status = "Sync loop interrupted... Press pause/break to resume.";
                while (Paused) await Task.Delay(1000);
            }
            else Status = $"Syncing... {unoptimised++} unoptimised sync responses...";
        }
    }

    private async Task ApplySyncChanges(SyncResponse newSync) {
        await ApplySpaceChanges(newSync);
        if (newSync.AccountData?.Events?.FirstOrDefault(x => x.Type == "m.direct") is { } evt) {
            await ApplyDirectMessagesChanges(evt);
        }
    }

    private async Task ApplySpaceChanges(SyncResponse newSync) {
        List<Task> tasks = [];
        foreach (var room in newSync.Rooms?.Join ?? []) {
            if (!AllRooms.ContainsKey(room.Key)) {
                // AllRooms.Add(room.Key, new RoomNode { Name = "Loading..." });
                AllRooms.Add(room.Key, new RoomNode { Name = "", RoomID = room.Key });
            }

            if (room.Value.State?.Events is not null) {
                var nameEvent = room.Value.State!.Events!.FirstOrDefault(x => x.Type == "m.room.name" && x.StateKey == "");
                if (nameEvent is not null)
                    AllRooms[room.Key].Name = (nameEvent?.TypedContent as RoomNameEventContent)?.Name ?? "";
            }

            if (string.IsNullOrWhiteSpace(AllRooms[room.Key].Name)) {
                AllRooms[room.Key].Name = "Loading...";
                tasks.Add(_authService.Homeserver!.GetRoom(room.Key).GetNameOrFallbackAsync().ContinueWith(r => AllRooms[room.Key].Name = r.Result));
                // Status = $"Getting room name for {room.Key}...";
                // AllRooms[room.Key].Name = await _authService.Homeserver!.GetRoom(room.Key).GetNameOrFallbackAsync();
            }
        }

        await AwaitTasks(tasks, "Waiting for {0}/{1} tasks while applying room changes...");

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

    private async Task ApplyDirectMessagesChanges(StateEventResponse evt) {
        _logger.LogCritical("Direct messages updated!");
        var dms = evt.RawContent.Deserialize<Dictionary<string, string[]?>>();
        List<Task> tasks = [];
        foreach (var (userId, roomIds) in dms) {
            if (roomIds is null || roomIds.Length == 0) continue;
            var space = DirectMessages.ChildSpaces.FirstOrDefault(x => x.RoomID == userId);
            if (space is null) {
                space = new SpaceNode { Name = userId, RoomID = userId };
                tasks.Add(_authService.Homeserver!.GetProfileAsync(userId)
                    .ContinueWith(r => space.Name = string.IsNullOrWhiteSpace(r.Result?.DisplayName) ? userId : r.Result.DisplayName));
                DirectMessages.ChildSpaces.Add(space);
            }

            foreach (var roomId in roomIds) {
                var room = space.ChildRooms.FirstOrDefault(x => x.RoomID == roomId);
                if (room is null) {
                    room = AllRooms.TryGetValue(roomId, out var existing) ? existing : new RoomNode { Name = "Unknown: " + roomId, RoomID = roomId };
                    space.ChildRooms.Add(room);
                }
            }

            foreach (var spaceChildRoom in space.ChildRooms.ToList()) {
                if (!roomIds.Contains(spaceChildRoom.RoomID)) {
                    space.ChildRooms.Remove(spaceChildRoom);
                }
            }
        }

        await AwaitTasks(tasks, "Waiting for {0}/{1} tasks while applying DM changes...");
    }

    private async Task AwaitTasks(List<Task> tasks, string message) {
        if (tasks.Count > 0) {
            int total = tasks.Count;
            while (tasks.Any(x => !x.IsCompleted)) {
                int incomplete = tasks.Count(x => !x.IsCompleted);
                Program.Beep((short)MathUtil.Map(incomplete, 0, total, 20, 7500), 5);
                // Program.Beep(0, 0);
                Status = string.Format(message, incomplete, total);
                await Task.WhenAny(tasks);
                tasks.RemoveAll(x => x.IsCompleted);
            }

            Program.Beep(0, 0);
        }
    }
}

// implementation details
public class AllRoomsSpaceNode : SpaceNode {
    public AllRoomsSpaceNode(ClientViewModel vm) : base(false) {
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