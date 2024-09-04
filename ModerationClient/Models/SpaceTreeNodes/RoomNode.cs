using ArcaneLibs;

namespace ModerationClient.Models.SpaceTreeNodes;

public class RoomNode : NotifyPropertyChanged {
    private string? _name;

    public string RoomID { get; set; }

    public string? Name {
        get => _name;
        set => SetField(ref _name, value);
    }
}