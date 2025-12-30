using CustomSaveTx;
using DMPS.PlayerHooks;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using TheDroneMaster.DMPS.DMPSDrone;
using UnityEngine;
using static TheDroneMaster.LaserDroneGraphics;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSPort
{
    public class PSDronePortGraphics
    {
        public static readonly float portBiasX = 7f;
        public static readonly float portBiasY = 8f;

        readonly int longFlashCounter = 40;
        readonly int shortFlashCounter = 5;

        Color laserColor;

        public PlayerGraphics pGraphics;
        public PhysicalObject currentCastTarget;

        public WeakReference<DreamStateOverride> overrideRef = new WeakReference<DreamStateOverride>(null);

       
        int _startIndex;
        public int startIndex
        {
            get => _startIndex;
            set
            {
                if (_startIndex != value)
                {
                    _startIndex = value;
                    if(flameTrails != null)
                    {
                        for (int i = 0; i < flameTrails.Length; i++)
                        {
                            flameTrails[i].sprite = startIndex + arm.totSprites + 2 + i;
                        }
                        arm.sprite = startIndex;
                    }
                }
            }
        }

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

        public Color LaserColor = LaserDroneGraphics.defaultLaserColor;

        public Color sparkColor => LaserColor;
        public Color RandomSparkColor => Color.Lerp(Color.Lerp(sparkColor, Color.blue, Random.value * 0.5f), Color.white, Random.value);
        public Player Player => pGraphics.player;
        public int totalSprites => arm.totSprites + 2 + flameTrails.Length + 2;

        //jet mode
        ChunkSoundEmitter sound;
        JetFlameTrail[] flameTrails;
        float targetJet;
        float jet;
        float lastJet;
        float jetLerpParam;

        //arm
        PortArm arm;

        public PSDronePortGraphics(PlayerGraphics playerGraphics, int startIndex, Color laserColor)
        {
            pGraphics = playerGraphics;
            this.startIndex = startIndex;
            this.laserColor = laserColor;

            if (PlayerPatchs.modules.TryGetValue(Player, out var module))
            {
                LaserColor = module.laserColor;
                if (module.stateOverride != null)
                {
                    overrideDisplayDronePort = module.stateOverride.initDronePortGraphics;
                    overrideRef = new WeakReference<DreamStateOverride>(module.stateOverride);
                }
            }

            if (pGraphics.player.room.game.session is StoryGameSession)
            {
                portLightFlashing = DeathPersistentSaveDataRx.GetTreatmentOfType<ScannedCreatureSaveUnit>().KingScanned && !pGraphics.player.room.game.rainWorld.progression.currentSaveState.deathPersistentSaveData.altEnding;
            }

            arm = new PortArm(this, startIndex);

            this.laserColor = laserColor;
            flameTrails = new JetFlameTrail[4];
            for(int i = 0;i < flameTrails.Length; i++)
            {
                if (i > 1)
                    flameTrails[i] = new JetFlameTrail(this, startIndex + arm.totSprites + 2 + i, Color.white, maxLength: 60f);
                else
                    flameTrails[i] = new JetFlameTrail(this, startIndex + arm.totSprites + 2 + i, laserColor, maxLength: 100f);
            }
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!overrideDisplayDronePort) return;

            arm.InitiateSprites(sLeaser, rCam);

            sLeaser.sprites[startIndex + arm.totSprites] = new FSprite("pixel", true) { scaleX = 10f, scaleY = 17f };
            sLeaser.sprites[startIndex + arm.totSprites + 1] = new FSprite("Circle20", true) { scaleX = 0.1f, scaleY = 0.1f };

            for (int i = 0; i < flameTrails.Length; i++)
            {
                flameTrails[i].InitiateSprites(sLeaser, rCam);
            }

            sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length] = new FSprite("DMPS_JetFlare", true)
            {
                alpha = 0,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length + 1] = new FSprite("DMPS_JetFlare", true)
            {
                alpha = 0,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!overrideDisplayDronePort) return;

            for (int i = startIndex; i < startIndex + totalSprites; i++)
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }

            sLeaser.sprites[startIndex + 0].MoveBehindOtherNode(sLeaser.sprites[0]);
            sLeaser.sprites[startIndex + 1].MoveBehindOtherNode(sLeaser.sprites[startIndex + 0]);
            sLeaser.sprites[startIndex + arm.totSprites + 1].MoveInFrontOfOtherNode(sLeaser.sprites[startIndex + 1]);
            sLeaser.sprites[startIndex + arm.totSprites].MoveBehindOtherNode(sLeaser.sprites[startIndex + arm.totSprites + 1]);

            for (int i = startIndex + 2 + arm.totSprites; i < startIndex + totalSprites; i++)
            {
                sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[startIndex]);
            }

            var water = rCam.ReturnFContainer("Water");
            water.AddChild(sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length]);
            water.AddChild(sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length + 1]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!overrideDisplayDronePort) return;

            arm.ApplyPalette(sLeaser, rCam, palette);
            sLeaser.sprites[startIndex + arm.totSprites].color = palette.blackColor;

            if (PlayerPatchs.modules.TryGetValue(Player, out var module))
            {
                sLeaser.sprites[startIndex + arm.totSprites + 1].color = module.laserColor;
            }
            else
            {
                sLeaser.sprites[startIndex + arm.totSprites + 1].color = LaserDroneGraphics.defaultLaserColor;
            }

            sLeaser.sprites[startIndex + 2 + arm.totSprites + flameTrails.Length].color = laserColor;
            sLeaser.sprites[startIndex + 2 + arm.totSprites + flameTrails.Length + 1].color = laserColor;
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!overrideDisplayDronePort) return;

            Vector2 vector = Vector2.Lerp(pGraphics.drawPositions[0, 1], pGraphics.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(pGraphics.drawPositions[1, 1], pGraphics.drawPositions[1, 0], timeStacker);

            float rotation = Custom.AimFromOneVectorToAnother(vector2, vector);
            Vector2 dir = Custom.DirVec(vector2, vector);

            Vector2 perDir = Custom.PerpendicularVector(dir) * Custom.LerpMap(Mathf.Abs(Mathf.Abs(rotation) - 90f), 90f, 60f, 0f, 1f) * portBiasX * (rotation > 0 ? 1f : -1f);
            Vector2 perDir2 = perDir + Custom.PerpendicularVector(dir) * 3f * (rotation > 0 ? 1f : -1f);

            if (pGraphics.player.aerobicLevel > 0.5f)
            {
                float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(pGraphics.lastBreath, pGraphics.breath, timeStacker) * Mathf.PI * 2f);
                vector += Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, num) * Mathf.InverseLerp(0.5f, 1f, pGraphics.player.aerobicLevel) * 0.5f;
            }

            Vector2 portPos = vector + perDir - dir * portBiasY;

            sLeaser.sprites[startIndex + arm.totSprites].rotation = rotation;
            sLeaser.sprites[startIndex + arm.totSprites].SetPosition(portPos - camPos);
            sLeaser.sprites[startIndex + arm.totSprites + 1].SetPosition(vector - camPos + perDir2 - dir * 1.6f * portBiasY);
            sLeaser.sprites[startIndex + arm.totSprites + 1].alpha = portLightOff ? 0f : 1f;

            Vector2 jetPosA = vector - camPos + perDir2 - dir * 2f * portBiasY;
            Vector2 jetPosB = vector - camPos - perDir2 * 0.5f - dir * 2f * portBiasY;

            float smoothJet = Mathf.Lerp(lastJet, jet, timeStacker);
            flameTrails[0].DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPosA, smoothJet);
            flameTrails[1].DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPosB, smoothJet);

            flameTrails[2].DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPosA, smoothJet);
            flameTrails[3].DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPosB, smoothJet);

            Vector2 armExtendPerpDir = (dir.y > 0 ? -dir : dir);
            Vector2 armExtendDir = Custom.PerpendicularVector(armExtendPerpDir);
            if ((armExtendPerpDir.x * armExtendDir.x) < 0f)
                armExtendDir = -armExtendDir;
            arm.DrawSprite(sLeaser, rCam, timeStacker, camPos, portPos, armExtendDir, armExtendPerpDir);

            if (jet == 0)
            {
                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length].isVisible = false;
                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length + 1].isVisible = false;
            }
            else
            {
                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length].isVisible = true;
                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length + 1].isVisible = true;

                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length].SetPosition(jetPosA);
                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length].alpha = Random.value * smoothJet;

                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length + 1].SetPosition(jetPosB);
                sLeaser.sprites[startIndex + arm.totSprites + 2 + flameTrails.Length + 1].alpha = Random.value * smoothJet;
            }

        }

        public void Update()
        {
            if (!overrideDisplayDronePort) return;

            PlayerDamageStateUpdate();

            if(jet != targetJet || lastJet != targetJet)
            {
                lastJet = jet;
                jet = Mathf.Lerp(jet, targetJet, jetLerpParam);
                if (Mathf.Approximately(jet, targetJet))
                {
                    jet = targetJet;
                    if (jet == 0f)
                        sound.Destroy();
                }
                sound.volume = jet * 0.5f;

            }

            if (portLightFlashing)
            {
                if (switchLightCounter > 0)
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
                        if (playLightningSounds) Player.room.PlaySound(SoundID.Zapper_Disrupted_LOOP, Player.mainBodyChunk);
                    }
                }
            }


            Vector2 jetVel = (Player.bodyChunks[1].pos - Player.bodyChunks[0].pos).normalized * 30f;
            for(int i = 0;i < flameTrails.Length; i++)
            {
                flameTrails[i].Update(jetVel, Player.bodyChunks[0].vel, jet);
            }
        }

        public void PlayerDamageStateUpdate()
        {
            if (!overrideDisplayDronePort) return;

            if (pGraphics == null || pGraphics.owner == null) return;
            if (PlayerPatchs.modules.TryGetValue(Player, out var module))
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
                        if (Random.value < 0.05f)
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
            Player.room.PlaySound(SoundID.Fire_Spear_Explode, Player.mainBodyChunk);

            Player.room.AddObject(new ShockWave(Player.mainBodyChunk.pos, 100f, 0.04f, 10));
            Player.room.AddObject(new Explosion(Player.room, Player, Player.DangerPos, 7, 250f, 6.2f, 2f, 280f, 0.25f, Player, 0.7f, 160f, 1f));
            Player.room.ScreenMovement(Player.mainBodyChunk.pos, default, 2f);
            Player.room.InGameNoise(new Noise.InGameNoise(Player.mainBodyChunk.pos, 9000f, Player, 1f));
        }

        public void IntoJetMode()
        {
            targetJet = 1f;
            jetLerpParam = 0.25f;
            sound = Player.room.PlaySound(Watcher.WatcherEnums.WatcherSoundID.Box_Worm_Spitting_Steam_LOOP, Player.mainBodyChunk);
            sound.pitch = 4f;
        }

        public void ExitJetMode()
        {
            targetJet = 0f;
            jetLerpParam = 0.1f;
        }

        public class JetFlameTrail
        {
            public int maxTrailLength;

            public Vector2[,] positionsList;
            public List<Color> colorsList;

            public int sprite;
            public float powFactor;
            public Color color;

            float segmentLength;

            public JetFlameTrail(PSDronePortGraphics owner, int sprite, Color color, int maxTrailLength = 5, float powFactor = 1.5f, float maxLength = 100f)
            {
                this.sprite = sprite;
                this.color = color;
                this.maxTrailLength = maxTrailLength;
                this.powFactor = powFactor;
                segmentLength = maxLength / maxTrailLength;
                InitJetPos();
            }

            void InitJetPos()
            {
                positionsList = new Vector2[maxTrailLength, 2];
                for(int i = 0;i < maxTrailLength; i++)
                {
                    positionsList[i, 0] = Vector2.zero;
                    positionsList[i, 1] = Vector2.zero;
                }
                colorsList = new List<Color>
                {
                    color
                };
            }

            public Vector2 GetSmoothPos(int i, float timeStacker)
            {
                return Vector2.Lerp(GetPos(i, true), GetPos(i, false), timeStacker);
            }

            public Vector2 GetPos(int i, bool lastPos = false)
            {
                return positionsList[Custom.IntClamp(i, 0, maxTrailLength - 1), (lastPos ? 1 : 0)];
            }

            public Color GetCol(int i)
            {
                var col = colorsList[Custom.IntClamp(i, 0, this.colorsList.Count - 1)];
                return col;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                int segments = maxTrailLength - 1;
                bool pointyTip = false;
                bool customColor = true;
                TriangleMesh.Triangle[] array = new TriangleMesh.Triangle[(segments - 1) * 4 + (pointyTip ? 1 : 2)];
                for (int i = 0; i < segments - 1; i++)
                {
                    int num = i * 4;
                    for (int j = 0; j < 4; j++)
                    {
                        array[num + j] = new TriangleMesh.Triangle(num + j, num + j + 1, num + j + 2);
                    }
                }
                array[(segments - 1) * 4] = new TriangleMesh.Triangle((segments - 1) * 4, (segments - 1) * 4 + 1, (segments - 1) * 4 + 2);
                if (!pointyTip)
                {
                    array[(segments - 1) * 4 + 1] = new TriangleMesh.Triangle((segments - 1) * 4 + 1, (segments - 1) * 4 + 2, (segments - 1) * 4 + 3);
                }
                TriangleMesh triangleMesh = new TriangleMesh("pixel", array, customColor, true);
                triangleMesh.alpha = 0.8f;

                sLeaser.sprites[this.sprite] = triangleMesh;
            }

            public void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 jetPos, float jet)
            {
                if(jet == 0f)
                {
                    sLeaser.sprites[this.sprite].isVisible = false;
                    return;
                }
                else
                    sLeaser.sprites[this.sprite].isVisible = true;

                Vector2 lastPos = jetPos;
                float num = 5f * jet;
                for (int i = 0; i < this.maxTrailLength - 1; i++)
                {
                    Vector2 smoothPos = this.GetSmoothPos(i, timeStacker) + jetPos;
                    Vector2 smoothPos2 = this.GetSmoothPos(i + 1, timeStacker) + jetPos;
                    Vector2 dir = (lastPos - smoothPos).normalized;
                    Vector2 perDir = Custom.PerpendicularVector(dir);
                    dir *= Vector2.Distance(lastPos, smoothPos2) / 5f;
                    float width = Mathf.Pow(Custom.LerpMap(i, 0, maxTrailLength - 1, 1f, 0f), powFactor);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4, lastPos - perDir * num * width - dir);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4 + 1, lastPos + perDir * num * width - dir);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4 + 2, smoothPos - perDir * num * width + dir);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4 + 3, smoothPos + perDir * num * width + dir);
                    lastPos = smoothPos;
                }
                for (int j = 0; j < (sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors.Length; j++)
                {
                    float num2 = (float)j / (float)((sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors.Length - 1);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors[j] = GetCol(j);
                }
            }

            public void SetVisible(RoomCamera.SpriteLeaser sLeaser, bool visible)
            {
                sLeaser.sprites[sprite].isVisible = visible;
            }

            public void Update(Vector2 jetVel, Vector2 ownerVel, float jet)
            {
                for(int i = 0;i < maxTrailLength; i++)
                {
                    positionsList[i, 1] = positionsList[i, 0];
                    positionsList[i, 0] = Vector2.ClampMagnitude(positionsList[i, 0] + jetVel * Custom.LerpMap(i,0, maxTrailLength - 1, 1f, 0.5f), i * segmentLength * jet);
                }
            }
        }
    
        public class PortArm
        {
            public static Vector2 size = new Vector2(30f, 3f);

            PSDronePortGraphics owner;
            public int sprite;
            public int totSprites => 2;

            public PortArm(PSDronePortGraphics owner, int sprite)
            {
                this.owner = owner;
                this.sprite = sprite;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[sprite] = new FSprite("pixel", true)
                {
                    scaleX = size.x,
                    scaleY = size.y,
                    //scaleX = 2,
                    //scaleY = 2,
                    anchorX = 0,
                };
                sLeaser.sprites[sprite + 1] = new FSprite("pixel", true)
                {
                    scaleX = size.x,
                    scaleY = size.y,
                    //scaleX = 2,
                    //scaleY = 2,
                    anchorX = 0,
                };
            }

            public void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 armBasePos, Vector2 armExtendDir, Vector2 perpArmExtendDir)
            {
                float armExtend = 0f;
                bool hasModule = PlayerPatchs.TryGetModule<DMPSModule>(owner.Player, out var m);
                if (hasModule)
                {
                    armExtend = Mathf.Lerp(m.port.lastArmExtend, m.port.armExtend, timeStacker);
                }

                if(armExtend <= 0.01f)
                {
                    sLeaser.sprites[sprite].isVisible = false;
                    sLeaser.sprites[sprite + 1].isVisible = false;
                    return;
                }
                else
                {
                    sLeaser.sprites[sprite].isVisible = true;
                    sLeaser.sprites[sprite + 1].isVisible = true;
                }

                (Vector2 armTipPos, Vector2 armJointPos, float armLength) = GetTipNJointPosNArmLength(owner.Player, armExtend, timeStacker);

                if (hasModule)
                {
                    m.port.armTipPos = armTipPos;
                    m.port.armTipDir = (armTipPos - armJointPos).normalized;
                }

                sLeaser.sprites[sprite].rotation = Custom.VecToDeg((armJointPos - armBasePos).normalized) + 90f;
                sLeaser.sprites[sprite].SetPosition(armJointPos - camPos);
                sLeaser.sprites[sprite].scaleX = armLength;

                sLeaser.sprites[sprite + 1].rotation = Custom.VecToDeg((armJointPos - armTipPos).normalized) - 90f;
                sLeaser.sprites[sprite + 1].SetPosition(armTipPos - camPos);
                sLeaser.sprites[sprite + 1].scaleX = armLength;
            }
            
            public static (Vector2 tipPos, Vector2 jointPos, float armLength) GetTipNJointPosNArmLength(Player player, float armExtend, float timeStacker)
            {
                Vector2 vector = Vector2.Lerp(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, timeStacker);
                Vector2 vector2 = Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker);

                float rotation = Custom.AimFromOneVectorToAnother(vector2, vector);
                Vector2 dir = Custom.DirVec(vector2, vector);

                Vector2 perDir = Custom.PerpendicularVector(dir);
                perDir = perDir.normalized;
                if (player.flipDirection < 0)
                    perDir = -perDir;

                Vector2 armBasePos = vector + perDir * Custom.LerpMap(Mathf.Abs(Mathf.Abs(rotation) - 90f), 90f, 60f, 0f, 1f) * portBiasX * (rotation > 0 ? 1f : -1f) - dir * portBiasY;
                Vector2 armExtendDir = perDir.normalized;
                Vector2 perpArmExtendDir = -dir;

                float armLength = Mathf.Lerp(10f, size.x, DMHelper.LerpEase(armExtend));

                Vector2 extend = armExtendDir * size.x * Mathf.Sin(Mathf.PI * armExtend) - perpArmExtendDir * size.x * armExtend * 1.6f;
                Vector2 armTipPos = armBasePos + extend;


                Vector2 perpExtendDir = Custom.PerpendicularVector(extend.normalized);
                if (player.flipDirection < 0)
                    perpExtendDir = -perpExtendDir;
                Vector2 armJointPos = (armBasePos + armTipPos) / 2f + Mathf.Sin(Mathf.Acos((Vector2.Distance(armBasePos, armTipPos) / 2f) / armLength)) * armLength * perpExtendDir;

                return (armTipPos, armJointPos, armLength);
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                sLeaser.sprites[sprite].color = palette.blackColor;
                sLeaser.sprites[sprite + 1].color = palette.blackColor;
                //sLeaser.sprites[sprite].color = Color.red;
                //sLeaser.sprites[sprite + 1].color = Color.green;
            }

            public void Update(Vector2 jetVel, Vector2 ownerVel, float jet)
            {
            }
        }
    }
}
