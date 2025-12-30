using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using MoreSlugcats;
using CustomSaveTx;

namespace TheDroneMaster
{
    public class DronePortGraphics
    {
        public static readonly float portBiasX = 7f;
        public static readonly float portBiasY = 8f;

        readonly int longFlashCounter = 40;
        readonly int shortFlashCounter = 5;


        public PlayerGraphics pGraphics;
        public CastLine[] castLines = new CastLine[25];

        public PhysicalObject currentCastTarget;

        public WeakReference<DreamStateOverride> overrideRef = new WeakReference<DreamStateOverride>(null);

        public int startIndex;

        public int SparkToGen = 0;
        public int GenSparkCooler = 0;
        public int LightningToGen = 0;
        public int GenLightningCooler = 0;
        public int castCounter = 0;

        public int switchLightCounter;
        public int nextLightFlashCounter;

        public bool playLightningSounds = true;
        public bool overrideDisplayDronePort = true;

        public bool portLightFlashing = false;
        public bool portLightOff = false;

        public float currentCastRad;

        public Vector2 currentCastPos;

        public Color LaserColor = LaserDroneGraphics.defaultLaserColor;


        public Color sparkColor => LaserColor;
        public Color RandomSparkColor => Color.Lerp(Color.Lerp(sparkColor, Color.blue, Random.value * 0.5f), Color.white, Random.value);
        public Player Player => pGraphics.player;
        public int totalSprites => overrideDisplayDronePort ? 2 + castLines.Length : 0;
        public DronePortGraphics(PlayerGraphics playerGraphics,int startIndex)
        {
            pGraphics = playerGraphics;
            this.startIndex = startIndex;

            if (PlayerPatchs.modules.TryGetValue(Player, out var module))
            {
                LaserColor = module.laserColor;
                if(module.stateOverride != null)
                {
                    overrideDisplayDronePort = module.stateOverride.initDronePortGraphics;
                    overrideRef = new WeakReference<DreamStateOverride>(module.stateOverride);
                }
            }

            if(pGraphics.player.room.game.session is StoryGameSession)
            {
                portLightFlashing = DeathPersistentSaveDataRx.GetTreatmentOfType<ScannedCreatureSaveUnit>().KingScanned && !pGraphics.player.room.game.rainWorld.progression.currentSaveState.deathPersistentSaveData.altEnding;
            }
            
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!overrideDisplayDronePort) return;

            for (int i = 0; i < castLines.Length; i++)
            {
                castLines[i] = new CastLine(this, startIndex + 2 + i);
            }

            sLeaser.sprites[startIndex] = new FSprite("pixel", true) { scaleX = 10f,scaleY = 17f};
            sLeaser.sprites[startIndex + 1] = new FSprite("Circle20", true) { scaleX = 0.1f, scaleY = 0.1f};

            for(int i = 0;i < castLines.Length; i++)
            {
                castLines[i].InitSprites(sLeaser, rCam);
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!overrideDisplayDronePort) return;

            for (int i = startIndex; i < startIndex + totalSprites; i++)
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }

            sLeaser.sprites[startIndex + 1].MoveBehindOtherNode(sLeaser.sprites[0]);
            sLeaser.sprites[startIndex].MoveBehindOtherNode(sLeaser.sprites[startIndex + 1]);

            for(int i = startIndex + 2;i < startIndex + totalSprites; i++)
            {
                sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[startIndex]);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!overrideDisplayDronePort) return;

            sLeaser.sprites[startIndex].color = palette.blackColor;

            if(PlayerPatchs.modules.TryGetValue(Player, out var module))
            {
                sLeaser.sprites[startIndex + 1].color = module.laserColor;
            }
            else
            {
                sLeaser.sprites[startIndex + 1].color = LaserDroneGraphics.defaultLaserColor;
            }
            

            for (int i = 0; i < castLines.Length; i++)
            {
                castLines[i].ApplyPalette(sLeaser, rCam,palette);
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!overrideDisplayDronePort) return;

            Vector2 vector = Vector2.Lerp(pGraphics.drawPositions[0, 1], pGraphics.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(pGraphics.drawPositions[1, 1], pGraphics.drawPositions[1, 0], timeStacker);

            float rotation = Custom.AimFromOneVectorToAnother(vector2, vector);
            Vector2 dir = Custom.DirVec(vector2, vector);

            if(overrideRef.TryGetTarget(out var overrides))
            {
                float connectProggress = overrides.connectToDMProggress;

                vector = Vector2.Lerp(overrides.dronePortGraphicsPos, vector, connectProggress);
                rotation = Mathf.Lerp(rotation,Mathf.Lerp(overrides.dronePortGraphicsRotation,rotation,connectProggress),0.05f);

                dir = Custom.DegToVec(rotation);

                overrides.currentPortPos = vector;
            }


            Vector2 perDir = Custom.PerpendicularVector(dir) * Custom.LerpMap(Mathf.Abs(Mathf.Abs(rotation) - 90f),90f,60f,0f,1f) * portBiasX * (rotation > 0 ? 1f : -1f);
            Vector2 perDir2 = perDir + Custom.PerpendicularVector(dir) * 3f * (rotation > 0 ? 1f : -1f);

            if (pGraphics.player.aerobicLevel > 0.5f)
            {
                float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(pGraphics.lastBreath, pGraphics.breath, timeStacker) * 3.1415927f * 2f);
                vector += Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, num) * Mathf.InverseLerp(0.5f, 1f, pGraphics.player.aerobicLevel) * 0.5f;
            }
            sLeaser.sprites[startIndex].rotation = rotation;
            sLeaser.sprites[startIndex].SetPosition(vector - camPos + perDir - dir * portBiasY);
            sLeaser.sprites[startIndex + 1].SetPosition(vector - camPos + perDir2 - dir * 1.6f * portBiasY);
            sLeaser.sprites[startIndex + 1].alpha = portLightOff ? 0f : 1f;

            for (int i = 0;i < castLines.Length; i++)
            {
                castLines[i].DrawSpites(sLeaser, rCam, timeStacker, camPos, vector + perDir - dir * portBiasY);
            }
        }

        public void Update()
        {
            if (!overrideDisplayDronePort) return;

            PlayerDamageStateUpdate();

            if (castCounter > 0)
            {
                if (currentCastTarget != null)
                {
                    if (currentCastTarget.room == null ||(pGraphics.owner != null && currentCastTarget.room != pGraphics.owner.room)) currentCastTarget = null;
                    Cast(currentCastTarget.firstChunk.pos, currentCastRad);
                }
                else
                {
                    Cast(currentCastPos, currentCastRad);
                }
                castCounter--;
            }
            else if (currentCastTarget != null) currentCastTarget = null;

            if (portLightFlashing)
            {
                if(switchLightCounter > 0)
                {
                    switchLightCounter--;
                }
                else
                {
                    switchLightCounter = !portLightOff ? shortFlashCounter : nextLightFlashCounter;
                    if (!portLightOff)
                        nextLightFlashCounter = (nextLightFlashCounter == longFlashCounter) ? shortFlashCounter : longFlashCounter;
                    portLightOff = !portLightOff;
                }
            }

            if (GenSparkCooler > 0) GenSparkCooler--;
            else
            {
                if (SparkToGen > 0)
                {
                    int sparkToGenThisFrame = (int)(SparkToGen / 2f + 3);
                    sparkToGenThisFrame = Mathf.Clamp(sparkToGenThisFrame, 0, SparkToGen);
                    SparkToGen -= sparkToGenThisFrame;

                    for (int i = 0; i < sparkToGenThisFrame; i++)
                    {
                        Player.room.AddObject(new Spark(Player.DangerPos, Custom.RNV() * Mathf.Lerp(4f, 28f, Random.value), RandomSparkColor, null, 20, 40));
                    }

                    GenSparkCooler = (int)Mathf.Lerp(10, 40, Random.value);
                    Player.room.PlaySound(SoundID.Zapper_Disrupted_LOOP, Player.mainBodyChunk);
                    Player.room.PlaySound(SoundID.Centipede_Electric_Charge_LOOP, Player.mainBodyChunk);
                }
            }
            if (GenLightningCooler > 0) GenLightningCooler--;
            else
            {
                if (LightningToGen > 0)
                {
                    int lightningToGenThisFrame = (int)(LightningToGen / 3f + 1);
                    lightningToGenThisFrame = Mathf.Clamp(lightningToGenThisFrame, 0, LightningToGen);
                    LightningToGen -= lightningToGenThisFrame;

                    for (int i = 0; i < lightningToGenThisFrame; i++)
                    {
                        Vector2 dir = Custom.RNV();
                        Vector2 pos = Player.DangerPos;

                        Vector2 corner = Custom.RectCollision(pos, pos + dir * 100000f, Player.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                        IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(Player.room, pos, corner);
                        if (intVector != null)
                        {
                            corner = Custom.RectCollision(corner, pos, Player.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                        }

                        var Lightning = new LightningBolt(pos, corner, 0, Mathf.Lerp(0.5f, 0.1f, Random.value));
                        Lightning.color = RandomSparkColor;
                        Lightning.intensity = 1f;

                        Player.room.AddObject(Lightning);
                        GenLightningCooler = (int)Mathf.Lerp(10, 20, Random.value);
                        if(playLightningSounds) Player.room.PlaySound(SoundID.Zapper_Disrupted_LOOP, Player.mainBodyChunk);
                    }

                    var vultures = from crit in Player.room.updateList where crit is Vulture select crit as Vulture;
                    if (vultures != null)
                    {
                        foreach (var vulture in vultures)
                        {
                            if (vulture.IsKing)
                            {
                                foreach (var tusk in vulture.kingTusks.tusks)
                                {
                                    if (tusk.impaleChunk != null && tusk.impaleChunk.owner == Player)
                                    {
                                        tusk.SwitchMode(KingTusks.Tusk.Mode.Dangling);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void PlayerDamageStateUpdate()
        {
            if (!overrideDisplayDronePort) return;

            if (pGraphics == null || pGraphics.owner == null) return;
            if(PlayerPatchs.modules.TryGetValue(Player, out var module))
            {
                switch (module.playerDeathPreventer.AcceptableDamageCount)
                {
                    case 0:
                        if (Random.value < 0.15f) Player.room.AddObject(new Explosion.ExplosionSmoke(Player.mainBodyChunk.pos, Custom.RNV() * 2f * Random.value, 1f));
                        if (Random.value < 0.2f)
                        {
                            Player.room.AddObject(new Spark(Player.DangerPos, Custom.RNV() * Mathf.Lerp(4f, 28f, Random.value), RandomSparkColor, null, 20, 40));
                        }
                        break;
                    case 1:
                        if(Random.value < 0.05f)
                        {
                             Player.room.AddObject(new Spark(Player.DangerPos, Custom.RNV() * Mathf.Lerp(4f, 28f, Random.value), RandomSparkColor, null, 10, 40));
                        }
                        break;
                    case 2:
                    default:
                        break;
                }
            } 
        }

        public void Charge(float size)
        {
            if (!overrideDisplayDronePort) return;

            if (SparkToGen > 0) return;
            SparkToGen = (int)Custom.LerpMap(size, 0f, 2f, 10, 50);
            GenSparkCooler = (int)Mathf.Lerp(10, 40, Random.value);
            Player.room.PlaySound(SoundID.Centipede_Electric_Charge_LOOP, Player.mainBodyChunk);
        }

        public void OverCharge(float size)
        {
            if (!overrideDisplayDronePort) return;

            playLightningSounds = true;
            if (LightningToGen > 0) return;
            LightningToGen = (int)Custom.LerpMap(size, 0, 2f, 10, 25);
            Player.room.PlaySound(SoundID.Zapper_Zap, Player.mainBodyChunk);
        }

        public void DeathShock(string shockfrom)
        {
            if (!overrideDisplayDronePort) return;

            Plugin.Log(shockfrom);
            if (Player == null) return;
            if (Player.room == null) return;

            (Player.State as PlayerState).permanentDamageTracking = 0;

            playLightningSounds = false;
            LightningToGen = (int)Custom.LerpMap(Random.value, 0, 2f, 10, 25);
            Player.room.PlaySound(SoundID.Zapper_Zap, Player.mainBodyChunk);
            Player.room.PlaySound(SoundID.Fire_Spear_Explode,Player.mainBodyChunk);

            Player.room.AddObject(new ShockWave(Player.mainBodyChunk.pos, 100f, 0.04f, 10));
            Player.room.AddObject(new Explosion(Player.room, Player, Player.DangerPos, 7, 250f, 6.2f, 2f, 280f, 0.25f, Player, 0.7f, 160f, 1f));
            Player.room.ScreenMovement(Player.mainBodyChunk.pos, default, 2f);
            Player.room.InGameNoise(new Noise.InGameNoise(Player.mainBodyChunk.pos, 9000f, Player, 1f));
        }

        public void ContinuousCast(Vector2 pos,int time,float rad)
        {
            if (!overrideDisplayDronePort) return;

            castCounter = time;
            currentCastPos = pos;
            currentCastRad = rad;
        }

        public void ContinuousCast(PhysicalObject castObj,int time,float rad)
        {
            if (!overrideDisplayDronePort) return;

            if (castObj.room != null && castObj.room == pGraphics.owner.room)
            currentCastTarget = castObj;
            castCounter = time;
            currentCastRad = rad;
        }

        public void Cast(Vector2 pos,float rad)
        {
            if (!overrideDisplayDronePort) return;

            if (Random.value > 0.25f) return;

            for(int i = 0;i < castLines.Length; i++)
            {
                if (castLines[i].ThisLineAvaialbleForNewCast)
                {
                    castLines[i].CastTo(pos + Custom.RNV() * rad * Random.value, (int)Custom.LerpMap(Random.value, 0f, 1f, 4, 8));
                    break;
                }
            }
        }


        public class CastLine
        {
            public float castWidth => 1f;
            public DronePortGraphics portGraphics;

            public int startIndex;
            public int show = 0;

            public Vector2 castPos;

            public bool ThisLineAvaialbleForNewCast => show == 0;

            public CastLine(DronePortGraphics portGraphics,int startIndex)
            {
                this.portGraphics = portGraphics;
                this.startIndex = startIndex;
            }

            public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[startIndex] = new CustomFSprite("pixel") { shader = rCam.room.game.rainWorld.Shaders["Hologram"] };
            }


            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                for(int i = 0;i < 4; i++)
                {
                    (sLeaser.sprites[startIndex] as CustomFSprite).verticeColors[i] = portGraphics.LaserColor;
                }
            }

            public void DrawSpites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 projectorPos)
            {
                if(show == 0)
                {
                    sLeaser.sprites[startIndex].isVisible = false;
                    return;
                }
                show--;
                sLeaser.sprites[startIndex].isVisible = true;

                Vector2 dir = Custom.DirVec(projectorPos, camPos);
                Vector2 perpDir = Custom.PerpendicularVector(dir);

                (sLeaser.sprites[startIndex] as CustomFSprite).MoveVertice(0, projectorPos - camPos + perpDir * castWidth / 2f);
                (sLeaser.sprites[startIndex] as CustomFSprite).MoveVertice(1, projectorPos - camPos - perpDir * castWidth / 2f);
                (sLeaser.sprites[startIndex] as CustomFSprite).MoveVertice(2, castPos - camPos - perpDir * castWidth / 2f);
                (sLeaser.sprites[startIndex] as CustomFSprite).MoveVertice(3, castPos - camPos + perpDir * castWidth / 2f);
            }

            public void CastTo(Vector2 castPos,int showTime)
            {
                this.castPos = castPos;
                show = showTime;
            }
        }
    }
}

