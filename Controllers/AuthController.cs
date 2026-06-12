using Microsoft.AspNetCore.Mvc;
using FemRec2023.Classes;
using FemRec2023.Classes.DBs;
using FemRec2023.Classes.DBs.DBClasses;
using FemRec2023.Auth;

namespace FemRec2023.Controllers
{
    [ApiController]
    [Route("/auth")]
    public class AuthController : ControllerBase
    {
        [HttpGet("eac/challenge")]
        public IActionResult GetEACChallenge()
        {
            string challenge = $"\"e\"";
            return Ok(challenge);
        }
        
        [HttpGet("cachedlogin/forplatformid/{platform}/{platformId}")]
        public IActionResult GetCachedLogins(PlayerDBClasses.Platforms platform, ulong platformId)
        {
            if (PlayerDB.GetLogins(platform, platformId, out var accounts) && accounts.Count > 0)
            {
                return Ok(accounts);
            }
            
            var newPlayer = PlayerDB.CreateAccount(platform, platformId, false); // todo use connect token create acc grant type instead but ts is for now

            var newCachedLogin = new List<PlayerDBClasses.CachedLogins>
            {
                new PlayerDBClasses.CachedLogins
                {
                    accountId = newPlayer.PlayerId,
                    lastLoginTime = newPlayer.Player.LastLoginAt,
                    platform = platform,
                    platformId = platformId.ToString(),
                    requirePassword = false
                }
            };

            return Ok(newCachedLogin);
        }
        
        [HttpPost("connect/token")]
        public async Task<IActionResult> ConnectToken(
            [FromForm] string grant_type,
            [FromForm] long account_id,
            [FromForm] string client_id,
            [FromForm] string client_secret,
            [FromForm] PlayerDBClasses.Platforms platform,
            [FromForm] ulong platform_id,
            [FromForm] string device_id,
            [FromForm] PlayerDBClasses.DeviceClasses? device_class,
            [FromForm] DateTime? time,
            [FromForm] int? ver,
            [FromForm] string build_key,
            [FromForm] string asid,
            [FromForm] string eac_challenge,
            [FromForm] string eac_response,
            [FromForm] string platform_auth
        )
        {
            switch (grant_type)
            {
                case "cached_login":
                {
                    string token = AuthStuff.Encode(account_id);
					Console.WriteLine("bitch try to login: who? here: " + account_id);
                    return Ok(new
                    {
                        access_token = token,
                        error = "",
                        error_description = "",
                        refresh_token = "eeeeeeeeeeee",
                        key = ""
                    });
                }

                default:
                    return BadRequest();
            }
        }
    }
}