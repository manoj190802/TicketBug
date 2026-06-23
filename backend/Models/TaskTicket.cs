using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketBug.Backend.Models
{
    public class TaskTicket
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("Description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("Category")]
        public string Category { get; set; } = "Full Stack"; // "Frontend", "Backend", "Full Stack"

        [BsonElement("RequiredSkills")]
        public List<string> RequiredSkills { get; set; } = new();

        [BsonElement("AssignedDeveloperId")]
        public string? AssignedDeveloperId { get; set; }

        [BsonElement("AssignedDeveloperName")]
        public string? AssignedDeveloperName { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; } = "Pending"; // "Pending", "Assigned", "Completed"

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
