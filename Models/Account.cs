using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoneWorkingBackend.Models
{
    [Serializable]
    public class Account
    {
        // Schema for an account entity
        // Any fields not sent by the frontend must be nullable
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}

        [BsonElement("Name")]
        public string? Name{get; set;} = null!;

        public string Email {get; set;}

        public string Password {get; set;}

        public string? Salt {get; set;} = null!;

        public bool? Verified {get; set;}

        public string? AuthCode {get; set;} = null!;

        public string? currentRoom{get; set;} = null!;
        public string? signInTime{get; set;} = null!;

        public int[][] signInHeatmap{get; set;} = new int[4][];

        public int? heatmapLastUpdate{get; set;}
        
        public bool? Admin {get; set;}
    }
}