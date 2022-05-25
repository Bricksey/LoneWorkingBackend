using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoneWorkingBackend.Models
{
    public class Sensor
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}
        public string timeSent {get; set;}
        public Dictionary<string, int> timeRecieved {get; set;}
    }
}