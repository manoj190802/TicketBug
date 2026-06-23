using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketBug.Backend.Models
{
    public class AssignmentHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("TicketId")]
        public string TicketId { get; set; } = string.Empty;

        [BsonElement("TicketTitle")]
        public string TicketTitle { get; set; } = string.Empty;

        [BsonElement("DeveloperId")]
        public string DeveloperId { get; set; } = string.Empty;

        [BsonElement("DeveloperName")]
        public string DeveloperName { get; set; } = string.Empty;

        [BsonElement("AssignedAt")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("MatchScore")]
        public double MatchScore { get; set; }
    }
}
