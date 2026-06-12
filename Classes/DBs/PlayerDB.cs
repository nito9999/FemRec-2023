using System;
using LiteDB;
using FemRec2023.Classes;
using FemRec2023.Classes.DBs.DBClasses;
using static FemRec2023.Classes.DBs.DBClasses.PlayerDBClasses;

namespace FemRec2023.Classes.DBs
{
    public class PlayerDB
    {
        public static LiteDatabase PlayerDBFile = new LiteDatabase(Path.Combine(Program.dataDir, "DBs", "Players.db"));
        public static readonly ILiteCollection<FullPlayer> Players = PlayerDBFile.GetCollection<FullPlayer>("Players");
        
        public static FullPlayer CreateAccount(Platforms platform, ulong platformId, bool isJunior)
        {
            string username = NameGen.GetRandomName();
            var newPlayerData = new Player
            {
                Username = username,
                DisplayName = username,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                ProfileImage = "DefaultPFP.png",
                AvailableUsernameChanges = 3,
                IsJunior = isJunior,
                Level = 1,
                XP = 0,
                PlayerExtra = new PlayerExtra
                {
                    Settings = new List<Setting>
                            {
                                new Setting { Key = "Recroom.AccountCreation.HasStarted", Value = "True" },
                                new Setting { Key = "Recroom.AccountCreation.HasChosenUsername", Value = "True" },
                                new Setting { Key = "Recroom.AccountCreation.HasCreatedPassword", Value = "True" },
                                new Setting { Key = "Recroom.AccountCreation.HasFinished", Value = "True" },
                                new Setting { Key = "TUTORIAL_COMPLETE_MASK", Value = "57" }
                            }
                }
            };

            var newFullPlayer = new FullPlayer
            {
                PlatformIds = new List<mPlatformID> 
                { 
                    new mPlatformID { Platform = platform, PlatformId = platformId } 
                },
                Player = newPlayerData,
                PlayerRoles = new List<PlayerRoles> { PlayerRoles.Developer },
                AuthToken = Guid.NewGuid().ToString()
            };
            
            Players.Insert(newFullPlayer);

            return newFullPlayer;
        }
        
        public static bool GetLogins(Platforms platform, ulong platformId, out List<CachedLogins> accounts)
        {
            var results = Players.Find(x => x.PlatformIds
                            .Select(p => p.PlatformId)
                            .Any(id => id == platformId))
                            .Where(p => p.PlatformIds.Any(pid => pid.Platform == platform))
                            .OrderByDescending(x => x.Player.LastLoginAt)
                            .ToList();

            accounts = results.Select(p => new CachedLogins
            {
                accountId = p.PlayerId,
                lastLoginTime = p.Player.LastLoginAt,
                platform = platform,
                platformId = platformId.ToString()
            }).ToList();

            return accounts.Count > 0;
        }
        
        private static PlayerDTOBase MapToDTO(FullPlayer player, bool accountMe)
        {
            int platformFlags = player.PlatformIds?.Aggregate(0, (acc, pid) => acc | (int)pid.Platform) ?? 0;

            var p = player.Player ?? new Player();

            PlayerDTOBase dto = accountMe ? new PlayerMeDTO() : new PlayerDTO();

            dto.accountId = player.PlayerId;
            dto.username = p.Username;
            dto.displayName = p.DisplayName;
            dto.profileImage = p.ProfileImage;
            dto.isJunior = p.IsJunior;
            dto.createdAt = p.CreatedAt;
            dto.platforms = platformFlags;
            dto.personalPronouns = 0;
            dto.identityFlags = 0;

            if (accountMe && dto is PlayerMeDTO meDto)
            {
                meDto.availableUsernameChanges = p.AvailableUsernameChanges;
                meDto.birthday = p.Birthday;
                meDto.email = p.Email;
                meDto.phone = null;
            }

            return dto;
        }
        
        public static List<PlayerDTOBase> GetAccountsBulk(List<long> playerIds)
        {
            var players = Players.Find(x => playerIds.Contains(x.PlayerId)).ToList();
            
            return players
                .Select(p => MapToDTO(p, false))
                .OrderBy(a => a.accountId)
                .ToList();
        }
        
        public static PlayerMeDTO? GetAccountMe(long accountId)
        {
            var player = Players.FindById(accountId);
            if (player == null)
                return null;
            
            return MapToDTO(player, true) as PlayerMeDTO;
        }
        
        public static bool SetAvatar(long accountId, Avatar avatar)
        {
            var player = Players.FindById(accountId);
            if (player == null || player.Player == null) 
                return false;

            player.Player.PlayerExtra.Avatar = avatar;
            return Players.Update(player);
        }
        
        public static void SetPlayerSetting(string key, string value, long playerId)
        {
            if (string.IsNullOrWhiteSpace(key) || playerId <= 0)
                return;

            if (key is "SplitTestAssignedSegments" or "Growth.LastEmailPromptTime")
                return;

            var player = Players.FindById(playerId);
            if (player == null || player.Player == null) 
                return;

            var settings = player.Player.PlayerExtra.Settings;
            var existingSetting = settings.FirstOrDefault(s => s.Key == key);

            if (existingSetting != null)
            {
                existingSetting.Value = value;
            }
            else
            {
                settings.Add(new Setting { Key = key, Value = value });
            }

            Players.Update(player);
        }
        
        public static List<PlayerProgressionDTO> GetProgressionBulk(List<long> playerIds)
        {
            var players = Players.Find(x => playerIds.Contains(x.PlayerId)).ToList();

            return players.Select(p => new PlayerProgressionDTO
            {
                PlayerId = p.PlayerId,
                Level = p.Player?.Level ?? 1,
                XP = p.Player?.XP ?? 0
            }).ToList();
        }
        
        public static List<Reputation> GetReputationBulk(List<long> playerIds)
        {
            var players = Players.Find(x => playerIds.Contains(x.PlayerId)).ToList();

            return players.Select(p => {
                var rep = p.Player?.Reputation ?? new Reputation();
                return new Reputation
                {
                    AccountId = p.PlayerId,
                    IsCheerful = rep.IsCheerful,
                    Noteriety = rep.Noteriety,
                    SelectedCheer = rep.SelectedCheer,
                    CheerCredit = rep.CheerCredit,
                    CheerGeneral = rep.CheerGeneral,
                    CheerHelpful = rep.CheerHelpful,
                    CheerCreative = rep.CheerCreative,
                    CheerGreatHost = rep.CheerGreatHost,
                    CheerSportsman = rep.CheerSportsman,
                    SubscriberCount = rep.SubscriberCount,
                    SubscribedCount = rep.SubscribedCount
                };
            }).ToList();
        }
        
        public static Heartbeat GetPlayerHeartbeat(long playerId)
        {
            var player = Players.FindOne(x => x.PlayerId == playerId);
            var hb = player?.Player.PlayerExtra.Heartbeat;
            hb.playerId = playerId;
            return hb;
        }
        
        public static List<Heartbeat> GetPlayerHeartbeatsBulk(List<long> playerIds)
        {
            var players = Players.Find(x => playerIds.Contains(x.PlayerId)).ToList();

            return players
                .Select(p => 
                {
                    var hb = p.Player?.PlayerExtra?.Heartbeat ?? new Heartbeat();
                    hb.playerId = p.PlayerId;
                    return hb;
                })
                .ToList();
        }
        
        public static Heartbeat? UpdatePlayerHeartbeat(
            long playerId,
            RoomInstance? roomInstance,
            bool online = true,
            Platforms platform = Platforms.All,
            DeviceClasses deviceClasses = DeviceClasses.Unknown)
        {
            var player = Players.FindById(playerId);

            if (player != null)
            {
                player.Player.PlayerExtra ??= new PlayerExtra();
                player.Player.PlayerExtra.Heartbeat ??= new Heartbeat();

                var hb = player.Player.PlayerExtra.Heartbeat;

                hb.roomInstance = online ? roomInstance : null;
                hb.errorCode = 0;
                hb.isOnline = online;

                if (hb.roomInstance != null)
                    hb.roomInstance.dataBlob = "";

                Players.Update(player);
                return hb;
            }
            else
            {
                return null; 
            }
        }
    }
}