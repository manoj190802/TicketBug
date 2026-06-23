using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using TicketBug.Backend.Models;

namespace TicketBug.Backend.Services
{
    public class MongoDbService
    {
        private readonly IMongoDatabase? _database;
        private readonly IMongoCollection<Developer>? _developersCollection;
        private readonly IMongoCollection<TaskTicket>? _ticketsCollection;
        private readonly IMongoCollection<AssignmentHistory>? _historyCollection;
        
        private readonly bool _useFallback;
        private readonly string _dataDirectory;
        private readonly string _devsFile;
        private readonly string _ticketsFile;
        private readonly string _historyFile;
        
        private readonly object _lock = new();

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("MongoDbSettings:ConnectionString") ?? "mongodb://localhost:27017";
            var databaseName = configuration.GetValue<string>("MongoDbSettings:DatabaseName") ?? "TicketBugDb";

            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            _devsFile = Path.Combine(_dataDirectory, "developers.json");
            _ticketsFile = Path.Combine(_dataDirectory, "tickets.json");
            _historyFile = Path.Combine(_dataDirectory, "history.json");

            try
            {
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2); // Short timeout for fallback detection
                var client = new MongoClient(settings);
                _database = client.GetDatabase(databaseName);
                
                // Try pinging to see if MongoDB is actually running
                _database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                
                _developersCollection = _database.GetCollection<Developer>("Developers");
                _ticketsCollection = _database.GetCollection<TaskTicket>("Tickets");
                _historyCollection = _database.GetCollection<AssignmentHistory>("History");
                
                _useFallback = false;
                Console.WriteLine("Successfully connected to MongoDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MongoDB connection failed: {ex.Message}. Falling back to JSON file storage.");
                _useFallback = true;
                
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
                
                EnsureFileExists(_devsFile, "[]");
                EnsureFileExists(_ticketsFile, "[]");
                EnsureFileExists(_historyFile, "[]");
            }
            
            // Seed developers if database or JSON files are empty
            SeedDefaultDevelopers().Wait();
        }

        private void EnsureFileExists(string path, string defaultContent)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultContent);
            }
        }

        private async Task SeedDefaultDevelopers()
        {
            var devs = await GetDevelopersAsync();
            if (devs.Count == 0)
            {
                var defaultDevs = new List<Developer>
                {
                    new() { Name = "Alice Vance", Skills = new() { "angular", "html", "css", "typescript", "javascript" }, Experience = 4, Workload = 0, AvailabilityStatus = "Available" },
                    new() { Name = "Bob Sterling", Skills = new() { "dotnet", "c#", "sql", "postgres", "database" }, Experience = 5, Workload = 1, AvailabilityStatus = "Available" },
                    new() { Name = "Charlie Cruz", Skills = new() { "angular", "dotnet", "c#", "typescript", "mongodb" }, Experience = 6, Workload = 3, AvailabilityStatus = "Busy" },
                    new() { Name = "Diana Prince", Skills = new() { "angular", "react", "vue", "javascript", "tailwind", "css" }, Experience = 3, Workload = 1, AvailabilityStatus = "Available" },
                    new() { Name = "Evan Wright", Skills = new() { "python", "fastapi", "mongodb", "rest api", "docker", "backend" }, Experience = 5, Workload = 0, AvailabilityStatus = "Available" }
                };

                foreach (var dev in defaultDevs)
                {
                    await CreateDeveloperAsync(dev);
                }
                Console.WriteLine("Database seeded with default developers.");
            }
        }

        // --- DEVELOPERS ---
        public async Task<List<Developer>> GetDevelopersAsync()
        {
            if (!_useFallback)
            {
                return await _developersCollection!.Find(_ => true).ToListAsync();
            }

            lock (_lock)
            {
                var json = File.ReadAllText(_devsFile);
                return JsonSerializer.Deserialize<List<Developer>>(json) ?? new();
            }
        }

        public async Task<Developer?> GetDeveloperByIdAsync(string id)
        {
            if (!_useFallback)
            {
                return await _developersCollection!.Find(d => d.Id == id).FirstOrDefaultAsync();
            }

            lock (_lock)
            {
                var devs = JsonSerializer.Deserialize<List<Developer>>(File.ReadAllText(_devsFile)) ?? new();
                return devs.FirstOrDefault(d => d.Id == id);
            }
        }

        public async Task CreateDeveloperAsync(Developer developer)
        {
            if (!_useFallback)
            {
                await _developersCollection!.InsertOneAsync(developer);
                return;
            }

            lock (_lock)
            {
                var devs = JsonSerializer.Deserialize<List<Developer>>(File.ReadAllText(_devsFile)) ?? new();
                developer.Id ??= Guid.NewGuid().ToString("n").Substring(0, 24);
                devs.Add(developer);
                File.WriteAllText(_devsFile, JsonSerializer.Serialize(devs, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public async Task UpdateDeveloperAsync(string id, Developer updatedDeveloper)
        {
            if (!_useFallback)
            {
                await _developersCollection!.ReplaceOneAsync(d => d.Id == id, updatedDeveloper);
                return;
            }

            lock (_lock)
            {
                var devs = JsonSerializer.Deserialize<List<Developer>>(File.ReadAllText(_devsFile)) ?? new();
                var index = devs.FindIndex(d => d.Id == id);
                if (index != -1)
                {
                    updatedDeveloper.Id = id;
                    devs[index] = updatedDeveloper;
                    File.WriteAllText(_devsFile, JsonSerializer.Serialize(devs, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }

        public async Task DeleteDeveloperAsync(string id)
        {
            if (!_useFallback)
            {
                await _developersCollection!.DeleteOneAsync(d => d.Id == id);
                return;
            }

            lock (_lock)
            {
                var devs = JsonSerializer.Deserialize<List<Developer>>(File.ReadAllText(_devsFile)) ?? new();
                var dev = devs.FirstOrDefault(d => d.Id == id);
                if (dev != null)
                {
                    devs.Remove(dev);
                    File.WriteAllText(_devsFile, JsonSerializer.Serialize(devs, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }

        // --- TICKETS ---
        public async Task<List<TaskTicket>> GetTicketsAsync()
        {
            if (!_useFallback)
            {
                return await _ticketsCollection!.Find(_ => true).ToListAsync();
            }

            lock (_lock)
            {
                var json = File.ReadAllText(_ticketsFile);
                return JsonSerializer.Deserialize<List<TaskTicket>>(json) ?? new();
            }
        }

        public async Task<TaskTicket?> GetTicketByIdAsync(string id)
        {
            if (!_useFallback)
            {
                return await _ticketsCollection!.Find(t => t.Id == id).FirstOrDefaultAsync();
            }

            lock (_lock)
            {
                var tickets = JsonSerializer.Deserialize<List<TaskTicket>>(File.ReadAllText(_ticketsFile)) ?? new();
                return tickets.FirstOrDefault(t => t.Id == id);
            }
        }

        public async Task CreateTicketAsync(TaskTicket ticket)
        {
            if (!_useFallback)
            {
                await _ticketsCollection!.InsertOneAsync(ticket);
                return;
            }

            lock (_lock)
            {
                var tickets = JsonSerializer.Deserialize<List<TaskTicket>>(File.ReadAllText(_ticketsFile)) ?? new();
                ticket.Id ??= Guid.NewGuid().ToString("n").Substring(0, 24);
                tickets.Add(ticket);
                File.WriteAllText(_ticketsFile, JsonSerializer.Serialize(tickets, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public async Task UpdateTicketAsync(string id, TaskTicket updatedTicket)
        {
            if (!_useFallback)
            {
                await _ticketsCollection!.ReplaceOneAsync(t => t.Id == id, updatedTicket);
                return;
            }

            lock (_lock)
            {
                var tickets = JsonSerializer.Deserialize<List<TaskTicket>>(File.ReadAllText(_ticketsFile)) ?? new();
                var index = tickets.FindIndex(t => t.Id == id);
                if (index != -1)
                {
                    updatedTicket.Id = id;
                    tickets[index] = updatedTicket;
                    File.WriteAllText(_ticketsFile, JsonSerializer.Serialize(tickets, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }

        // --- HISTORY ---
        public async Task<List<AssignmentHistory>> GetHistoryAsync()
        {
            if (!_useFallback)
            {
                return await _historyCollection!.Find(_ => true).ToListAsync();
            }

            lock (_lock)
            {
                var json = File.ReadAllText(_historyFile);
                return JsonSerializer.Deserialize<List<AssignmentHistory>>(json) ?? new();
            }
        }

        public async Task CreateHistoryAsync(AssignmentHistory history)
        {
            if (!_useFallback)
            {
                await _historyCollection!.InsertOneAsync(history);
                return;
            }

            lock (_lock)
            {
                var list = JsonSerializer.Deserialize<List<AssignmentHistory>>(File.ReadAllText(_historyFile)) ?? new();
                history.Id ??= Guid.NewGuid().ToString("n").Substring(0, 24);
                list.Add(history);
                File.WriteAllText(_historyFile, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }
}
