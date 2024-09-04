using System.Collections.ObjectModel;

namespace ModerationClient.Models.SpaceTreeNodes;

public class SpaceNode : RoomNode {
    private bool _isExpanded = false;

    public SpaceNode(bool includeSelf = true) {
        if(includeSelf)
            ChildRooms = [this];
    }

    public ObservableCollection<SpaceNode> ChildSpaces { get; set; } = [];
    public ObservableCollection<RoomNode> ChildRooms { get; set; } = [];
}