using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoneWorkingBackend.Models
{
    public class Account
    {
        // Schema for an account entity
        // Any fields not sent by the frontend must be nullable
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}

        [BsonElement("Name")]
        public string Name{get; set;}

        public string Email {get; set;}

        public string Password {get; set;}

        public string? Salt {get; set;} = null!;

        public bool? Verified {get; set;}

        public string? AuthCode {get; set;} = null;
        
        public bool? Admin {get; set;}
    }
}