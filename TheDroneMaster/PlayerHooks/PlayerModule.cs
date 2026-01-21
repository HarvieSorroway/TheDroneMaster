using CustomDreamTx;
using SlugBase.DataTypes;
using SlugBase.Features;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DreamComponent.DreamHook;
using UnityEngine;

namespace TheDroneMaster
{
    public class DroneMasterModule : PlayerModule
    {
        public readonly PlayerExtraMovement playerExtraMovement;

        #region WorldState
        public EnemyCreator enemyCreator;
        #endregion

        //graphics
        public MetalGills metalGills;
        public DronePortGraphics portGraphics;

        public int grillIndex = -1;
        public int portIndex = -1;

        public DronePort port;
        public DroneMasterModule(Player player) : base(player)
        {
            port = AddUtil(new DronePort(player));

            if (Plugin.instance.config.moreEnemies.Value && isStoryGamePlayer && !Plugin.SkinOnly)
                enemyCreator = AddUtil(new EnemyCreator(this));

            playerExtraMovement = AddUtil(new PlayerExtraMovement());
        }

        public override void SyncAcceptableDamage(int val)
        {
            metalGills.acceptableDamage = val;
        }

        public override void InitExtraGraphics(PlayerGraphics playerGraphics)
        {
            metalGills = new MetalGills(playerGraphics, grillIndex);
            portGraphics = new DronePortGraphics(playerGraphics, portIndex);
        }

        public override void ExtraGraphicsInitSprites(PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            graphicsInited = true;

            newEyeIndex = sLeaser.sprites.Length;
            portIndex = newEyeIndex + 1;
            grillIndex = portIndex + portGraphics.totalSprites;

            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1 + metalGills.totalSprites + portGraphics.totalSprites);

            sLeaser.sprites[newEyeIndex] = new FSprite("FaceA0", true);
            metalGills.startSprite = grillIndex;
            metalGills.InitiateSprites(sLeaser, rCam);
            portGraphics.startIndex = portIndex;
            portGraphics.InitSprites(sLeaser, rCam);
        }

        public override void ExtraGraphicsAddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            FContainer container = rCam.ReturnFContainer("Midground");

            container.AddChild(sLeaser.sprites[newEyeIndex]);
            sLeaser.sprites[newEyeIndex].MoveInFrontOfOtherNode(sLeaser.sprites[9]);

            metalGills.AddToContainer(sLeaser, rCam, container);
            portGraphics.AddToContainer(sLeaser, rCam, container);
        }

        public override void ExtraGraphicsApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            metalGills.SetGillColors(bodyColor, laserColor, eyeColor);
            metalGills.ApplyPalette(sLeaser, rCam, palette);

            portGraphics.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void ExtraGraphicsUpdate(PlayerGraphics self)
        {
            metalGills.Update();
            portGraphics.Update();
        }

        public override void ExtraGraphicsDrawSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Color color = eyeColor;
            if (PlayerDroneHUD.instance != null)
                color = Color.Lerp(eyeColor, laserColor, PlayerDroneHUD.instance.alpha);

            sLeaser.sprites[newEyeIndex].color = color;

            sLeaser.sprites[newEyeIndex].element = sLeaser.sprites[9].element;
            sLeaser.sprites[newEyeIndex].SetPosition(sLeaser.sprites[9].x, sLeaser.sprites[9].y);
            sLeaser.sprites[newEyeIndex].scaleX = sLeaser.sprites[9].scaleX;
            sLeaser.sprites[newEyeIndex].scaleY = sLeaser.sprites[9].scaleY;
            sLeaser.sprites[newEyeIndex].rotation = sLeaser.sprites[9].rotation;
            sLeaser.sprites[newEyeIndex].isVisible = sLeaser.sprites[9].isVisible;

            metalGills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            portGraphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }
    }

    public class PlayerModule
    {
        public readonly WeakReference<Player> playerRef;

        public readonly SlugBaseCharacter character;

        //public static SlugcatStats.Name DroneMasterName { get; private set; }

        //public readonly bool ownDrones;
        public readonly bool usingDefaultCol = false;
        public readonly bool isStoryGamePlayer;

        public readonly Color eyeColor;
        public readonly Color bodyColor;
        public readonly Color laserColor;


        #region Graphics
        public bool graphicsInited = false;
        public int newEyeIndex = -1;
        #endregion

        #region PlayeState
        public PlayerDeathPreventer playerDeathPreventer;
        public bool lockMovementInput = false;
        public bool FullCharge => playerRef.TryGetTarget(out var player) && player.playerState.foodInStomach == player.MaxFoodInStomach;
        #endregion

        public DreamStateOverride stateOverride;


        public readonly List<PlayerModuleUtil> utils = new List<PlayerModuleUtil>();

        public PlayerModule(Player player)
        {
            //ownDrones = Plugin.OwnLaserDrone.TryGet(player, out bool ownLaserDrone) && ownLaserDrone;
            playerRef = new WeakReference<Player>(player);
            isStoryGamePlayer = player.room.game.session is StoryGameSession;
            var name = player.slugcatStats.name;

            //Plugin.Log(DeathPersistentSaveDataPatch.GetUnitOfHeader(EnemyCreator.header).ToString());

            if (SlugBaseCharacter.TryGet(name, out character))
            {
                ColorSlot[] array;
                bool flag4 = character.Features.TryGet<ColorSlot[]>(PlayerFeatures.CustomColors, out array);
                if (flag4)
                {
                    Plugin.Log(array.Length.ToString());

                    if (array.Length > 0)
                    {
                        bodyColor = array[0].GetColor(player.playerState.playerNumber);
                        Plugin.Log("eyecolor : " + ColorUtility.ToHtmlStringRGB(bodyColor));

                    }
                    if (array.Length > 1)
                    {
                        eyeColor = array[1].GetColor(player.playerState.playerNumber);
                        Plugin.Log("bodyColor : " + ColorUtility.ToHtmlStringRGB(eyeColor));
                    }
                    if (array.Length > 2)
                    {
                        laserColor = array[2].GetColor(player.playerState.playerNumber);
                        Plugin.Log("laserColor : " + ColorUtility.ToHtmlStringRGB(laserColor));
                    }
                }
                if (PlayerGraphics.customColors != null && !player.IsJollyPlayer)
                {
                    if (PlayerGraphics.customColors.Count > 0)
                    {
                        bodyColor = PlayerGraphics.CustomColorSafety(0);
                        Plugin.Log("Custom-eyecolor : " + ColorUtility.ToHtmlStringRGB(bodyColor));
                    }
                    if (PlayerGraphics.customColors.Count > 1)
                    {
                        eyeColor = PlayerGraphics.CustomColorSafety(1);
                        Plugin.Log("Custom-bodyColor : " + ColorUtility.ToHtmlStringRGB(eyeColor));
                    }
                    if (PlayerGraphics.customColors.Count > 2)
                    {
                        laserColor = PlayerGraphics.CustomColorSafety(2);
                        Plugin.Log("Custom-laserColor : " + ColorUtility.ToHtmlStringRGB(laserColor));
                    }
                }
                if (PlayerGraphics.jollyColors != null && player.IsJollyPlayer)
                {
                    bodyColor = PlayerGraphics.JollyColor(player.playerState.playerNumber, 0);
                    Plugin.Log("Jolly-eyecolor : " + ColorUtility.ToHtmlStringRGB(bodyColor));

                    eyeColor = PlayerGraphics.JollyColor(player.playerState.playerNumber, 1);
                    Plugin.Log("Jolly-bodyColor : " + ColorUtility.ToHtmlStringRGB(eyeColor));

                    laserColor = PlayerGraphics.JollyColor(player.playerState.playerNumber, 2);
                    Plugin.Log("Jolly-laserColor : " + ColorUtility.ToHtmlStringRGB(laserColor));
                }

                SetUpOverrides(player);

                if (!Plugin.instance.config.canBackSpear.Value)
                    player.spearOnBack = null;

                playerDeathPreventer = AddUtil(new PlayerDeathPreventer(this));
            }
        }

        public void Update(Player player) // call in Player.Update
        {
            foreach (var util in utils)
                util.Update(player);
            //if (port != null) port.Update();
            //if (playerDeathPreventer != null) playerDeathPreventer.Update();
            //if (enemyCreator != null) enemyCreator.Update();
            //if (playerExtraMovement != null) playerExtraMovement.Update(player);
        }

        public virtual void SyncAcceptableDamage(int val)
        {

        }

        public T AddUtil<T>(T util) where T : PlayerModuleUtil
        {
            utils.Add(util);
            return util;
        }

        public virtual void InitExtraGraphics(PlayerGraphics playerGraphics)
        {

        }

        public virtual void ExtraGraphicsInitSprites(PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            //newEyeIndex = sLeaser.sprites.Length;
            //return 1;
        }

        public virtual void ExtraGraphicsAddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {

        }

        public virtual void ExtraGraphicsApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

        }

        public virtual void ExtraGraphicsUpdate(PlayerGraphics self)
        {

        }

        public virtual void ExtraGraphicsDrawSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

        }

        /// <summary>
        /// 用于在不同的梦境中修改背包的状态
        /// </summary>
        void SetUpOverrides(Player player)
        {
            if (CustomDreamRx.currentActivateNormalDream == null)
                return;

            if (CustomDreamRx.currentActivateNormalDream.activateDreamID == DroneMasterDream.DroneMasterDream_0)
            {
                stateOverride = new DreamStateOverride(0, false, Vector2.zero, 0f, 2);
            }
            else if (CustomDreamRx.currentActivateNormalDream.activateDreamID == DroneMasterDream.DroneMasterDream_1)
            {
                stateOverride = new DreamStateOverride(0, true, Vector2.zero, 0f, 2) { connectToDMProggress = 0f };
            }
            else if (CustomDreamRx.currentActivateNormalDream.activateDreamID == DroneMasterDream.DroneMasterDream_2)
            {
                stateOverride = new DreamStateOverride(1, true, Vector2.zero, 0f, 2);
            }
            else if (CustomDreamRx.currentActivateNormalDream.activateDreamID == DroneMasterDream.DroneMasterDream_3)
            {
                stateOverride = new DreamStateOverride(0, true, Vector2.zero, 0f, 1);
            }
            else if (CustomDreamRx.currentActivateNormalDream.activateDreamID == DroneMasterDream.DroneMasterDream_4)
            {
                stateOverride = new DreamStateOverride(1, true, Vector2.zero, 0f, 2);
            }
        }

        public class PlayerModuleUtil
        {
            public virtual void Update(Player player)
            {

            }

        }
    }

}
