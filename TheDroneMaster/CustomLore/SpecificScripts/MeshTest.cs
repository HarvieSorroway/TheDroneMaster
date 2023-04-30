using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.CustomLore.SpecificScripts;
using UnityEngine;

namespace TheDroneMaster
{
    class MeshTest : CosmeticSprite
    {
        public MeshTest(Player player)
        {
            pos = player.mainBodyChunk.pos + Vector2.up * 20;
            this.player = player;

            var gameObject = new GameObject();
            var meshFilter = gameObject.AddComponent<MeshFilter>();

            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assetbundles/rendertest"));

            meshFilter.mesh = bundle.LoadAsset<Mesh>("Assets/Scenes/WigmanGUN.fbx");

            
        

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = bundle.LoadAsset<Material>("Assets/Scenes/Unlit_Unlit.mat");

            //meshRenderer.material.SetFloat("_Width", 0.1f);
            gameObject.transform.localScale *= 400;
            node = new FOpaqueGameObjectNode(gameObject, false, false, false);

            bundle.Unload(false);
        }
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[0];
            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            pos = player.mainBodyChunk.pos + Vector2.up * 100;

            node.x = pos.x - camPos.x;
            node.y = pos.y - camPos.y;
            node.gameObject.transform.localPosition = node.screenConcatenatedMatrix.GetVector3FromLocalVector2(Vector2.zero, 300f);
            node.gameObject.transform.localRotation = Quaternion.Euler(0f,180f * (Mathf.Sin(Time.time) * 2f + 1f),0f);
            //node.gameObject.GetComponent<MeshRenderer>().material.SetFloat("_Width", Mathf.Lerp(0.8f,0.1f,(Mathf.Sin(mesh.rotation3D.x / 180f * Mathf.PI)+1)/2f));
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Midground");
            }
            newContatiner.AddChild(node);
        }
        Player player;
        FGameObjectNode node;
    }

    public class FOpaqueGameObjectNode : FGameObjectNode
    {
        public FOpaqueGameObjectNode(GameObject gameObject, bool shouldLinkPosition, bool shouldLinkRotation, bool shouldLinkScale) : base(gameObject, shouldLinkPosition, shouldLinkRotation, shouldLinkScale)
        {

        }
        public override void Update(int depth)
        {
            base.Update(depth - 3000);
        }
    }
}