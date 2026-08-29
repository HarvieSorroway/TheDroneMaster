using DMPS.PlayerHooks;
using TheDroneMaster.DMPS.DMPShud.EnergyBar;
using TheDroneMaster.DMPS.DMPSutils;

namespace TheDroneMaster.DMPS.PlayerHooks.BioReactors
{
    internal class ThunderBoltReactor : DMPSBioReactor
    {
        private ShockWaveObject shockWave;
        private int shockWaveChargeFrames, coolDownFrames;
        private bool shockWaveCastLocked;

        public static class Config
        {
            public const int ShockWaveEnergyRequired = 4;
            public const int ShockWaveChargeDurationFrames = 40;
            public const float ShockWaveRadiaus = 300f;
            public const int CoolDown = 120;
        }

        ThunderBoltMessage ThunderBoltMsg => message as ThunderBoltMessage;
        public ThunderBoltReactor(Player player, DMPSModule module) : base(player, module)
        {
        }

        public override void SetMessage(DMPSModule module)
        {
            module.energyBarMessage = message = new ThunderBoltMessage();
        }

        public override void Update(Player player)
        {
            base.Update(player);

            if (shockWave is not null && (shockWave.slatedForDeletetion || shockWave.room != player.room))
            {
                shockWave = null;
            }


            if (coolDownFrames > 0)
            {
                coolDownFrames--;
            }
            ThunderBoltMsg.chargeReady = IsPlayerReady(player);

            if (player.input[0].spec && IsPlayerReady(player) && coolDownFrames == 0)
            {
                if(shockWaveChargeFrames < Config.ShockWaveChargeDurationFrames)
                    shockWaveChargeFrames++;

                if (shockWaveChargeFrames == Config.ShockWaveChargeDurationFrames)
                {
                    shockWaveChargeFrames = 0;
                    if (TrySpendEnergy(Config.ShockWaveEnergyRequired))
                    {
                        var shockWave = new ShockWaveObject(player, Config.ShockWaveRadiaus);
                        player.room.AddObject(shockWave);

                        ThunderBoltMsg.releaseThisFrame = true;
                        coolDownFrames = Config.CoolDown;
                    }
                }
            }
            else if(shockWaveChargeFrames > 0)
            {
                shockWaveChargeFrames--;
            }

            ThunderBoltMsg.chargeProgression = shockWaveChargeFrames / (float)Config.ShockWaveChargeDurationFrames;
        }

        private bool IsPlayerReady(Player player)
        {
            bool animationReady =
                (player.bodyMode == Player.BodyModeIndex.Stand && player.canJump > 0) ||
                player.bodyMode == Player.BodyModeIndex.Swimming ||
                player.bodyMode == Player.BodyModeIndex.ZeroG ||
                (player.bodyMode == Player.BodyModeIndex.Default && player.canJump > 0);

            return player.room is not null &&
                player.Consious &&
                !player.Stunned &&
                animationReady &&
                reactorEnergy > Config.ShockWaveEnergyRequired;
        }
    }

    public class ThunderBoltMessage : EnergyBarMessage
    {
        public float chargeProgression;
        public bool releaseThisFrame;
        public bool chargeReady;
    }
}
