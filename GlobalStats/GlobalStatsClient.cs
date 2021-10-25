using Exiled.API.Enums;
using Exiled.API.Features;
using GlobalStats.Enums;
using LiteNetLib.Utils;
using MEC;
using NetworkedPlugins.API;
using Respawning;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GlobalStats
{
    public class GlobalStatsClient : NPAddonClient<ClientConfig>
    {
        public override string AddonAuthor { get; } = "Killers0992";
        public override string AddonId { get; } = "xZQETjq36QYaJC6E";
        public override string AddonName { get; } = "GlobalStats";
        public override Version AddonVersion { get; } = new Version(1, 0, 0);

        Dictionary<string, int> damageReceived { get; set; } = new Dictionary<string, int>();
        Dictionary<string, int> damageDeal { get; set; } = new Dictionary<string, int>();
        Dictionary<string, int> shootsFired { get; set; } = new Dictionary<string, int>();
        Dictionary<string, int> shootsFiredhead { get; set; } = new Dictionary<string, int>();
        Dictionary<string, Dictionary<RoleType, int>> timePlayed { get; set; } = new Dictionary<string, Dictionary<RoleType, int>>();
        GameObject lastSpeaker = null;
        int speakingTime = 0;

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Player.Died += Player_Died;
            Exiled.Events.Handlers.Server.RestartingRound += Server_RestartingRound;
            Exiled.Events.Handlers.Player.Hurting += Player_Hurting;
            Exiled.Events.Handlers.Player.Destroying += Player_Destroying;
            Exiled.Events.Handlers.Player.ActivatingWarheadPanel += Player_ActivatingWarheadPanel;
            Exiled.Events.Handlers.Warhead.Starting += Warhead_Starting;
            Exiled.Events.Handlers.Player.ItemUsed += Player_ItemUsed;
            Exiled.Events.Handlers.Server.RoundEnded += Server_RoundEnded;
            Exiled.Events.Handlers.Player.Shot += Player_Shot;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += Player_EscapingPocketDimension;
            Exiled.Events.Handlers.Scp049.FinishingRecall += Scp049_FinishingRecall;
            Exiled.Events.Handlers.Scp096.Enraging += Scp096_Enraging;
            Exiled.Events.Handlers.Player.Escaping += Player_Escaping;
            Exiled.Events.Handlers.Server.RespawningTeam += Server_RespawningTeam;
            Exiled.Events.Handlers.Player.ThrowingItem += Player_ThrowingItem;
            Exiled.Events.Handlers.Player.Verified += Player_Verified;
            Logger.Info("Addon started, register eventhandlers.");
            Timing.RunCoroutine(TimeCollector());
        }

        private void Player_Verified(Exiled.Events.EventArgs.VerifiedEventArgs ev)
        {
            if (!damageReceived.ContainsKey(ev.Player.UserId))
                damageReceived.Add(ev.Player.UserId, 0);
            if (!damageDeal.ContainsKey(ev.Player.UserId))
                damageDeal.Add(ev.Player.UserId, 0);
            if (!shootsFired.ContainsKey(ev.Player.UserId))
                shootsFired.Add(ev.Player.UserId, 0);
            if (!shootsFiredhead.ContainsKey(ev.Player.UserId))
                shootsFiredhead.Add(ev.Player.UserId, 0);
        }

        private void Player_ThrowingItem(Exiled.Events.EventArgs.ThrowingItemEventArgs ev)
        {
            if (ev.Item.Type != ItemType.GrenadeFlash &&
                ev.Item.Type != ItemType.GrenadeHE &&
                ev.Item.Type != ItemType.SCP018)
                return;

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.ThrowItem);
            switch (ev.Item.Type)
            {
                case ItemType.GrenadeFlash:
                    dataWriter.Put((byte)ThrowedItem.Flash);
                    break;
                case ItemType.GrenadeHE:
                    dataWriter.Put((byte)ThrowedItem.Grenade);
                    break;
                case ItemType.SCP018:
                    dataWriter.Put((byte)ThrowedItem.SCP018);
                    break;
            }
            dataWriter.Put(ev.Player.UserId);
            SendData(dataWriter);
        }

        private void Player_ItemUsed(Exiled.Events.EventArgs.UsedItemEventArgs ev)
        {
            if (ev.Item.Type != ItemType.Adrenaline &&
                ev.Item.Type != ItemType.Medkit &&
                ev.Item.Type != ItemType.Painkillers &&
                ev.Item.Type != ItemType.SCP500)
                return;

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.ItemUsed);
            dataWriter.Put(ev.Player.UserId);
            switch (ev.Item.Type)
            {
                case ItemType.Adrenaline:
                    dataWriter.Put((byte)UsedItem.Adrenaline);
                    break;
                case ItemType.Medkit:
                    dataWriter.Put((byte)UsedItem.Medkit);
                    break;
                case ItemType.Painkillers:
                    dataWriter.Put((byte)UsedItem.Painkillers);
                    break;
                case ItemType.SCP500:
                    dataWriter.Put((byte)UsedItem.SCP500);
                    break;
            }
            SendData(dataWriter);
        }

        private void Server_RespawningTeam(Exiled.Events.EventArgs.RespawningTeamEventArgs ev)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.RespawnedAs);
            dataWriter.PutArray(ev.Players.Select(p => p.UserId).ToArray());
            switch (ev.NextKnownTeam)
            {
                case SpawnableTeamType.ChaosInsurgency:
                    dataWriter.Put((byte)RespawnType.ChaosInsurgency);
                    break;
                case SpawnableTeamType.NineTailedFox:
                    dataWriter.Put((byte)RespawnType.NineTailedFox);
                    break;
            }
            SendData(dataWriter);
        }

        private void Player_Escaping(Exiled.Events.EventArgs.EscapingEventArgs ev)
        {
            if (!ev.IsAllowed) return;

            if (ev.NewRole != RoleType.ChaosConscript &&
                ev.NewRole != RoleType.NtfPrivate)
                return;

            NetDataWriter dataWriter = new NetDataWriter();
            NetDataWriter dataWriter2 = new NetDataWriter();
            dataWriter.Put((byte)DataType.BasicData);
            dataWriter2.Put((byte)DataType.BestEscape);
            dataWriter.Put(ev.Player.UserId);
            dataWriter2.Put(ev.Player.UserId);

            switch (ev.NewRole)
            {
                case RoleType.ChaosConscript:
                    dataWriter.Put((byte)BasicData.EscapeAsClassD);
                    dataWriter2.Put((byte)EscapeType.ClassD);
                    dataWriter2.Put(Round.ElapsedTime.TotalSeconds);
                    break;
                case RoleType.NtfSpecialist:
                    dataWriter.Put((byte)BasicData.EscapeAsScientist);
                    dataWriter2.Put((byte)EscapeType.Scientist);
                    dataWriter2.Put(Round.ElapsedTime.TotalSeconds);
                    break;
            }
            SendData(dataWriter);
            SendData(dataWriter2);
        }

        private void Scp096_Enraging(Exiled.Events.EventArgs.EnragingEventArgs ev)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.BasicData);
            dataWriter.Put(ev.Player.UserId);
            dataWriter.Put((byte)BasicData.Scp096Enrage);
            SendData(dataWriter);
        }

        private void Scp049_FinishingRecall(Exiled.Events.EventArgs.FinishingRecallEventArgs ev)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.BasicData);
            dataWriter.Put(ev.Scp049.UserId);
            dataWriter.Put((byte)BasicData.Scp049Recalls);
            SendData(dataWriter);
        }

        private void Player_EscapingPocketDimension(Exiled.Events.EventArgs.EscapingPocketDimensionEventArgs ev)
        {
            if (!ev.IsAllowed) return;

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.BasicData);
            dataWriter.Put(ev.Player.UserId);
            dataWriter.Put((byte)BasicData.EscapeFromPD);
            SendData(dataWriter);
        }

        private void Player_Shot(Exiled.Events.EventArgs.ShotEventArgs ev)
        {
            if (!shootsFired.ContainsKey(ev.Shooter.UserId))
                shootsFired.Add(ev.Shooter.UserId, 0);
            shootsFired[ev.Shooter.UserId]++;

            if (ev.Hitbox._dmgMultiplier == HitboxIdentity.DamagePercent.Headshot)
            {
                if (!shootsFiredhead.ContainsKey(ev.Shooter.UserId))
                    shootsFiredhead.Add(ev.Shooter.UserId, 0);
                shootsFiredhead[ev.Shooter.UserId]++;
            }
        }

        private void Server_RoundEnded(Exiled.Events.EventArgs.RoundEndedEventArgs ev)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.PlayedRound);
            dataWriter.PutArray(Player.List.Select(p => p.UserId).ToArray());
            SendData(dataWriter);

            dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.RoundWon);
            List<string> players = new List<string>();
            foreach (var plr in Player.List)
            {
                switch (ev.LeadingTeam)
                {
                    case LeadingTeam.Anomalies:
                        if (plr.Team == Team.SCP)
                            players.Add(plr.UserId);
                        break;
                    case LeadingTeam.ChaosInsurgency:
                        if (plr.Team == Team.CHI || plr.Team == Team.CDP)
                            players.Add(plr.UserId);
                        break;
                    case LeadingTeam.Draw:
                    case LeadingTeam.FacilityForces:
                        if (plr.Team == Team.MTF || plr.Team == Team.RSC)
                            players.Add(plr.UserId);
                        break;
                }
            }
            if (players.Count == 0) return;

            dataWriter.PutArray(players.ToArray());
            SendData(dataWriter);
        }

        private void Warhead_Starting(Exiled.Events.EventArgs.StartingEventArgs ev)
        {
            if (ev.Player == null) return;

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.WarheadStart);
            dataWriter.Put(ev.Player.UserId);
            SendData(dataWriter);
        }

        private void Player_ActivatingWarheadPanel(Exiled.Events.EventArgs.ActivatingWarheadPanelEventArgs ev)
        {
            if (!ev.IsAllowed) return;

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.WarheadActivate);
            dataWriter.Put(ev.Player.UserId);
            SendData(dataWriter);
        }

        public IEnumerator<float> TimeCollector()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1f);
                try
                {
                    foreach (var plr in Player.List)
                    {
                        if (!timePlayed.ContainsKey(plr.UserId))
                            timePlayed.Add(plr.UserId, new Dictionary<RoleType, int>());
                        if (!timePlayed[plr.UserId].ContainsKey(plr.Role))
                            timePlayed[plr.UserId].Add(plr.Role, 0);
                        timePlayed[plr.UserId][plr.Role]++;
                    }
                    if (Intercom.host.Networkspeaker != null)
                    {
                        lastSpeaker = Intercom.host.Networkspeaker;
                        speakingTime++;
                    }
                    else
                    {
                        if (lastSpeaker != null)
                        {
                            var hub = ReferenceHub.GetHub(lastSpeaker);
                            if (hub != null)
                            {
                                NetDataWriter dataWriter = new NetDataWriter();
                                dataWriter.Put((byte)DataType.IntercomTime);
                                dataWriter.Put(hub.characterClassManager.UserId);
                                dataWriter.Put(speakingTime);
                                SendData(dataWriter);
                            }
                            speakingTime = 0;
                            lastSpeaker = null;
                        }
                    }
                }
                catch (Exception) {}
            }
        }

        private void Player_Destroying(Exiled.Events.EventArgs.DestroyingEventArgs ev)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            foreach (var role in timePlayed[ev.Player.UserId])
            {
                dataWriter.Put((byte)DataType.TimePlayed);
                dataWriter.Put(ev.Player.UserId);
                dataWriter.Put((int)role.Key);
                dataWriter.Put(role.Value);
                SendData(dataWriter);
            }
            timePlayed.Remove(ev.Player.UserId);

            dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.DamageInfo);
            dataWriter.Put(ev.Player.UserId);
            dataWriter.Put(damageDeal[ev.Player.UserId]);
            dataWriter.Put(damageReceived[ev.Player.UserId]);
            SendData(dataWriter);

            dataWriter = new NetDataWriter();
            dataWriter.Put((byte)DataType.ShotsFired);
            dataWriter.Put(ev.Player.UserId);
            dataWriter.Put(shootsFired[ev.Player.UserId]);
            dataWriter.Put(shootsFiredhead[ev.Player.UserId]);
            SendData(dataWriter);
        }


        private void Player_Hurting(Exiled.Events.EventArgs.HurtingEventArgs ev)
        {
            damageReceived[ev.Target.UserId] += (int)ev.Amount;

            if (ev.Attacker == ev.Target)
                return;

            damageDeal[ev.Attacker.UserId] += (int)ev.Amount;
        }

        private void Server_RestartingRound()
        {
            damageReceived.Clear();
            damageDeal.Clear();
            shootsFired.Clear();
            shootsFiredhead.Clear();
            timePlayed.Clear();
        }

        private void Player_Died(Exiled.Events.EventArgs.DiedEventArgs ev)
        {
            NetDataWriter wr = new NetDataWriter();
            wr.Put((byte)DataType.PlayerDeath);
            wr.Put(ev.Target.UserId);
            wr.Put((sbyte)ev.Target.Role);
            wr.Put(ev.Target.DoNotTrack);
            wr.Put(ev.Killer.UserId);
            wr.Put((sbyte)ev.Killer.Role);
            wr.Put(ev.Killer.DoNotTrack);
            SendData(wr);
            if (ev.HitInformations.Tool == DamageTypes.Nuke)
            {
                wr = new NetDataWriter();
                wr.Put((byte)DataType.BasicData);
                wr.Put(ev.Target.UserId);
                wr.Put((byte)BasicData.NukeDeath);
                SendData(wr);
            }
            else if (ev.HitInformations.Tool == DamageTypes.Decont)
            {
                wr = new NetDataWriter();
                wr.Put((byte)DataType.BasicData);
                wr.Put(ev.Target.UserId);
                wr.Put((byte)BasicData.DecontaminationDeath);
                SendData(wr);
            } 
            else if (ev.HitInformations.Tool == DamageTypes.Falldown)
            {
                wr = new NetDataWriter();
                wr.Put((byte)DataType.BasicData);
                wr.Put(ev.Target.UserId);
                wr.Put((byte)BasicData.FalldownDeath);
                SendData(wr);
            } 
            else if (ev.HitInformations.Tool == DamageTypes.Pocket)
            {
                wr = new NetDataWriter();
                wr.Put((byte)DataType.BasicData);
                wr.Put(ev.Target.UserId);
                wr.Put((byte)BasicData.PocketDeath);
                SendData(wr);
            } 
            else if (ev.HitInformations.Tool == DamageTypes.Scp939)
            {
                wr = new NetDataWriter();
                wr.Put((byte)DataType.BasicData);
                wr.Put(ev.Target.UserId);
                wr.Put((byte)BasicData.Scp939Death);
                SendData(wr);
            }
        }
    }
}
