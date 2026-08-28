using DMPS.PlayerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSutils;

namespace TheDroneMaster.DMPS.PlayerHooks.BioReactors
{
    internal class ThunderBoltReactor : DMPSBioReactor
    {
        ShockWaveObject shockWave;
        public static class Config
        {
            public const int ShockWaveEnergyRequired = 5, ShockWaveSpawnDelay = 5;
            public const float ShockWaveRadiaus = 300f;
            public const bool ShockWaveShowDebugHint = true;
        }
        public ThunderBoltReactor(Player player, DMPSModule module) : base(player, module)
        {
        }
        public override void Update(Player player)
        {
            base.Update(player);

            if (shockWave is not null && (shockWave.slatedForDeletetion || shockWave.room != player.room))
            {
                shockWave = null;
            }
            if (shockWave is null && ShockWaveObject.IsPlayerReady(player, this) && player.room is not null)
            {
                bool flag = !player.input[Config.ShockWaveSpawnDelay].spec;
                for (int i = 0; i < Config.ShockWaveSpawnDelay; i++)
                    flag &= player.input[i].spec;
                if (flag)
                {
                    shockWave = new ShockWaveObject(player, Config.ShockWaveRadiaus, Config.ShockWaveShowDebugHint, this);
                    player.room.AddObject(shockWave);
                }
            }
            
        }
    }
}
