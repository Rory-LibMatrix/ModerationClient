using System.Text.Json;
using LibMatrix;
using LibMatrix.Homeservers.ImplementationDetails.Synapse.Models.Responses;

// var src1 = new SynapseCollectionResult<EventIdResponse>();
// var src2 = new SynapseCollectionResult<EventIdResponse>(chunkKey: "meow", nextTokenKey: "woof", prevTokenKey: "bark", totalKey: "purr");
//
// for (int i = 0; i < 10000000; i++) {
//     src1.Chunk.Add(new EventIdResponse { EventId = Guid.NewGuid().ToString() });
//     src2.Chunk.Add(new EventIdResponse { EventId = Guid.NewGuid().ToString() });
// }
//
// File.WriteAllText("src1.json", JsonSerializer.Serialize(src1, new JsonSerializerOptions(){WriteIndented = true}));
// File.WriteAllText("src2.json", JsonSerializer.Serialize(src2, new JsonSerializerOptions(){WriteIndented = true}));

using var stream1 = File.OpenRead("src1.json");
var dst1 = new SynapseCollectionResult<EventIdResponse>().FromJson(stream1, (item) => {
    ArgumentNullException.ThrowIfNull(item.EventId);
});

var a = new StateEventResponse();