using Microsoft.AspNetCore.Mvc;
using FemRec2023.Classes;
using FemRec2023.Classes.DBs;
using FemRec2023.Classes.DBs.DBClasses;
using FemRec2023.Auth;

namespace FemRec2023.Controllers
{
    [ApiController]
    [Route("/roomserver")]
    public class RoomController : ControllerBase
    {
        [HttpGet("rooms")]
        public async Task<IActionResult> GetRoomBy([FromQuery] string? name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var room = RoomDB.GetRoomByName(name);
                return room != null ? Ok(room) : NotFound();
            }

            return NotFound();
        }
        
        [HttpGet("rooms/{roomId}")]
        public async Task<IActionResult> GetRoomById(long roomId)
        {
            var room = RoomDB.GetRoom(roomId);
            return room != null ? Ok(room) : NotFound();
        }
        
        [HttpGet("rooms/bulk")]
        public async Task<IActionResult> GetRoomsByNames([FromQuery] List<string> name)
        {
            var rooms = RoomDB.GetRoomsByNames(name);
            return Ok(rooms);
        }
        
        [HttpGet("photon_access_token")]
        public async Task<IActionResult> GetPhotonAccessToken()
        {
        	var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();
                
        	var permissions = new List<object>
            {
                new { Override = true, Permission = "CAN_USE_ROOM_RESET_BUTTON", Role = 0, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_USE_DELETE_ALL_BUTTON", Role = 0, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_SAVE_INVENTIONS", Role = 0, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_SPAWN_INVENTIONS", Role = 0, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_USE_PLAY_GIZMOS_TOGGLE", Role = 0, Type = 0, Value = "True" },
                new { Override = false, Permission = "CAN_USE_MAKER_PEN", Role = 30, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_USE_ROOM_RESET_BUTTON", Role = 30, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_USE_DELETE_ALL_BUTTON", Role = 30, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_SAVE_INVENTIONS", Role = 30, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_SPAWN_INVENTIONS", Role = 30, Type = 0, Value = "True" },
                new { Override = true, Permission = "CAN_USE_PLAY_GIZMOS_TOGGLE", Role = 30, Type = 0, Value = "True" }
            };
            
            var heartbeat = PlayerDB.GetPlayerHeartbeat((long)id);
            var response = new
            {
                Permissions = permissions.ToArray(),
                PhotonAccessToken = "",
                RoomInstanceId = heartbeat?.roomInstance?.roomInstanceId
            };

            return Ok(response);
        }
        
        [HttpGet("rooms/hot")]
        public IActionResult HotRooms(string tag, int skip = 0, int take = 30)
        {
            var (results, total) = RoomDB.GetHotRooms(tag, skip, take);

            return Ok(new 
            {
            	Results = results ?? new List<RoomDBClasses.Room>(),
                TotalResults = total
            });
        }
        
        [HttpGet("rooms/{roomId}/interactionby/me")]
        public IActionResult GetInteractionByMe(long roomId)
        {
            return Ok(new 
            {
            	Cheered = false,
                Favorited = false,
                LastVisitedAt = DateTime.UtcNow
            });
        }
    }
}