using System;
using System.Linq;
using System.Text.Json;
using LiteDB;
using FemRec2023.Classes;
using FemRec2023.Classes.DBs.DBClasses;
using static FemRec2023.Classes.DBs.DBClasses.RoomDBClasses;

namespace FemRec2023.Classes.DBs
{
    public class RoomDB
    {
        public static LiteDatabase RoomDBFile = new LiteDatabase(Path.Combine(Program.dataDir, "DBs", "Rooms.db"));
        public static readonly ILiteCollection<Room> Rooms = RoomDBFile.GetCollection<Room>("Rooms");

        public static async Task ImportRooms(string path)
        {
            try
            {
                string jsonData = await File.ReadAllTextAsync(path);
                
                var roomsList = System.Text.Json.JsonSerializer.Deserialize<List<Room>>(jsonData);

                if (roomsList == null) return;

                foreach (var room in roomsList)
                {
                    await AddRoom(room, log: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Import Error] Failed to import JSON: {ex.Message}");
            }
        }

        public static async Task AddRoom(Room newRoom, bool log = false, bool shouldAssignNewIds = true)
        {
            if (newRoom == null) return;

            await Task.Run(() =>
            {
                try
                {
                    if (shouldAssignNewIds || newRoom.RoomId <= 0)
                    {
                        newRoom.RoomId = GetNextRoomId();
                    }

                    newRoom.RankedEntityId = newRoom.RoomId.ToString();

                    if (newRoom.SubRooms != null && newRoom.SubRooms.Any())
                    {
                        long currentMaxSubId = GetNextSubRoomId();
                        foreach (var sub in newRoom.SubRooms)
                        {
                            if (shouldAssignNewIds || sub.SubRoomId <= 0)
                            {
                                sub.SubRoomId = currentMaxSubId;
                                currentMaxSubId++;
                            }
                            sub.RoomId = newRoom.RoomId;
                        }
                    }

                    Rooms.Insert(newRoom);

                    if (log)
                    {
                        Console.WriteLine($"[DB] Added room: {newRoom.Name} (ID: {newRoom.RoomId})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB Error] Failed to add room {newRoom.Name}: {ex.Message}");
                }
            });
        }

        public static long GetNextRoomId()
        {
            if (Rooms.Count() == 0) 
                return 1;
            
            var maxId = Rooms.Max(x => x.RoomId);

            return Convert.ToInt64(maxId) + 1;
        }

        public static long GetNextSubRoomId()
        {
            var allRooms = Rooms.FindAll().ToList();

            if (allRooms.Count == 0)
                return 1;

            long maxSubId = 0;
            foreach (var room in allRooms)
            {
                if (room.SubRooms != null && room.SubRooms.Any())
                {
                    long currentMax = room.SubRooms.Max(s => Convert.ToInt64(s.SubRoomId));
                    if (currentMax > maxSubId) maxSubId = currentMax;
                }
            }
            return maxSubId + 1;
        }
        
        public static Room GetRoom(long roomId)
        {
            return Rooms.FindById(roomId);
        }

        public static Room GetRoomByName(string name)
        {
            return Rooms.FindOne(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public static List<Room> GetRoomsByNames(List<string> names)
        {
            if (names == null || names.Count == 0) 
                return new List<Room>();
            
            return Rooms.Find(room => 
                names.Contains(room.Name, StringComparer.OrdinalIgnoreCase)
            ).ToList();
        }
        
        public static (List<Room> Results, int Total) GetHotRooms(string tag, int skip, int take)
        {
            var query = Rooms.Query().Where(r => !r.IsDorm);
            string t = tag?.ToLower();
            bool isRRO = (t == "rro" || t == "recroomoriginal");

            if (isRRO) 
            {
                query = query.Where("Tags[*].Tag ANY IN ['rro', 'recroomoriginal']");
            }
            else if (t != "new") 
            {
                query = query.Where("Tags[*].Tag ALL NOT IN ['base', 'rro', 'recroomoriginal']");
            }

            if (!isRRO) 
            {
                query = query.Where(r => r.Accessibility == RoomAccessibility.Public);
            }

            var finalQuery = (t == "new") 
                ? query.OrderByDescending(r => r.CreatedAt) 
                : query.OrderByDescending(r => r.Stats.VisitCount);

            var results = finalQuery.Skip(skip).Limit(take).ToList();
            int total = finalQuery.Count();

            return (results, total);
        }
    }
}