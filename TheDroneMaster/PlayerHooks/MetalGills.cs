using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;


namespace TheDroneMaster
{
	//imitate PlayerGraphics.AxolotlGills :P
	public class MetalGills
    {

		public readonly float width = 13f;
		public readonly float height = 2.5f;
		public readonly float gap = 5f;
		public readonly float misplacement = 3.5f;
		public readonly float horizontalBias = 2f;
		public readonly float[] widthFactor = new float[3]
		{
			1f,
			0.9f,
			0.8f
		};

        public PlayerGraphics pGraphics;
		public PlayerGraphics.AxolotlScale[] scaleObjects;

		public int acceptableDamage = 2;

		public float[] backwardsFactors;
		public float graphicHeight;
		public float rigor;
		public float scaleX;

		public bool colored;

		public Vector2[] scalesPositions;

		public int graphic;
		public int totalSprites;
		public int startSprite;

		public RoomPalette palette;

		public Color baseColor;
		public Color effectColor;
		public Color effectColorMidlle;

		public MetalGills(PlayerGraphics pGraphics, int startSprite)
		{
			this.pGraphics = pGraphics;
			this.startSprite = startSprite;
			rigor = 0.5873646f;
			float num = 1.310689f;
			colored = true;
			graphic = 3;
			graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic.ToString()).sourcePixelSize.y;
			int num2 = 3;
			scalesPositions = new Vector2[num2 * 2];
			scaleObjects = new PlayerGraphics.AxolotlScale[this.scalesPositions.Length];
			backwardsFactors = new float[this.scalesPositions.Length];
			float num3 = 0.1542603f;
			float num4 = 0.1759363f;
			for (int i = 0; i < num2; i++)
			{
				float num5 = 0.03570603f;
				float num6 = 0.659981f;
				float num7 = 0.9722961f;
				float num8 = 0.3644831f;
				if (i == 1)
				{
					num5 = 0.02899241f;
					num6 = 0.76459f;
					num7 = 0.6056554f;
					num8 = 0.9129724f;
				}
				if (i == 2)
				{
					num5 = 0.02639332f;
					num6 = 0.7482835f;
					num7 = 0.7223744f;
					num8 = 0.4567381f;
				}
				for (int j = 0; j < 2; j++)
				{
					scalesPositions[i * 2 + j] = new Vector2((j != 0) ? num6 : (-num6), num5);
					scaleObjects[i * 2 + j] = new PlayerGraphics.AxolotlScale(pGraphics);
					scaleObjects[i * 2 + j].length = Mathf.Lerp(2.5f, 15f, num * num7);
					scaleObjects[i * 2 + j].width = Mathf.Lerp(0.65f, 1.2f, num3 * num);
					backwardsFactors[i * 2 + j] = num4 * num8;
				}
			}
			totalSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i] = new CustomFSprite("pixel");
				if (this.colored)
				{
					sLeaser.sprites[i + this.scalesPositions.Length] = new CustomFSprite("pixel");
				}
			}

			//Plugin.Log("MetalGills Initsprites");
		}

		public void Update()
		{
			for (int i = 0; i < this.scaleObjects.Length; i++)
			{
				Vector2 pos = this.pGraphics.owner.bodyChunks[0].pos;
				Vector2 pos2 = this.pGraphics.owner.bodyChunks[1].pos;
				float num2 = 90f;
				int num3 = i % (this.scaleObjects.Length / 2);
				float num4 = num2 / (float)(this.scaleObjects.Length / 2);
				if (i >= this.scaleObjects.Length / 2)
				{
					pos.x += 8f;
				}
				else
				{
					pos.x -= 8f;
				}
				Vector2 vector = Custom.rotateVectorDeg(Custom.DegToVec(0f), (float)num3 * num4 - num2 / 2f  + 90f);
				float num5 = Custom.VecToDeg(this.pGraphics.lookDirection);
				Vector2 vector2 = Custom.rotateVectorDeg(Custom.DegToVec(0f), (float)num3 * num4 - num2 / 2f);
				Vector2 vector3 = Vector2.Lerp(vector2, Custom.DirVec(pos2, pos), Mathf.Abs(num5));
				if (this.scalesPositions[i].y < 0.2f)
				{
					vector3 -= vector * Mathf.Pow(Mathf.InverseLerp(0.2f, 0f, this.scalesPositions[i].y), 2f) * 2f;
				}
				vector3 = Vector2.Lerp(vector3, vector2, Mathf.Pow(this.backwardsFactors[i], 1f)).normalized;
				Vector2 vector4 = pos + vector3 * this.scaleObjects[i].length;
				if (!Custom.DistLess(this.scaleObjects[i].pos, vector4, this.scaleObjects[i].length / 2f))
				{
					Vector2 vector5 = Custom.DirVec(this.scaleObjects[i].pos, vector4);
					float num6 = Vector2.Distance(this.scaleObjects[i].pos, vector4);
					float num7 = this.scaleObjects[i].length / 2f;
					scaleObjects[i].pos += vector5 * (num6 - num7);
					scaleObjects[i].vel += vector5 * (num6 - num7);
				}
				scaleObjects[i].vel += Vector2.ClampMagnitude(vector4 - this.scaleObjects[i].pos, 10f) / Mathf.Lerp(5f, 1.5f, this.rigor);
				scaleObjects[i].vel *= Mathf.Lerp(1f, 0.8f, this.rigor);
				scaleObjects[i].ConnectToPoint(pos, this.scaleObjects[i].length, true, 0f, new Vector2(0f, 0f), 0f, 0f);
				scaleObjects[i].Update();
			}
		}
		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (this.pGraphics.owner == null)
			{
				return;
			}

            //Plugin.Log("MetalGills Initsprites");

            Vector2 sum = Vector2.zero;
			for (int t = 0; t < 6; t++)
			{
				sum += Vector2.Lerp(this.scaleObjects[t].lastPos, this.scaleObjects[t].pos, timeStacker);
			}
			sum /= 6f;

			float num5 = Custom.VecToDeg(this.pGraphics.lookDirection);
			float sinNum = Mathf.Sin(num5 * Mathf.PI / 180f);
			float perspectiveLeft = Custom.LerpMap(sinNum, 0f, -1f, 1.1f, 0.8f);
			float perspectiveRight = Custom.LerpMap(sinNum, 0f, 1f, 1.1f, 0.8f);

			if(pGraphics.player.input[0].x != 0)
            {
				perspectiveLeft = pGraphics.player.input[0].x < 0 ? 0.8f : 1.1f;
				perspectiveRight = pGraphics.player.input[0].x < 0 ? 1.1f : 0.8f;
			}

			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				Vector2 rootPos = new Vector2(sLeaser.sprites[9].x + camPos.x, sLeaser.sprites[9].y + camPos.y);

				int realIndex = (i - startSprite) % (this.scaleObjects.Length / 2);
				

				float rotation = Custom.AimFromOneVectorToAnother(sum + Vector2.right * 2f, Vector2.Lerp(this.scaleObjects[realIndex].lastPos, this.scaleObjects[realIndex].pos, timeStacker));
				float perspective;
				if (i >= this.startSprite + this.scalesPositions.Length / 2)
				{
					rootPos.x -= gap;
					realIndex = 2 - realIndex;
					rotation += 180f;
					perspective = perspectiveLeft;
				}
				else
				{
					perspective = perspectiveRight;
					rootPos.x += gap;
				}
				rootPos.y += horizontalBias;
				float facotr = widthFactor[realIndex] * perspective;

				Vector2 dir = Custom.DegToVec(rotation);
				Vector2 perpendicularDir = Custom.PerpendicularVector(dir);

				int upsideDown = 1;
				if (i < this.startSprite + this.scalesPositions.Length / 2)
				{
					upsideDown = -1;
				}

				(sLeaser.sprites[i] as CustomFSprite).MoveVertice(0, rootPos + perpendicularDir * height * 0.55f * upsideDown  - camPos);
				(sLeaser.sprites[i] as CustomFSprite).MoveVertice(1, rootPos - perpendicularDir * height * 0.55f * upsideDown - camPos);
				(sLeaser.sprites[i] as CustomFSprite).MoveVertice(2, rootPos - perpendicularDir * height * 0.5f * upsideDown - dir * (width * 0.4f * facotr) - camPos);
				(sLeaser.sprites[i] as CustomFSprite).MoveVertice(3, rootPos + perpendicularDir * height * 0.5f * upsideDown - dir * (width * 0.4f * facotr + misplacement) - camPos);

				if (colored)
				{
                    (sLeaser.sprites[i + this.scalesPositions.Length] as CustomFSprite).MoveVertice(0, (sLeaser.sprites[i] as CustomFSprite).vertices[3]);
                    (sLeaser.sprites[i + this.scalesPositions.Length] as CustomFSprite).MoveVertice(1, (sLeaser.sprites[i] as CustomFSprite).vertices[2]);
                    (sLeaser.sprites[i + this.scalesPositions.Length] as CustomFSprite).MoveVertice(2, rootPos - perpendicularDir * height * 0.35f * upsideDown - dir * (width * facotr) - camPos);
                    (sLeaser.sprites[i + this.scalesPositions.Length] as CustomFSprite).MoveVertice(3, rootPos + perpendicularDir * height * 0.35f * upsideDown - dir * (width * facotr + misplacement) - camPos);

					for (int k = 0; k < 4; k++)
					{
						(sLeaser.sprites[i + this.scalesPositions.Length] as CustomFSprite).verticeColors[k] = realIndex <= acceptableDamage ? effectColor : effectColorMidlle;
					}

					
				}
                //Plugin.Log(string.Format("i{0},root : {1}, color : {2},visible : {3]",i,rootPos, realIndex <= acceptableDamage ? effectColor : effectColorMidlle, (sLeaser.sprites[i + this.scalesPositions.Length] as CustomFSprite).isVisible));
            }
		}

		public void SetGillColors(Color baseCol, Color effectCol,Color effectColorMiddle)
		{
			baseColor = baseCol;
			effectColorMidlle = effectColorMiddle;
			effectColor = effectCol;
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			this.palette = palette;
			for (int j = this.startSprite + this.scalesPositions.Length - 1; j >= this.startSprite; j--)
			{
				(sLeaser.sprites[j] as CustomFSprite).verticeColors[0] = baseColor;
				(sLeaser.sprites[j] as CustomFSprite).verticeColors[1] = baseColor;
				(sLeaser.sprites[j] as CustomFSprite).verticeColors[2] = effectColorMidlle;
				(sLeaser.sprites[j] as CustomFSprite).verticeColors[3] = effectColorMidlle;
				if (colored)
				{
					for (int k = 0; k < 4; k++)
					{
						(sLeaser.sprites[j + this.scalesPositions.Length] as CustomFSprite).verticeColors[k] = effectColor;
					}
				}
			}
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			for (int i = this.startSprite; i < this.startSprite + this.totalSprites; i++)
			{
				newContatiner.AddChild(sLeaser.sprites[i]);
			}
        }
	}
}
