using FemRec2023.Classes.DBs;
using FemRec2023.Classes.DBs.DBClasses;
using static FemRec2023.Classes.DBs.DBClasses.PlayerDBClasses;
using static FemRec2023.Classes.DBs.DBClasses.RoomDBClasses;

namespace FemRec2023.Classes
{
    public class Sessions
    {
        private static readonly Random _rng = new();

        public static Heartbeat? CreateRoom(long playerId, long roomId, string sceneName = "", bool isPrivate = false)
        {
            var roomData = RoomDB.GetRoom(roomId);
            if (roomData == null) 
                return null;

            var sub = roomData.SubRooms?.FirstOrDefault();
            if (sub == null) 
                return null;

            long instanceNumber = _rng.Next(1000, 999999999);
            var photonSuffix = isPrivate ? $"-private-room-{instanceNumber}-{Guid.NewGuid()}" : "room";
            var roomName = roomData.Name ?? "UnknownRoom";
            
            var session = new RoomInstance {
                Name = $"^{roomName}",
                dataBlob = sub.DataBlob ?? "",
                isPrivate = isPrivate,
                location = sub.UnitySceneId,
                maxCapacity = sub.MaxPlayers,
                photonRegion = "eu", photonRegionId = "eu",
                photonRoomId = $"FemRecRoom-{roomName}-{photonSuffix}",
                roomId = (long)roomData.RoomId,
                roomInstanceId = instanceNumber,
                subRoomId = (long)sub.SubRoomId
            };

            if (!string.IsNullOrEmpty(sceneName)) ApplySceneSettings(session, roomData, sceneName);
            else ApplyRandomPublicSubroom(session, roomData);
            
            var newHeartbeat = PlayerDB.UpdatePlayerHeartbeat(playerId, session);
            return newHeartbeat;
        }

        public static Heartbeat CreateDorm(long playerId, string name)
        {
            long dormId = 1;
            var dorm = RoomDB.GetRoom(dormId);
            long instanceId = _rng.Next(1000, 999999999);
            
            var session = new RoomInstance 
            {
                isPrivate = true,
                location = "76d98498-60a1-430c-ab76-b54a29b7a163",
                maxCapacity = 4,
                Name = $"@{name}'s Dorm",
                photonRegion = "eu", photonRegionId = "eu",
                photonRoomId = $"FemRecDorm-{instanceId}-room",
                roomId = dormId,
                roomInstanceId = instanceId,
                subRoomId = (long)(dorm?.SubRooms?.FirstOrDefault()?.SubRoomId ?? 0)
            };
            
            var newHeartbeat = PlayerDB.UpdatePlayerHeartbeat(playerId, session);
            return newHeartbeat;
        }

        private static void ApplySceneSettings(RoomInstance sess, Room data, string scene)
        {
            var target = data.SubRooms?.FirstOrDefault(s => s.Name.Equals(scene, StringComparison.OrdinalIgnoreCase));
            if (target != null) UpdateSession(sess, target);
        }

        private static void ApplyRandomPublicSubroom(RoomInstance sess, Room data)
        {
            var publics = data.SubRooms?.Where(s => s.Accessibility == RoomAccessibility.Public).ToList();
            if (publics?.Any() == true) UpdateSession(sess, publics[_rng.Next(publics.Count)]);
        }

        private static void UpdateSession(RoomInstance sess, SubRooms sub)
        {
            sess.dataBlob = sub.DataBlob ?? "";
            sess.subRoomId = (long)sub.SubRoomId;
            sess.location = sub.UnitySceneId;
        }
    }
}
