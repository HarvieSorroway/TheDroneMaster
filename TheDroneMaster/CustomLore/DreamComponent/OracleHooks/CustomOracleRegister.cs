using CustomOracleTx;
using HUD;
using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DreamComponent.OracleHooks;
using UnityEngine;
using static MonoMod.InlineRT.MonoModRule;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DreamComponent.OracleHooks
{
}
public class CustomOracleStateViz
{
    Oracle oracle;

    public FLabel label;
    public CustomOracleStateViz(Oracle oracle)
    {
        this.oracle = oracle;
        InitSprites();
    }

    public void InitSprites()
    {
        label = new FLabel(Custom.GetFont(), "") { anchorX = 0f, anchorY = 1f, scale = 1.1f,isVisible = true,alpha = 1f };
        Futile.stage.AddChild(label);
    }

    public void Update()
    {
        label.SetPosition(new Vector2(400f,300f));
        string text = string.Format("Oracle : {0}\n", oracle.ID);

        for(int i = 0;i < oracle.bodyChunks.Length;i++)
        {
            text += string.Format("Bodychunk{0} pos : {1}\n", i, oracle.bodyChunks[i].pos);
        }

        
        CustomOracleGraphic customOracleGraphic = oracle.graphicsModule as CustomOracleGraphic;
        CustomOracleBehaviour behaviour = (oracle.oracleBehavior as CustomOracleBehaviour);
        if (customOracleGraphic != null) 
        {
            text += string.Format("getToPos {0}\n", behaviour.OracleGetToPos);
            text += string.Format("idealPos {0}\n", behaviour.baseIdeal);
            text += string.Format("progression {0:f2}\n", behaviour.pathProgression);
            text += string.Format("Action : {0} inActionCounter {1}\n", behaviour.action, behaviour.inActionCounter);
            text += string.Format("Conversation : {0}", behaviour.conversation?.id);
        }
        label.text = text;
    }

    public void ClearSprites()
    {
        label.RemoveFromContainer();
    }
}
