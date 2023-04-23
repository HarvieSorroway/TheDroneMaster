using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DreamComponent.DreamHook
{
    /// <summary>
    /// DreamNutils类包含了部分梦境注册信息
    /// </summary>
    public class DreamNutils
    {
        /// <summary>
        /// 获取梦境场景的Session
        /// </summary>
        /// <param name="game">当前game</param>
        /// <param name="name">梦境前战役所使用的猫猫的名字</param>
        /// <returns></returns>
        virtual public DreamGameSession GetSession(RainWorldGame game, SlugcatStats.Name name)
        {
            return new DreamGameSession(game, name, this);
        }

        /// <summary>
        /// 判断本轮回是否有梦，一般可以获取game.Players进行具体判断
        /// </summary>
        /// <param name="game">当前game</param>
        /// <param name="malnourished">是否是饥饿状态</param>
        /// <returns></returns>
        virtual public bool HasDreamThisCycle(RainWorldGame game, bool malnourished)
        {
            return false;
        }


        /// <summary>
        /// 退出梦境，一般情况下不需要重写。
        /// </summary>
        /// <param name="game">当前game</param>
        /// <param name="survived">是否成功度过雨循环</param>
        /// <param name="newMalnourished">是否饥饿</param>
        virtual public void ExitDream(RainWorldGame game, bool survived, bool newMalnourished)
        {
            game.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;

            if (game.manager.musicPlayer != null)
                game.manager.musicPlayer.FadeOutAllSongs(SongFadeOut);

            if (!survived)
                game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.DeathScreen, SleepFadeIn);
            else
                game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SleepScreen, DeathFadeIn);
        }

        /// <summary>
        /// 梦境中加载的第一个房间
        /// 如果IsSingleWorld为true，则会从world文件夹下搜索
        /// 如果IsSingleWorld为false，则会在levels文件夹下搜索
        /// </summary>
        virtual public string FirstRoom => "accelerator";

        /// <summary>
        /// 如果为单房间(IsSingleWorld为true)情况下，是否在竞技场隐藏该房间(FirstRoom)
        /// </summary>
        virtual public bool HiddenRoomInArena => false;

        /// <summary>
        /// 是否为单房间模式
        /// </summary>
        virtual public bool IsSingleWorld => true;

        /// <summary>
        /// 在多房间模式下是否显示HUD界面（不会显示MAP）
        /// </summary>
        virtual public bool HasHUD => true;

        /// <summary>
        /// 梦中死亡是否计入保存
        /// 如果为true则梦中死亡不影响正常存档
        /// </summary>
        virtual public bool ForceSave => false;

        /// <summary>
        /// 进入雨眠界面的淡出时长
        /// </summary>
        virtual public float SleepFadeIn => 3f;

        /// <summary>
        /// 进入死亡界面的淡出时长
        /// </summary>
        virtual public float DeathFadeIn => 3f;

        /// <summary>
        /// 梦境结束时歌曲淡出时长
        /// </summary>
        virtual public float SongFadeOut => 20f;

        /// <summary>
        /// 存档用函数，会调用ExitDream
        /// </summary>
        /// <param name="game"></param>
        /// <param name="asDeath"></param>
        /// <param name="asQuit"></param>
        /// <param name="newMalnourished"></param>
        public void ExitDream_Base(RainWorldGame game, bool asDeath, bool asQuit, bool newMalnourished)
        {
            var survived = !(asDeath || asQuit) || ForceSave;
            var oldgame = game.manager.oldProcess as RainWorldGame;
            if (oldgame == null)
                DreamSessionHook.LogException(new Exception("[DreamGameSession] OldPrcess is not a RainWorldGame Class!"));

            //progression会在切换process时清空(PostSwitchMainProcess)，需重新赋值
            oldgame.rainWorld.progression.currentSaveState = oldgame.GetStorySession.saveState;
            oldgame.GetStorySession.saveState.SessionEnded(oldgame, survived, newMalnourished);

            ExitDream(game, survived, newMalnourished);
            return;
        }
    }

    public class DreamGameSession : GameSession
    {
        /// <summary>
        /// 获取DreamNutils，获取一部分参数
        /// </summary>
        public DreamNutils owner;

        /// <summary>
        /// 标志结束session，会在QuitGame被设置为true
        /// </summary>
        public bool EndSession;

        /// <summary>
        /// session运行时间
        /// 初始化后SessionCounter会每帧+1，可用作梦中计时器
        /// </summary>
        protected int SessionCounter;

        /// <summary>
        /// 在初始化结束后会每帧刷新
        /// SessionCounter会每帧+1，可用作梦中计时器
        /// </summary>
        virtual public void Update()
        {
        }

        /// <summary>
        /// 在世界加载后调用，放置巢穴(den)内的生物或offscreen的生物用
        /// </summary>
        virtual public void PostWorldLoaded()
        {
        }

        /// <summary>
        /// 在初始房间实例化后调用，一般可以放置shortcut内生成的生物。
        /// 玩家放置也在这个函数内进行
        /// </summary>
        virtual public void PostFirstRoomRealized()
        {

        }

        #region 生物生成API

        public AbstractCreature SpawnCreatureOffScreen(CreatureTemplate.Type type)
        {
            AbstractCreature abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(type), null, new WorldCoordinate(game.world.offScreenDen.index, 0, 0, 0), game.GetNewID());
            game.world.GetAbstractRoom(0).MoveEntityToDen(abstractCreature);
            game.world.offScreenDen.AddEntity(abstractCreature);
            return abstractCreature;
        }
        public AbstractCreature SpawnCreatureInShortCut(CreatureTemplate.Type type, string roomName, int suggestExits)
        {
            int roomID = game.world.GetAbstractRoom(roomName).index;
            return SpawnCreatureInShortCut(type, roomID, suggestExits);
        }
        public AbstractCreature SpawnCreatureInShortCut(CreatureTemplate.Type type, int roomID, int suggestExits)
        {
            if (game.world.GetAbstractRoom(roomID).realizedRoom == null)
            {
                DreamSessionHook.LogException(new Exception("Can't spawn creature in non-activate room short cut"));
                return null;
            }
            int exits = game.world.GetAbstractRoom(roomID).exits;

            int node = Mathf.Min(suggestExits, exits);
            AbstractCreature abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(type), null, new WorldCoordinate(0, -1, -1, -1), game.GetNewID());
            abstractCreature.state = new HealthState(abstractCreature);

            abstractCreature.Realize();
            ShortcutHandler.ShortCutVessel shortCutVessel = new ShortcutHandler.ShortCutVessel(new IntVector2(-1, -1), abstractCreature.realizedCreature, game.world.GetAbstractRoom(roomID), 0);
            shortCutVessel.entranceNode = node;
            shortCutVessel.room = game.world.GetAbstractRoom(roomID);
            abstractCreature.pos.room = game.world.offScreenDen.index;
            game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            return abstractCreature;
        }
        public AbstractCreature SpawnCreatureInDen(CreatureTemplate.Type type, string roomName, int suggestDen)
        {
            int roomID = game.world.GetAbstractRoom(roomName).index;
            return SpawnCreatureInDen(type, roomID, suggestDen);
        }
        public AbstractCreature SpawnCreatureInDen(CreatureTemplate.Type type, int roomID, int suggestDen)
        {

            var room = game.world.GetAbstractRoom(roomID);
            int dens = room.dens;
            suggestDen = Mathf.Min(suggestDen, dens);
            int denNodeIndex = -1;
            for (int i = 0; i < room.nodes.Length; i++)
            {
                if (room.nodes[i].type == AbstractRoomNode.Type.Den)
                {
                    denNodeIndex = i;
                    if (suggestDen == 0)
                        break;
                    suggestDen--;
                }
            }
            AbstractCreature abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(type), null, new WorldCoordinate(roomID, denNodeIndex, -1, -1), game.GetNewID());
            abstractCreature.remainInDenCounter = 20;
            room.MoveEntityToDen(abstractCreature);
            return abstractCreature;
        }
        public AbstractCreature SpawnPlayerInShortCut(string roomName, int suggestShortCut)
        {
            int roomID = game.world.GetAbstractRoom(roomName).index;
            return SpawnPlayerInShortCut(roomID, suggestShortCut);
        }
        public AbstractCreature SpawnPlayerInShortCut(int roomID, int suggestShortCut)
        {
            if (game.world.GetAbstractRoom(roomID).realizedRoom == null)
            {
                DreamSessionHook.LogException(new Exception("Can't spawn player in non-activate room"));
                return null;
            }
            int exits = game.world.GetAbstractRoom(roomID).exits;
            suggestShortCut = Mathf.Min(suggestShortCut, exits);

            AbstractCreature abstractCreature = null;
            if (Players.Count == 0)
            {
                abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(roomID, -1, -1, -1), new EntityID(-1, 0));
                game.cameras[0].followAbstractCreature = abstractCreature;
            }
            else
                abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(roomID, -1, -1, -1), game.GetNewID());
            abstractCreature.state = new PlayerState(abstractCreature, 0, characterStats.name, false);

            abstractCreature.Realize();
            ShortcutHandler.ShortCutVessel shortCutVessel = new ShortcutHandler.ShortCutVessel(new IntVector2(-1, -1), abstractCreature.realizedCreature, game.world.GetAbstractRoom(roomID), 0);
            shortCutVessel.entranceNode = suggestShortCut;
            shortCutVessel.room = game.world.GetAbstractRoom(roomID);
            abstractCreature.pos.room = game.world.offScreenDen.index;
            game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            AddPlayer(abstractCreature);
            return abstractCreature;
        }
        public AbstractCreature SpawnPlayerInRoom(string roomName, IntVector2 pos)
        {
            int roomID = game.world.GetAbstractRoom(roomName).index;
            return SpawnPlayerInRoom(roomID, pos);
        }
        public AbstractCreature SpawnPlayerInRoom(int roomID, IntVector2 pos)
        {
            if (game.world.GetAbstractRoom(roomID).realizedRoom == null)
            {
                DreamSessionHook.LogException(new Exception("Can't spawn player in non-activate room"));
                return null;
            }

            AbstractCreature abstractCreature = null;
            if (Players.Count == 0)
            {
                abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(roomID, pos.x, pos.y, -1), new EntityID(-1, 0));
                game.cameras[0].followAbstractCreature = abstractCreature;
            }
            else
                abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(roomID, pos.x, pos.y, -1), game.GetNewID());

            abstractCreature.state = new PlayerState(abstractCreature, 0, characterStats.name, false);
            abstractCreature.Realize();
            AddPlayer(abstractCreature);
            return abstractCreature;
        }

        #endregion

        #region 基类
        /// <summary>
        /// 基本的update，不许你们动xx
        /// </summary>
        public void Base_Update()
        {
            if (EndSession) return;

            if (!isRealized && game.cameras[0].room != null && game.cameras[0].room.shortCutsReady)
            {
                PostFirstRoomRealized();
                Plugin.Log("Dream session room realized");
                isRealized = true;
            }
            if (!isLoaded && game.world != null)
            {
                PostWorldLoaded();
                Plugin.Log("Dream session world loaded");
                isLoaded = true;
            }

            if (isRealized)
            {
                Update();
                SessionCounter++;
            }
        }

        public DreamGameSession(RainWorldGame game, SlugcatStats.Name name, DreamNutils ow) : base(game)
        {
            owner = ow;
            characterStats = new SlugcatStats(name, false);
        }

        /// <summary>
        /// 初始房间是否实例化
        /// </summary>
        protected bool isRealized;


        /// <summary>
        /// 世界是否加载完毕
        /// </summary>
        protected bool isLoaded;
        #endregion
    }

    static public class DreamSessionHook
    {
        /// <summary>
        /// 注册梦到游戏
        /// </summary>
        /// <param name="dream">梦的参数类DreamNutils</param>
        static public void RegisterDream(DreamNutils dream)
        {
            OnModInit();
            dreams.Add(dream);
        }

        #region Hook
        static bool isLoaded = false;
        static DreamSessionHook()
        {
            dreams = new List<DreamNutils>();
        }
        public delegate SlugcatStats orig_slugcatStats(Player self);
        public static SlugcatStats Player_slugcatStats_get(orig_slugcatStats orig, Player self)
        {
            if (self.abstractCreature.world.game.session is DreamGameSession)
                return self.abstractCreature.world.game.session.characterStats;
            return orig(self);
        }



        static public void Log(string message)
        {
            Debug.Log("[DreamNutils] " + message);
        }
        static public void LogException(Exception e)
        {
            Debug.LogError("[DreamNutils] ERROR!");
            Debug.LogException(e);
        }

        static void OnModInit()
        {
            if (!isLoaded)
            {
                On.RainWorldGame.ExitGame += RainWorldGame_ExitGame;
                On.RainWorldGame.Win += RainWorldGame_Win;
                On.RainWorldGame.Update += RainWorldGame_Update;
                On.ProcessManager.PreSwitchMainProcess += ProcessManager_PreSwitchMainProcess;
                On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
                On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
                On.MultiplayerUnlocks.IsLevelUnlocked += MultiplayerUnlocks_IsLevelUnlocked;

                IL.RainWorldGame.ctor += RainWorldGame_ctorIL;
                IL.World.ctor += World_ctorIL;
                IL.RegionGate.ctor += RegionGate_ctorIL;

                Hook slugcatStateHook = new Hook(
                    typeof(Player).GetProperty("slugcatStats", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                    typeof(DreamSessionHook).GetMethod("Player_slugcatStats_get", BindingFlags.Static | BindingFlags.Public));

                isLoaded = true;
            }
        }

        static private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            if (cam.game.session is DreamGameSession)
            {
                if (!(cam.game.session as DreamGameSession).owner.HasHUD)
                    return;
                self.AddPart(new TextPrompt(self));
                self.AddPart(new KarmaMeter(self, self.fContainers[1], new IntVector2((self.owner as Player).Karma, (self.owner as Player).KarmaCap), (self.owner as Player).KarmaIsReinforced));
                self.AddPart(new FoodMeter(self, (self.owner as Player).slugcatStats.maxFood, (self.owner as Player).slugcatStats.foodToHibernate, null, 0));
                //self.AddPart(new Map(self, new Map.MapData(cam.room.world, cam.room.game.rainWorld)));
                self.AddPart(new RainMeter(self, self.fContainers[1]));
                if (ModManager.MSC)
                {
                    self.AddPart(new AmmoMeter(self, null, self.fContainers[1]));
                    self.AddPart(new HypothermiaMeter(self, self.fContainers[1]));
                    if ((self.owner as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
                    {
                        self.AddPart(new GourmandMeter(self, self.fContainers[1]));
                    }
                }
                if (ModManager.MMF && MMF.cfgBreathTimeVisualIndicator.Value)
                {
                    self.AddPart(new BreathMeter(self, self.fContainers[1]));
                    if (ModManager.CoopAvailable && cam.room.game.session != null)
                    {
                        for (int i = 1; i < cam.room.game.session.Players.Count; i++)
                        {
                            self.AddPart(new BreathMeter(self, self.fContainers[1], cam.room.game.session.Players[i]));
                        }
                    }
                }
                if (ModManager.MMF && MMF.cfgThreatMusicPulse.Value)
                {
                    self.AddPart(new ThreatPulser(self, self.fContainers[1]));
                }
                if (ModManager.MMF && MMF.cfgSpeedrunTimer.Value)
                {
                    self.AddPart(new SpeedRunTimer(self, null, self.fContainers[1]));
                }
                if (cam.room.abstractRoom.shelter)
                {
                    self.karmaMeter.fade = 1f;
                    self.rainMeter.fade = 1f;
                    self.foodMeter.fade = 1f;
                }
                return;
            }
            orig(self, cam);
        }

        static private bool MultiplayerUnlocks_IsLevelUnlocked(On.MultiplayerUnlocks.orig_IsLevelUnlocked orig, MultiplayerUnlocks self, string levelName)
        {
            foreach (var data in dreams)
            {
                if (data.IsSingleWorld && data.HiddenRoomInArena && data.FirstRoom == levelName)
                    return false;
            }
            return orig(self, levelName);
        }

        static private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            if (self.session is DreamGameSession)
                (self.session as DreamGameSession).Base_Update();
            orig(self);
        }

        static private void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            if (self.game.session is DreamGameSession)
            {
                var text = self.FIRSTROOM = self.game.startingRoom = (self.game.session as DreamGameSession).owner.FirstRoom;
                if (!(self.game.session as DreamGameSession).owner.IsSingleWorld)
                    self.LoadWorld(text.Split('_')[0].ToUpper(), self.game.session.characterStats.name, (self.game.session as DreamGameSession).owner.IsSingleWorld);
                else
                    self.LoadWorld(self.game.startingRoom, self.game.session.characterStats.name, (self.game.session as DreamGameSession).owner.IsSingleWorld);

                return;
            }
            orig(self);
        }

        static private void RegionGate_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            try
            {
                if (c.TryGotoNext(MoveType.After, i => i.MatchLdarg(0),
                                                 i => i.MatchCall<RegionGate>("Reset")))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    var label = c.DefineLabel();
                    c.EmitDelegate<Func<RegionGate, bool>>((gate) =>
                    {
                        if (gate is WaterGate)
                            (gate as WaterGate).waterLeft = 1f;
                        else if (gate is ElectricGate)
                            (gate as ElectricGate).batteryLeft = 1f;
                        return gate.room.world.game.session is DreamGameSession;
                    });
                    c.Emit(OpCodes.Brtrue_S, label);
                    c.GotoNext(MoveType.Before, i => i.MatchLdarg(0),
                                                i => i.MatchLdarg(0),
                                                i => i.MatchNewobj<RegionGateGraphics>());
                    c.MarkLabel(label);
                }
                else
                    LogException(new Exception("RegionGate_ctor IL hook failed!"));
            }
            catch (Exception e)
            {
                LogException(new Exception("RegionGate_ctor IL hook failed!"));
                LogException(e);
            }

        }

        static private void World_ctorIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After, i => i.MatchCallvirt<RainWorldGame>("get_GetArenaGameSession")))
                {
                    c.GotoPrev(MoveType.After, i => i.MatchLdarg(1));

                    var notArena = c.DefineLabel();
                    var arena = c.DefineLabel();
                    c.EmitDelegate<Func<RainWorldGame, bool>>((game) =>
                    {
                        return game.IsArenaSession;
                    });
                    c.Emit(OpCodes.Brfalse_S, notArena);

                    c.Emit(OpCodes.Ldarg_1);
                    c.GotoNext(MoveType.After, i => i.MatchStfld<World>("rainCycle"));
                    c.Emit(OpCodes.Br_S, arena);
                    c.MarkLabel(notArena);

                    c.EmitDelegate<Action<World, World>>((self, world) =>
                    {
                        //我无聊 好吧是为了清除返回值
                        self.rainCycle = new RainCycle(world, 100);
                    });
                    c.MarkLabel(arena);
                }
                else
                    LogException(new Exception("World_ctor IL hook failed!"));
            }
            catch (Exception e)
            {
                LogException(new Exception("RegionGate_ctor IL hook failed!"));
                LogException(e);
            }
        }

        static private void RainWorldGame_ExitGame(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
        {
            if (self.session is DreamGameSession)
            {
                (self.session as DreamGameSession).EndSession = true;
                (self.session as DreamGameSession).owner.ExitDream_Base(self, asDeath, asQuit, ma);
                return;
            }
            orig(self, asDeath, asQuit);
        }
        static private void ProcessManager_PreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {

            if (self.oldProcess is RainWorldGame && self.currentMainLoop is RainWorldGame && (self.currentMainLoop as RainWorldGame).session is DreamGameSession)
            {
                //切换回story进行数据传输
                var game = self.oldProcess;
                self.oldProcess = self.currentMainLoop;
                self.currentMainLoop = game;

                //手动删除梦境
                self.oldProcess.ShutDownProcess();
                self.oldProcess.processActive = false;

                //清除恼人的coop控件
                if (!game.processActive && ModManager.JollyCoop)
                {
                    foreach (var camera in (game as RainWorldGame).cameras)
                    {
                        if (camera.hud != null && camera.hud.jollyMeter != null)
                        {
                            camera.hud.parts.Remove(camera.hud.jollyMeter);
                            camera.hud.jollyMeter = null;
                        }
                    }
                }
            }
            orig(self, ID);
        }

        static private void RainWorldGame_ctorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before, i => i.MatchNewobj<OverWorld>(),
                                              i => i.MatchStfld<RainWorldGame>("overWorld")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RainWorldGame>>((self) =>
                {
                    if (self.manager.oldProcess is RainWorldGame)
                    {
                        if (activeDream != null)
                        {
                            self.session = activeDream.GetSession(self, (self.manager.oldProcess as RainWorldGame).session.characterStats.name);
                            activeDream = null;
                        }
                    }
                });
            }
        }

        static private void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (!(self.session is DreamGameSession))
            {
                foreach (var dream in dreams)
                {
                    if (dream.HasDreamThisCycle(self, malnourished))
                    {
                        activeDream = dream;
                        break;
                    }
                }
                if (activeDream != null)
                {
                    Plugin.Log("Try entering dream");
                    self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                    ma = malnourished;
                    self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                    return;
                }
            }
            orig(self, malnourished);
        }
        static List<DreamNutils> dreams;
        static bool ma;
        static DreamNutils activeDream;
        #endregion
    }

    public class DroneMasterDream : DreamNutils
    {
        public static DroneMasterDream instance;

        public DroneMasterDream() : base()
        {
            instance = this;
        }

        public override DreamGameSession GetSession(RainWorldGame game, SlugcatStats.Name name)
        {
            return new DroneMasterDreamSession(game, name, this);
        }

        public override string FirstRoom => "DMD_AI";

        public override bool ForceSave => true;
        public override bool IsSingleWorld => false;
        public override bool HasHUD => false;

        public override bool HasDreamThisCycle(RainWorldGame game, bool malnourished)
        {
            return true;
        }
    }

    public class DroneMasterDreamSession : DreamGameSession
    {
        public DroneMasterDreamSession(RainWorldGame game,SlugcatStats.Name name,DreamNutils ow) : base(game, name, ow)
        {

        }

        public override void AddPlayer(AbstractCreature player)
        {
            Plugin.Log("DMDream AddPlayer");
            base.AddPlayer(player);
        }
    }
}
