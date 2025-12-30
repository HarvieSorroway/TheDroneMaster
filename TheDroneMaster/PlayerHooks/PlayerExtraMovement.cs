using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public class PlayerExtraMovement : PlayerModule.PlayerModuleUtil
    {
        public Vector2 extraVelocity = Vector2.zero;

        public override void Update(Player player)
        {
            float factor = Mathf.InverseLerp(0, player.slugcatStats.runspeedFac * 4f, player.mainBodyChunk.vel.magnitude);
            factor = Mathf.Lerp(0.001f, 1f,1f - factor);

            player.mainBodyChunk.vel += factor * extraVelocity;
            extraVelocity *= 0.6f;
        }
        public void PlusSpeed(Vector2 acc)
        {
            extraVelocity += acc;
        }
    }
}
