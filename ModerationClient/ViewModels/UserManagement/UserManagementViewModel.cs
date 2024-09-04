using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using ArcaneLibs.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Homeservers.ImplementationDetails.Synapse.Models.Responses;
using Microsoft.Extensions.Logging;
using ModerationClient.Services;

namespace ModerationClient.ViewModels;

public partial class UserManagementViewModel : ViewModelBase {
    public UserManagementViewModel(ILogger<UserManagementViewModel> logger, MatrixAuthenticationService authService, CommandLineConfiguration cfg) {
        _logger = logger;
        _authService = authService;
        _cfg = cfg;
        _ = Task.Run(Run).ContinueWith(x=>x.Exception?.Handle(y=> {
            Console.WriteLine(y);
            return true;
        }));
    }

    private readonly ILogger<UserManagementViewModel> _logger;
    private readonly MatrixAuthenticationService _authService;
    private readonly CommandLineConfiguration _cfg;
    private string _status = "Loading...";
    public ObservableCollection<User> Users { get; set; } = [];

    public string Status {
        get => _status + " " + DateTime.Now;
        set => SetProperty(ref _status, value);
    }

    public async Task Run() {
        Users.Clear();
        Status = "Doing initial sync...";
        if (_authService.Homeserver is not AuthenticatedHomeserverSynapse synapse) {
            Console.WriteLine("This client only supports Synapse homeservers.");
            return;
        }

        await foreach (var user in synapse.Admin.SearchUsersAsync(chunkLimit: 100)) {
            Program.Beep(250, 1);
            Console.WriteLine("USERMANAGER GOT USER: " + user.ToJson(indent:false, ignoreNull: true));
            Users.Add(JsonSerializer.Deserialize<User>(user.ToJson())!);
        }
        Console.WriteLine("Done.");
    }
}

public class User : SynapseAdminUserListResult.SynapseAdminUserListResultUser {
    
}