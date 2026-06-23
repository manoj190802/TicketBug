using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketBug.Backend.Models
{
    public class Developer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Skills")]
        public List<string> Skills { get; set; } = new();

        [BsonElement("Experience")]
        public int Experience { get; set; }

        [BsonElement("Workload")]
        public int Workload { get; set; }

        [BsonElement("AvailabilityStatus")]
        public string AvailabilityStatus { get; set; } = "Available"; // "Available", "Busy", "On Leave"
    }
}
