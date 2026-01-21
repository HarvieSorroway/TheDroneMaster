using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu
{
    internal class SkillInfoScreen : PositionedMenuObject
    {
        static float SkillScreenHeigth = 200f;

        Vector2 rPos, lastrPos;

        float revealProg;
        string focusingSkillID = string.Empty;

        FSprite bkg_bk, bkg_title, icon_bkg,icon_bkg2, icon;
        FLabel skillNameLabel, skillDescriptionLabel;

        List<ConditionLabel> conditionLabels = new List<ConditionLabel>();

        public SkillInfoScreen(Menu.Menu menu, MenuObject owner) : base(menu, owner, Vector2.zero)
        {
            bkg_bk = new FSprite("pixel")
            {
                color = Color.black,
                scaleX = Custom.rainWorld.options.ScreenSize.x,
                scaleY = SkillScreenHeigth,
                anchorY = 0,
                anchorX = 0,
                alpha = 0.8f
            };
            Container.AddChild(bkg_bk);

            bkg_title = new FSprite("pixel")
            {
                color = SkillTreeMenu.pink,
                scaleX = Custom.rainWorld.options.ScreenSize.x,
                scaleY = 10f,
                anchorX = 0,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            Container.AddChild(bkg_title);

            icon_bkg = new FSprite("SkillScreen_IconBkg")
            {
                scaleX = 1f,
                scaleY = 1f,
                anchorX = 0,
                anchorY = 0.5f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            Container.AddChild(icon_bkg);

            icon_bkg2 = new FSprite("SkillScreen_IconBkg_2")
            {
                scaleX = .8f,
                scaleY = .8f,
                anchorX = 0,
                anchorY = 0.5f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            Container.AddChild(icon_bkg2);

            icon = new FSprite("pixel")
            {
                scaleX = 1.4f,
                scaleY = 1.4f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                isVisible = false
            };
            Container.AddChild(icon);

            skillNameLabel = new FLabel(Custom.GetDisplayFont(), "")
            {
                color = SkillTreeMenu.pink,
                anchorX = 0f,
                scale = 1.7f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            Container.AddChild(skillNameLabel);

            skillDescriptionLabel = new FLabel(Custom.GetDisplayFont(), "")
            {
                color = SkillTreeMenu.pink * 0.5f + Color.white * 0.5f,
                anchorX = 0f,
                anchorY = 1f
            };
            Container.AddChild(skillDescriptionLabel);

   
            lastrPos = rPos = Vector2.Lerp(Vector2.down * SkillScreenHeigth, Vector2.zero, DMHelper.EaseInOutCubic(revealProg));

            var button = new SkillTreeHoldButton(menu, this, new Vector2(1100f, 100f), new Vector2(100f, 35f), "Upgrade", null);
            subObjects.Add(button);
            menu.pages[0].selectables.Add(button);
        }

        public override void Update()
        {
            base.Update();
            
            lastrPos = rPos;

            if(!string.IsNullOrEmpty(focusingSkillID) && revealProg < 1f)
            {
                revealProg += 1 / 30f;
            }
            else if(string.IsNullOrEmpty(focusingSkillID) && revealProg > 0f)
            {
                revealProg -= 1 / 30f;
            }
            rPos = Vector2.Lerp(Vector2.down * SkillScreenHeigth, Vector2.zero, DMHelper.EaseInOutCubic(revealProg));
            pos = (owner as PositionedMenuObject).ScreenPos + rPos;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 smoothPos = Vector2.Lerp(lastrPos, rPos, timeStacker);
            bkg_bk.SetPosition(smoothPos);
            bkg_title.SetPosition(smoothPos + Vector2.up * (SkillScreenHeigth - 10f));
            icon_bkg.SetPosition(smoothPos + new Vector2(0f, SkillScreenHeigth / 2f - 15f));
            icon_bkg2.SetPosition(smoothPos + new Vector2(-50f, SkillScreenHeigth / 2f - 15f));
            icon.SetPosition(smoothPos + new Vector2(80f, SkillScreenHeigth / 2f - 15f));

            skillNameLabel.SetPosition(smoothPos + new Vector2(150f, SkillScreenHeigth - 40f));
            skillDescriptionLabel.SetPosition(smoothPos + new Vector2(200f, SkillScreenHeigth - 80f));

            foreach(var l in conditionLabels)
            {
                l.DrawSprites(smoothPos);
            }
        }

        public void SetFocusingSkillNode(string skillID, string sprite)
        {
            foreach (var l in conditionLabels)
                l.RemoveSprites();
            conditionLabels.Clear();

            focusingSkillID = skillID;

            if (string.IsNullOrEmpty(skillID))
            {
                skillNameLabel.text = "";
                skillDescriptionLabel.text = "";
                icon.isVisible = false;
                return;
            }

            if (SkillNodeLoader.loadedSkillNodes.TryGetValue(skillID, out var node))
            {
                icon.isVisible = true;
                if (node.TryGetDescriptionInfo(Custom.rainWorld.inGameTranslator.currentLanguage, out var res))
                {
                    skillNameLabel.text = res.name;
                    skillDescriptionLabel.text = res.description;
                    icon.SetElementByName(sprite);
                }
                else
                {
                    skillNameLabel.text = $"{skillID} - Name";
                    skillDescriptionLabel.text = $"{skillID} - DescriptionInfoNotFound";
                    icon.SetElementByName("RenderNode.PlaceHolder");
                }

                for(int i = 0;i < node.conditions.Count;i++)
                {
                    var conditionInfo = node.conditions[i];
                    var conditionLabel = new ConditionLabel(this, conditionInfo, i);
                    conditionLabels.Add(conditionLabel);
                }
            }
            else
            {
                skillNameLabel.text = $"{skillID} - Name";
                skillDescriptionLabel.text = $"{skillID} - skillNodeNotFound";
                icon.isVisible = false;
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            bkg_bk.RemoveFromContainer();
            bkg_title.RemoveFromContainer();
            icon_bkg.RemoveFromContainer();
            icon_bkg2.RemoveFromContainer();
            icon.RemoveFromContainer();

            skillNameLabel.RemoveFromContainer();
            skillDescriptionLabel.RemoveFromContainer();

            foreach(var l in conditionLabels)
                l.RemoveSprites();
            conditionLabels.Clear();
        }

        class ConditionLabel
        {
            SkillInfoScreen owner;
            FLabel label;
            int i;

            public ConditionLabel(SkillInfoScreen owner, SkillNode.SKillNodeConditionInfo conditionInfo, int i)
            {
                this.owner = owner;
                this.i = i;
                label = new FLabel(Custom.GetDisplayFont(), LabelText(conditionInfo, i))
                {
                    anchorX = 0f,
                    color = ConditionColor(CheckCondition()),
                    alpha = 0.6f,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"]
                };
                owner.Container.AddChild(label);
            }

            public void DrawSprites(Vector2 ownerBasePos)
            {
                label.SetPosition(ownerBasePos + new Vector2(600f, SkillScreenHeigth - 40f - i * 30f));
            }

            public void RemoveSprites()
            {
                label.RemoveFromContainer();
            }

            public bool CheckCondition()
            {
                return true;
            }

            public string LabelText(SkillNode.SKillNodeConditionInfo conditionInfo, int i )
            {
                if(conditionInfo.type == SkillNode.ConditionType.SkillNode)
                {
                    string prefix = "";
                    if (conditionInfo.boolType == SkillNode.ConditionBoolType.And)
                        prefix = "Requries: ";
                    else if (conditionInfo.boolType == SkillNode.ConditionBoolType.NotAnd)
                        prefix = "Must not have: ";
                    else if (conditionInfo.boolType == SkillNode.ConditionBoolType.Or)
                        prefix = "Requries: ";
                    else if (conditionInfo.boolType == SkillNode.ConditionBoolType.NotOr)
                        prefix = "Requires not have: ";

                    var otherNode = SkillNodeLoader.loadedSkillNodes[conditionInfo.info];
                    string name = otherNode.TryGetDescriptionInfo(Custom.rainWorld.inGameTranslator.currentLanguage, out var res) ? res.name : conditionInfo.info;
                    return prefix + name;
                }

                return "";
            }

            static Color ConditionColor(bool matched)
            {
                if (matched)
                    return Color.green;
                else
                    return Color.red;
            }
        }
    }
}
