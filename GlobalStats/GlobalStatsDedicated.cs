using GlobalStats.Enums;
using LiteNetLib.Utils;
using MySqlConnector;
using NetworkedPlugins.API;
using NetworkedPlugins.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlobalStats
{
    public class GlobalStatsDedicated : NPAddonDedicated<DedicatedConfig>
    {
        public override string AddonAuthor { get; } = "Killers0992";
        public override string AddonId { get; } = "xZQETjq36QYaJC6E";
        public override string AddonName { get; } = "GlobalStats";
        public override Version AddonVersion { get; } = new Version(1, 0, 0);

        public override void OnMessageReceived(NPServer server, NetDataReader reader)
        {
            switch ((DataType)reader.GetByte())
            {
                case DataType.PlayerDeath:
                    string targetUserid = reader.GetString();
                    Roletype targetRole = (Roletype)reader.GetSByte();
                    bool targetDnt = reader.GetBool();
                    string killerUserid = reader.GetString();
                    Roletype killerRole = (Roletype)reader.GetSByte();
                    bool killerDnt = reader.GetBool();
                    if (!targetDnt)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await Check(Config.DatabaseConnectionKey, false, targetUserid);

                            await UpdateStats(Config.DatabaseConnectionKey, targetUserid, targetRole, true);
                        });
                    }
                    if (!killerDnt)
                    {
                        if (targetUserid == killerUserid)
                            return;
                        Task.Factory.StartNew(async () =>
                        {
                            await Check(Config.DatabaseConnectionKey, false, killerUserid);

                            await UpdateStats(Config.DatabaseConnectionKey, killerUserid, killerRole, false);
                            await UpdateStats(Config.DatabaseConnectionKey, killerUserid, targetRole, false, true);
                        });
                    }
                    break;
                case DataType.ThrowItem:
                    byte grenadeType = reader.GetByte();
                    string userid = reader.GetString();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid);

                        await UpdateStatsGrenade(Config.DatabaseConnectionKey, userid, grenadeType == 0 ? "flash_thrown" : grenadeType == 1 ? "grenade_thrown" : "018_thrown");
                    });
                    break;
                case DataType.DamageInfo:
                    string userid2 = reader.GetString();
                    int dmgdeal = reader.GetInt();
                    int dmgreceived = reader.GetInt();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid2);
                        await UpdateDamage(Config.DatabaseConnectionKey, userid2, dmgdeal, dmgreceived);
                    });
                    break;
                case DataType.TimePlayed:
                    string userid3 = reader.GetString();
                    Roletype role = (Roletype)reader.GetInt();
                    int time = reader.GetInt();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid3);
                        await UpdateTime(Config.DatabaseConnectionKey, userid3, role, time);
                    });
                    break;
                case DataType.WarheadActivate:
                    string userid4 = reader.GetString();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid4);
                        await UpdateWarhead(Config.DatabaseConnectionKey, userid4, 1);
                    });
                    break;
                case DataType.WarheadStart:
                    string userid5 = reader.GetString();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid5);
                        await UpdateWarhead(Config.DatabaseConnectionKey, userid5, 0);
                    });
                    break;
                case DataType.BasicData:
                    string userid6 = reader.GetString();
                    int type = reader.GetInt();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid6);
                        await UpdateDied(Config.DatabaseConnectionKey, userid6, type);
                    });
                    break;
                case DataType.ItemUsed:
                    string userid7 = reader.GetString();
                    int type2 = reader.GetInt();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid7);
                        await UpdateMedical(Config.DatabaseConnectionKey, userid7, type2);
                    });
                    break;
                case DataType.PlayedRound:
                    var useris = reader.GetStringArray();
                    foreach(var user in useris)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await Check(Config.DatabaseConnectionKey, false, user);
                            await UpdateRoundsPlayed(Config.DatabaseConnectionKey, user);
                        });
                    }
                    break;
                case DataType.RoundWon:
                    var useris2 = reader.GetStringArray();
                    foreach (var user in useris2)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await Check(Config.DatabaseConnectionKey, false, user);
                            await UpdateRoundsWon(Config.DatabaseConnectionKey, user);
                        });
                    }
                    break;
                case DataType.IntercomTime:
                    var userid8 = reader.GetString();
                    int time2 = reader.GetInt();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid8);
                        await UpdateIntercomTime(Config.DatabaseConnectionKey, userid8, time2);
                    });
                    break;
                case DataType.ShotsFired:
                    var userid9 = reader.GetString();
                    int shoots = reader.GetInt();
                    int shootshead = reader.GetInt();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid9);
                        await UpdateShoots(Config.DatabaseConnectionKey, userid9, shoots, shootshead);
                    });
                    break;
                case DataType.BestEscape:
                    var userid10 = reader.GetString();
                    int type22 = reader.GetInt();
                    int time3 = reader.GetInt();
                    Task.Factory.StartNew(async () =>
                    {
                        await Check(Config.DatabaseConnectionKey, false, userid10);
                        await UpdateEscapeTime(Config.DatabaseConnectionKey, userid10, type22, time3);
                    });
                    break;
                case DataType.RespawnedAs:
                    var useris3 = reader.GetStringArray();
                    int type4 = reader.GetInt();
                    foreach (var user in useris3)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await Check(Config.DatabaseConnectionKey, false, user);
                            await UpdateRespawnTeam(Config.DatabaseConnectionKey, user, type4);
                        });
                    }
                    break;
            }
        }

        public async Task Check(string con, bool isdnt, string UserID)
        {
            try
            {
                using (var dbcon = new MySqlConnection(con))
                {
                    await dbcon.OpenAsync();
                    int c = 0;
                    using (var cmd = new MySqlCommand("SELECT userid FROM `stats` WHERE userid = @a", dbcon))
                    {
                        cmd.Parameters.AddWithValue("@a", UserID);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                c++;
                            }
                        }
                    }
                    if (c == 0)
                    {
                        if (!isdnt)
                        {
                            using (var cmd = new MySqlCommand("INSERT IGNORE INTO `stats` (userid) VALUES (@a)", dbcon))
                            {
                                cmd.Parameters.AddWithValue("@a", UserID);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        if (isdnt)
                        {
                            using (var cmd = new MySqlCommand("DELETE FROM `stats` WHERE userid = @a", dbcon))
                            {
                                cmd.Parameters.AddWithValue("@a", UserID);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    await dbcon.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }
        public async Task UpdateStatsGrenade(string con, string UserID, string fieldName)
        {

            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET " + fieldName + " = " + fieldName + " + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register " + fieldName + " for " + UserID);
        }

        public async Task UpdateDied(string con, string UserID, int type)
        {
            string fieldName = "";
            switch (type)
            {
                case 0:
                    fieldName = "died_by_nuke";
                    break;
                case 1:
                    fieldName = "died_by_decon";
                    break;
                case 2:
                    fieldName = "died_by_gravity";
                    break;
                case 3:
                    fieldName = "died_by_pdim";
                    break;
                case 4:
                    fieldName = "scp939_bites";
                    break;
                case 5:
                    fieldName = "escapes_from_pocket";
                    break;
                case 6:
                    fieldName = "scp049_zombies";
                    break;
                case 7:
                    fieldName = "scp096_triggered";
                    break;
                case 8:
                    fieldName = "escaped_as_classd";
                    break;
                case 9:
                    fieldName = "escaped_as_scientist";
                    break;
            }

            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET " + fieldName + " = " + fieldName + " + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register " + fieldName + " for " + UserID);
        }

        public async Task UpdateRespawnTeam(string con, string UserID, int type)
        {
            string fieldName = "";
            switch (type)
            {
                case 0:
                    fieldName = "respawned_as_chaos";
                    break;
                case 1:
                    fieldName = "respawned_as_ntf";
                    break;
            }

            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET " + fieldName + " = " + fieldName + " + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register " + fieldName + " for " + UserID);
        }

        public async Task UpdateRoundsPlayed(string con, string UserID)
        {
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET rounds_played = rounds_played + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register rounds_played for " + UserID);
        }

        public async Task UpdateIntercomTime(string con, string UserID, int time)
        {
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET intercom_time = intercom_time + @b WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    cmd.Parameters.AddWithValue("@b", time);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register intercom_time for " + UserID);
        }

        public async Task UpdateEscapeTime(string con, string UserID, int type, int time)
        {
            string fieldName = "";
            switch (type)
            {
                case 0:
                    fieldName = "fastest_escape_as_classd";
                    break;
                case 1:
                    fieldName = "fastest_escape_as_scientist";
                    break;
            }

            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET " + fieldName + " = " + fieldName + " + @b WHERE userid = @a AND " + fieldName + " < @b", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    cmd.Parameters.AddWithValue("@b", time);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register " + fieldName + " for " + UserID);
        }

        public async Task UpdateShoots(string con, string UserID, int shoots, int shootshead)
        {
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET shots_fired = shots_fired + @b WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    cmd.Parameters.AddWithValue("@b", shoots);
                    await cmd.ExecuteNonQueryAsync();
                }
                using (var cmd = new MySqlCommand("UPDATE `stats` SET head_shots = head_shots + @b WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    cmd.Parameters.AddWithValue("@b", shootshead);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register intercom_time for " + UserID);
        }

        public async Task UpdateRoundsWon(string con, string UserID)
        {
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET rounds_won = rounds_won + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register rounds_won for " + UserID);
        }

        public async Task UpdateMedical(string con, string UserID, int type)
        {
            string fieldName = "";
            switch (type)
            {
                case 0:
                    fieldName = "adrenaline_used";
                    break;
                case 1:
                    fieldName = "medkit_used";
                    break;
                case 2:
                    fieldName = "painkillers_used";
                    break;
                case 3:
                    fieldName = "scp500_used";
                    break;
            }

            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET " + fieldName + " = " + fieldName + " + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register " + fieldName + " for " + UserID);
        }

        public async Task UpdateWarhead(string con, string UserID, int type)
        {
            string fieldName = "";
            switch (type)
            {
                case 0:
                    fieldName = "started_nuke";
                    break;
                case 1:
                    fieldName = "armed_nuke";
                    break;
            }
            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET " + fieldName + " = " + fieldName + " + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register " + fieldName + " for " + UserID);
        }

        public async Task UpdateDamage(string con, string UserID, int deal, int received)
        {
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET damage_received = damage_receivd + @b WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    cmd.Parameters.AddWithValue("@b", received);
                    await cmd.ExecuteNonQueryAsync();
                }
                using (var cmd = new MySqlCommand("UPDATE `stats` SET damage_done = damage_done + @b WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    cmd.Parameters.AddWithValue("@b", deal);
                    await cmd.ExecuteNonQueryAsync();
                }
                await dbcon.CloseAsync();
            }

            Logger.Info("Register damage for " + UserID);
        }

        public async Task UpdateStats(string con, string UserID, Roletype role, bool isDeath = false, bool isKilled = false)
        {
            string fieldName = "";
            string isDeaths = isDeath ? "deaths_" : "kills_";
            isDeaths = isKilled ? "killed_" : isDeaths;
            switch (role)
            {
                case Roletype.ChaosMarauder:
                case Roletype.ChaosRepressor:
                case Roletype.ChaosConscript:
                case Roletype.ChaosRifleman:
                    fieldName = isDeaths + "chaos";
                    break;
                case Roletype.ClassD:
                    fieldName = isDeaths + "classd";
                    break;
                case Roletype.FacilityGuard:
                    fieldName = isDeaths + "guard";
                    break;
                case Roletype.NtfPrivate:
                    fieldName = isDeaths + "cadet";
                    break;
                case Roletype.NtfCaptain:
                    fieldName = isDeaths + "commander";
                    break;
                case Roletype.NtfSergeant:
                    fieldName = isDeaths + "lieutenant";
                    break;
                case Roletype.NtfSpecialist:
                    fieldName = isDeaths + "ntfscientist";
                    break;
                case Roletype.Scientist:
                    fieldName = isDeaths + "scientist";
                    break;
                case Roletype.Scp049:
                    fieldName = isDeaths + "049";
                    break;
                case Roletype.Scp0492:
                    fieldName = isDeaths + "0492";
                    break;
                case Roletype.Scp079:
                    fieldName = isDeaths + "079";
                    break;
                case Roletype.Scp096:
                    fieldName = isDeaths + "096";
                    break;
                case Roletype.Scp106:
                    fieldName = isDeaths + "106";
                    break;
                case Roletype.Scp173:
                    fieldName = isDeaths + "173";
                    break;
                case Roletype.Scp93953:
                case Roletype.Scp93989:
                    fieldName = isDeaths + "939";
                    break;
            }
            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand("UPDATE `stats` SET " + fieldName + " = " + fieldName + " + 1 WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    await cmd.ExecuteNonQueryAsync();
                }
                if (!isKilled)
                {
                    using (var cmd = new MySqlCommand("UPDATE `stats` SET " + (isDeath ? "deaths" : "kills") + " = " + (isDeath ? "deaths" : "kills") + " + 1 WHERE userid = @a", dbcon))
                    {
                        cmd.Parameters.AddWithValue("@a", UserID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await dbcon.CloseAsync();
            }
            Logger.Info("Register " + fieldName + " for " + UserID);

        }
        public async Task UpdateTime(string con, string UserID, Roletype role, int time)
        {
            string fieldName = "";
            switch (role)
            {
                case Roletype.ChaosMarauder:
                case Roletype.ChaosRepressor:
                case Roletype.ChaosConscript:
                case Roletype.ChaosRifleman:
                    fieldName = "chaos";
                    break;
                case Roletype.ClassD:
                    fieldName = "classd";
                    break;
                case Roletype.FacilityGuard:
                    fieldName = "guard";
                    break;
                case Roletype.NtfPrivate:
                    fieldName = "cadet";
                    break;
                case Roletype.NtfCaptain:
                    fieldName = "commander";
                    break;
                case Roletype.NtfSergeant:
                    fieldName = "lieutenant";
                    break;
                case Roletype.NtfSpecialist:
                    fieldName = "ntfscientist";
                    break;
                case Roletype.Scientist:
                    fieldName = "scientist";
                    break;
                case Roletype.Scp049:
                    fieldName = "049";
                    break;
                case Roletype.Scp0492:
                    fieldName =  "0492";
                    break;
                case Roletype.Scp079:
                    fieldName =  "079";
                    break;
                case Roletype.Scp096:
                    fieldName = "096";
                    break;
                case Roletype.Scp106:
                    fieldName =  "106";
                    break;
                case Roletype.Scp173:
                    fieldName =  "173";
                    break;
                case Roletype.Spectator:
                    fieldName = "spectator";
                    break;
                case Roletype.Scp93953:
                case Roletype.Scp93989:
                    fieldName = "939";
                    break;
                case Roletype.Tutorial:
                    fieldName = "tutorial";
                    break;
            }
            if (string.IsNullOrEmpty(fieldName))
                return;
            using (var dbcon = new MySqlConnection(con))
            {
                await dbcon.OpenAsync();
                using (var cmd = new MySqlCommand($"UPDATE `stats` SET time_as_{fieldName} = time_as_{fieldName} + @b WHERE userid = @a", dbcon))
                {
                    cmd.Parameters.AddWithValue("@a", UserID);
                    cmd.Parameters.AddWithValue("@b", time);
                    await cmd.ExecuteNonQueryAsync();
                }

                await dbcon.CloseAsync();
            }
            Logger.Info($"Register time_as_{fieldName} for " + UserID);

        }
    }
}
