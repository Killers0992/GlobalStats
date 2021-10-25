using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalStats.Enums
{
    public enum DataType : byte
    {
        PlayerDeath,
        ThrowItem,
        DamageInfo,
        TimePlayed,
        WarheadActivate,
        WarheadStart,
        BasicData,
        ItemUsed,
        PlayedRound,
        RoundWon,
        IntercomTime,
        ShotsFired,
        BestEscape,
        RespawnedAs,
    }
}
