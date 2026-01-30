using CustomSaveTx;
using Menu;
using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSave;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu.MenuAnim;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu
{
    internal class SkillTreeMenu : Menu.Menu
    {
        public static ProcessManager.ProcessID SkillTreeID = new ProcessManager.ProcessID(nameof(SkillTreeID), true);

        public static SkillTreeMenu Instance;

        internal Dictionary<string, ISkillTreeObject> idMapper = new Dictionary<string, ISkillTreeObject>();
        internal Dictionary<string, List<string>> groupIdMapper = new Dictionary<string, List<string>>();

        internal SkillTreeButton escButton;
        internal SkillTreeBkgEffect bkgEff;
        internal SkillInfoScreen skillInfoScreen;

        //saveInfo
        internal DMPSBasicSave skillTreeSave;

        //return info
        PauseMenu pauseMenuFrom;

        //layerInfo
        internal List<string>[] layerNodeLst;

        internal string focusingSkillTreeCatagory, focusingRenderNode;
        internal bool focus;

        MenuAnimation anim;
        internal Vector2[] layerPulseCenters;
        internal float[] layerPulseRads;

        internal Vector2 middleScreen;

        public float Energy => skillTreeSave.Energy;
        public int maxEnergy = 80;

        public bool previewMode;
        FLabel test;

        //anim
        FSprite bkg;
        public float alpha, lastAlpha;
        bool quitTriggered;

        public SkillTreeMenu(ProcessManager manager, PauseMenu pauseMenuFrom, bool previewMode) : base(manager, SkillTreeID)
        {
            this.pauseMenuFrom = pauseMenuFrom;
            this.previewMode = previewMode;
            skillTreeSave = DeathPersistentSaveDataRx.GetTreatmentOfType<DMPSBasicSave>();

            container.AddChild(bkg = new FSprite("pixel")
            {
                scale = 2000f,
                color = Custom.hexToColor("1C080E"),
                x = 500f,
                y = 300f,
                alpha = 0f
            });
            container.AddChild(test = new FLabel(Custom.GetDisplayFont(), "")
            {
                anchorX = 0f,
                anchorY = 0f
            });

            this.pages.Add(new Page(this, null, "main", 0));
            pages[0].pos = middleScreen = Custom.rainWorld.options.ScreenSize / 2f;
            layerNodeLst = new List<string>[3];
            layerPulseCenters = new Vector2[3];
            layerPulseRads = new float[3];

            for (int i = 0; i < 3; i++)
            {
                layerNodeLst[i] = new List<string>();
                layerPulseRads[i] = 0f;
            }
            layerPulseCenters[2] = new Vector2(150f, 600f);

            CreateRenderElement();
        }

        void CreateRenderElement()
        {
            bkgEff = new SkillTreeBkgEffect(this, pages[0], Vector2.zero);
            pages[0].subObjects.Add(bkgEff);

            List<SkillTreeRenderNode> postAddNodes = new List<SkillTreeRenderNode>();
            foreach (var renderNode in RenderNodeLoader.renderNodes)
            {
                if (renderNode.typeInfo == SkillTreeRenderType.LineNode)
                {
                    var newobj = new SkillTreeLineRenderer(this, pages[0], renderNode.posInfo, renderNode.layer);
                    pages[0].subObjects.Add(newobj);

                    idMapper.Add(renderNode.renderNodeIDInfo, newobj);
                    layerNodeLst[renderNode.layer].Add(renderNode.renderNodeIDInfo);
                }
                else
                {
                    postAddNodes.Add(renderNode);
                }
            }

            foreach(var renderNode in postAddNodes)
            {
                if(renderNode.typeInfo == SkillTreeRenderType.NodeGroup)
                {
                    groupIdMapper.Add(renderNode.renderNodeIDInfo, renderNode.subRenderNodeInfo.ToList());
                }
                else if(renderNode.typeInfo == SkillTreeRenderType.BasicNode)
                {
                    var newobj = new SkillTreeCatagoryButton(this, pages[0], renderNode.posInfo[0], renderNode.posInfo[1], renderNode.iconSprite, renderNode.renderNodeIDInfo, renderNode.scaleInfo, renderNode.typeInfo == SkillTreeRenderType.StaticNode);
                    pages[0].subObjects.Add(newobj);

                    idMapper.Add(renderNode.renderNodeIDInfo, newobj);
                    layerNodeLst[renderNode.layer].Add(renderNode.renderNodeIDInfo);
                }
                else if(renderNode.typeInfo == SkillTreeRenderType.StaticNode)
                {
                    var newobj = new SkillTreeButton(this, pages[0], renderNode.posInfo[0], renderNode.iconSprite, renderNode.renderNodeIDInfo, renderNode.scaleInfo, renderNode.typeInfo == SkillTreeRenderType.StaticNode);
                    pages[0].subObjects.Add(newobj);

                    idMapper.Add(renderNode.renderNodeIDInfo, newobj);
                    layerNodeLst[renderNode.layer].Add(renderNode.renderNodeIDInfo);
                }
                else if(renderNode.typeInfo == SkillTreeRenderType.SubBasicNode)
                {
                    var newobj = new SkillTreeSkillButton(this, pages[0], renderNode.posInfo[0], renderNode.posInfo[1], renderNode.iconSprite, renderNode.renderNodeIDInfo, renderNode.scaleInfo, renderNode.typeInfo == SkillTreeRenderType.StaticNode);
                    pages[0].subObjects.Add(newobj);

                    idMapper.Add(renderNode.renderNodeIDInfo, newobj);
                    layerNodeLst[renderNode.layer].Add(renderNode.renderNodeIDInfo);
                }
            }

            pages[0].subObjects.Add(escButton = new SkillTreeButton(this, pages[0], new Vector2(80f, 670f), "RenderNode.Esc", "RenderNode.Esc", 0.8f, false) { fixedPos = new Vector2(80f, 670f) });
            escButton.setAlpha = escButton.lastSetAlpha = 0f;

            pages[0].subObjects.Add(skillInfoScreen = new SkillInfoScreen(this, pages[0]));

            anim = new ShowMenuAnim(this);
            SyncSkillState();
        }

        void Reload()
        {
            bkgEff.RemoveSprites();
            bkgEff = null;

            escButton.RemoveSprites();
            escButton = null;

            skillInfoScreen.RemoveSprites();
            skillInfoScreen = null;

            pages[0].RemoveSprites();
            pages[0].subObjects.Clear();
            pages.RemoveAt(0);
            pages.Add(new Page(this, null, "main", 0));
            pages[0].pos = middleScreen = Custom.rainWorld.options.ScreenSize / 2f;

            idMapper.Clear();
            groupIdMapper.Clear();

            foreach (var lst in layerNodeLst)
                lst.Clear();

            focus = false;
            focusingRenderNode = string.Empty;
            focusingSkillTreeCatagory = string.Empty;

            anim = null;
            layerPulseRads[0] = layerPulseRads[1] = layerPulseRads[2] = 0f;

            RenderNodeLoader.Load();
            SkillNodeLoader.Load();
            CreateRenderElement();
        }

        public void SyncSkillState()
        {
            foreach(var item in idMapper.Values)
            {
                if(item is SkillTreeSkillButton skillTreeButton)
                {
                    skillTreeButton.SkillEnabled = skillTreeSave.CheckSkill(RenderNodeLoader.idMapper[skillTreeButton.id].bindSkillNodeInfo);
                }
            }
            skillInfoScreen.SyncState();
        }

        public override void Update()
        {
            base.Update();
            lastAlpha = alpha;
 
            if(anim != null)
            {
                anim.Update();
                if(anim.Finished)
                {
                    anim.Finish();
                    anim = null;
                }    
            }
            test.text = $"PulseRad : {layerPulseRads[0]} - {layerPulseRads[1]}, {layerPulseRads[2]}";
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            float a = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            bkg.alpha = a;
            pages[0].Container.alpha = a;

            if (Input.GetKeyDown(KeyCode.Escape) && !quitTriggered)
            {
                anim = new QuitMenuAnim(this);
                quitTriggered = true;
            }
            if (Input.GetKeyDown(KeyCode.R))
                Reload();
        }

        public void QuitSkillTreeMenu()
        {
            Instance = null;
            ShutDownProcess();
        }

        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
            Instance = null;
            //Plugin.postEffect.Strength = 0f;
            if (pauseMenuFrom != null)
            {
                pauseMenuFrom.SpawnExitContinueButtons();
            }

            manager.sideProcesses.Remove(this);
        }

        public override void Singal(MenuObject sender, string message)
        {
            if (anim != null && !anim.AllowSignal)
                return;

            if(message == "CLICKED")
            {
                var button = sender as SkillTreeButton;

                if(button.id == "RenderNode.Esc")
                {
                    focus = false;
                    anim = new UnfocusingCatagoryAnim(this);
                    PlaySound(SoundID.MENU_Dream_Button);
                }
                else if (layerNodeLst[1].Contains(button.id))
                {
                    focusingSkillTreeCatagory = button.id;
                    focus = true;
                    anim = new FocusingCatagoryAnim(this);
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
                else if (layerNodeLst[2].Contains(button.id))
                {
                    var renderInfo = RenderNodeLoader.idMapper[button.id];
                    skillInfoScreen.SetFocusingSkillNode(renderInfo.bindSkillNodeInfo, string.IsNullOrEmpty(renderInfo.iconSprite) ? "RenderNode.PlaceHolder" : renderInfo.iconSprite);
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            }
        }

        public void Save()
        {
            Custom.rainWorld.progression.SaveDeathPersistentDataOfCurrentState(false, false);
        }

        public static void OpenSkillTree(ProcessManager manager, PauseMenu pauseMenu, bool previewMode)
        {
            if(Instance == null)
                Instance = new SkillTreeMenu(manager, pauseMenu, previewMode);
            manager.sideProcesses.Add(Instance);
        }
    }

    internal interface ISkillTreeObject
    {
        void SetAlpha(float alpha);
        void SetShrink(float shrink);
    }
}
