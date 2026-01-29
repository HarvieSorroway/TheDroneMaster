using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.ParticleSystem
{

    public class EmitterException : Exception
    {
        public EmitterException(string message) : base(message)
        {
            
        }
        
    }
    public class KuyoParticleEmitter : CosmeticSprite
    {
        public static KuyoParticleEmitter CreateParticleEmitter(Room room, float emitterLife, bool loop = false, bool forceDie = false, CreateParticleDelegate createParticle = null,
            string container = "Water")
        {
            var re = new KuyoParticleEmitter(room, createParticle ?? SimpleParticle.DefaultParticle, emitterLife, loop, forceDie, container);
            room.AddObject(re);
            return re;
        }

        public static KuyoParticleEmitter CreateParticleEmitter(Room room,Vector2 pos, float emitterLife, bool loop = false, bool forceDie = false, CreateParticleDelegate createParticle = null,
            string container = "Water")
        {
            var re = new KuyoParticleEmitter(room, createParticle ?? SimpleParticle.DefaultParticle, emitterLife, loop, forceDie, container);
            re.lastPos = re.pos = pos;
            room.AddObject(re);
            return re;
        }

        protected KuyoParticleEmitter(Room room, CreateParticleDelegate createParticle, float emitterMaxTime, bool loop, bool forceDie, string container)
        {
            this.room = room;
            this.emitterMaxTime = emitterMaxTime;
            this.loop = loop;
            this.forceDie = forceDie;
            this.createParticle = createParticle;
            this.container = container;
            if (this.emitterMaxTime < 0)
                isInf = true;

            //仅负责创建
            createPlaceHolder = createParticle(this);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = Array.Empty<FSprite>();
            sLeaser.containers = new FContainer[extendLength];
            for(int i =0;i < sLeaser.containers.Length;i++)
                createPlaceHolder.InitiateSprites(rCam, sLeaser.containers[i] = new FContainer());
            containerIndex = rCam.SpriteLayerIndex[container];
        }


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
            {
                sLeaser.CleanSpritesAndRemove();
                foreach (var container in sLeaser.containers)
                {
                    container.RemoveAllChildren();
                    container.RemoveFromContainer();
                } 
                return;
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (sLeaser.containers.Length < maxLength)
            {
                var lastLength = sLeaser.containers.Length;
                Array.Resize(ref sLeaser.containers, maxLength + extendLength);
                for (int i = lastLength; i < sLeaser.containers.Length; i++)
                    createPlaceHolder.InitiateSprites(rCam, sLeaser.containers[i] = new FContainer());
            }

            if (needCleanContainer.Count != 0)
            {
                foreach (var index in needCleanContainer)
                {
                    sLeaser.containers[index].RemoveFromContainer();
                    //sLeaser.containers[index].SetPosition(-100, -100);
                }

                needCleanContainer.Clear();
            }

            var smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            foreach (var particle in particles)
            {
                if(sLeaser.containers[particle.BindContainerIndex].container == null)
                    rCam.SpriteLayers[containerIndex].AddChild(sLeaser.containers[particle.BindContainerIndex]);
                particle.DrawSprites(sLeaser.containers[particle.BindContainerIndex], smoothPos, rCam, timeStacker, camPos);
            }
        }

        #region ApplyModule

        public KuyoParticleEmitter AppendInitModule(IParticleInitModule module)
        {
            particleSpawnList.Add(module);
            return this;
        }


        public KuyoParticleEmitter AppendUpdateModule(IParticleUpdateModule module)
        {
            particleUpdateList.Add(module);
            if (module is IParticleNeedInitUpdateModule a)
                particleNeedInitUpdateList.Add(a);
            return this;
        }

        public KuyoParticleEmitter AppendDieModule(IParticleDieModule module)
        {
            particleDieList.Add(module);
            return this;
        }




        public KuyoParticleEmitter AppendEmitterModule(ModifyEmitterDelegate deg)
        {
            emitterUpdateList.Add(new DegEmitterModule(deg));
            return this;
        }

        public KuyoParticleEmitter AppendInitModule(ModifyParticleDelegate deg)
        {
            particleSpawnList.Add(new DegParticleModule(deg));
            return this;
        }


        public KuyoParticleEmitter AppendUpdateModule(ModifyParticleDelegate deg)
        {
            particleUpdateList.Add(new DegParticleModule(deg));
            return this;
        }

        public KuyoParticleEmitter AppendDieModule(ModifyParticleDelegate deg)
        {
            particleDieList.Add(new DegParticleModule(deg));
            return this;
        }


        public KuyoParticleEmitter AppendEmitterModule(IEmitterModule module)
        {
            emitterUpdateList.Add(module);
            return this;
        }

        #endregion


        public void Die(bool forceDelete = false)
        {
            if (forceDelete || forceDie)
                Destroy();
            else
                waitForDie = true;

        }

        public void SpawnParticle()
        {
            var newIndex = freeContainerIndex.Any() ? freeContainerIndex.Dequeue() : particles.Count;
            maxLength = Mathf.Max(maxLength, newIndex+1);
            var newParticle = (freeParticlesQueue.Any() ? freeParticlesQueue.Dequeue() : createParticle(this)).Reset(room, pos, newIndex);
            foreach (var module in particleSpawnList)
                module.ParticleFunction(newParticle);
           
            newParticle.AfterInit();

            foreach (var module in particleNeedInitUpdateList)
                module.ParticleFunction(newParticle);

            particles.Add(newParticle);
        }



        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!hasInitCheck)
            {
                hasInitCheck = true;
                if (emitterUpdateList.All(i => i is not IEmitterSpawnModule))
                    throw new EmitterException("At least a emitter spawn module is required.");
                
                if(particleSpawnList.All(i => i is not LifeModule))
                    throw new EmitterException("At least a life module is required.");

            }
            if (!Paused)
            {
                LastLifeTime = LifeTime;
                LifeTime += 1 / 40f / emitterMaxTime;
                TimeCounter++;

                foreach (var module in emitterUpdateList)
                {
                    if (module.UpdateWhenDie || !waitForDie)
                        module.UpdateEmitter(this);
                }

                if (waitForDie && particles.Count == 0)
                {
                    Destroy();
                }
            }
            else
            {
                foreach (var module in emitterUpdateList)
                {
                    module.UpdateEmitter(this);
                }
            }

            for (int i = particles.Count-1; i>=0 ;i--)
            {
                var particle = particles[i];
                particle.Update();
                foreach (var module in particleUpdateList)
                    module.ParticleFunction(particle);
                if (particle.life >= 1)
                {
                    foreach (var module in particleDieList)
                        module.ParticleFunction(particle);

                    //可能发生二次更新
                    if (particle.life >= 1)
                    {
                        freeParticlesQueue.Enqueue(particle);
                        needCleanContainer.Add(particle.BindContainerIndex);
                        freeContainerIndex.Enqueue(particle.BindContainerIndex);
                        particles.RemoveAt(i);
                    }
                }

            }

            if (LifeTime > 1 && !isInf)
            {
                if (loop)
                {
                    LifeTime = 0;
                    TimeCounter = 0;
                }
                else if(!forceDie)
                    waitForDie = true;
                else
                    Destroy();
            }
        }

        public delegate void ModifyParticleDelegate(SimpleParticle particle);
        public delegate void ModifyEmitterDelegate(KuyoParticleEmitter emitter);

        public delegate SimpleParticle CreateParticleDelegate(KuyoParticleEmitter emitter);


        public int extendLength = 10;

        public float LifeTime { get; private set; }
        public float LastLifeTime { get; private set; }


        public int TimeCounter { get; private set; }

        public bool Paused { get; set; }

        private int maxLength;

        private int containerIndex = -1;

        private bool waitForDie;

        private readonly bool isInf;

        private readonly bool loop;

        private readonly bool forceDie;

        private readonly float emitterMaxTime;

        private readonly string container;

        private bool hasInitCheck = false;



        private readonly SimpleParticle createPlaceHolder;

        private readonly CreateParticleDelegate createParticle;


        public readonly List<IEmitterModule> emitterUpdateList = new();

        public readonly List<IParticleModule> particleUpdateList = new();
        public readonly List<IParticleModule> particleDieList = new();
        public readonly List<IParticleModule> particleSpawnList = new();
        public readonly List<IParticleNeedInitUpdateModule> particleNeedInitUpdateList = new();


        public readonly List<SimpleParticle> particles = new();
        private readonly Queue<SimpleParticle> freeParticlesQueue = new();

        private readonly List<int> needCleanContainer = new();

        private readonly Queue<int> freeContainerIndex = new();

        private readonly Dictionary<string, object> customObjects = new();

        public bool TryGetData<T>(string name, out T data)
        {
            var re = customObjects.TryGetValue(name, out var r);
            data = (T)r;
            return re;
        }

        public T GetData<T>(string name, T defaultValue = default)
        {
            if(customObjects.TryGetValue(name, out var data))
                return (T)data;
            return defaultValue;
        }

        public void SetData<T>(string name, T data)
        {
            
            customObjects[name]= data;
        }

    }



    public class SimpleParticle
    {

        public class SpriteData
        {
            public float scaleX = 1;
            public float scaleY = 1;
            public Color color = Color.white;

            public SpriteData(float scaleX, float scaleY, Color? color = null)
            {
                this.scaleX = scaleX;
                this.scaleY = scaleY;
                this.color = color ?? Color.white;
            }
            public SpriteData(float scale, Color? color = null)
            {
                this.scaleX = scale;
                this.scaleY = scale;
                this.color = color ?? Color.white;
            }

            public SpriteData(Color color)
            {
                this.color = color;
            }
            public SpriteData() { }
        }

        public static KuyoParticleEmitter.CreateParticleDelegate SimplyParticle(int count) => (emitter) => new(emitter,count);

        public static KuyoParticleEmitter.CreateParticleDelegate DefaultParticle = (emitter) => new SimpleParticle(emitter, 1);
        public readonly KuyoParticleEmitter emitter;
        public SimpleParticle(KuyoParticleEmitter emitter,int spriteCount)
        {
            this.emitter = emitter;
            this.spriteCount = spriteCount;

            elements = new FAtlasElement[spriteCount];
            shaders = new FShader[spriteCount];
        }
        public readonly int spriteCount = 1;

        public Vector2 AbstractPos
        {
            get => isLocal ? pos + emitter.pos : pos + initPos;
            set
            {
                if(isLocal)
                    pos = value - emitter.pos;
                else 
                    pos = value - initPos;
            }
        }

        public Vector2 LastAbstractPos
        {
            get => isLocal ? lastPos + emitter.lastPos : lastPos + initPos;
            set
            {
                if(isLocal)
                    lastPos = value - emitter.lastPos;
                else 
                    lastPos = value - initPos;
            }
        }

        public int BindContainerIndex { get; private set; }

        public int RandomSeed { get; private set; }

        public XorShift32 RandModule { get; private set; }

        public virtual void InitiateSprites(RoomCamera rCam, FContainer container)
        {
            for (int i = 0; i < spriteCount; i++)
                container.AddChild(new FSprite("Futile_White"));
        }

        public virtual SimpleParticle Reset(Room newRoom, Vector2 initPos, int index)
        {
            counter = 0;
            isLocal = false;
            BindContainerIndex = index;

            Element = Futile.atlasManager.GetElementWithName("Futile_White");
            Shader = FShader.defaultShader;
            spriteDatas = new SpriteData[spriteCount];

            maxLife = 1f;
            lastLife = life = 0f;
            lastScale = scale = Vector2.one;
            lastPos = pos = Vector2.zero;
            lastRotation = rotation = 0f;
            lastColor = color = Color.white;
            vel = Vector2.zero;
            this.initPos = initPos;
            RandomSeed = DateTime.Now.Ticks.GetHashCode() + BindContainerIndex;
            RandModule = new XorShift32((uint)RandomSeed);
            room = newRoom;
            customObjects.Clear();
            return this;
        }

        public virtual void AfterInit()
        {
            initColor = color;
            initScale = scale;
        }
        

        public virtual void Update()
        {
            counter++;
            lastPos = pos;
            lastLife = life;
            lastRotation = rotation;
            lastScale = scale;
            lastColor = color;
            pos += vel;
            life += 1 / 40f / maxLife;
        }

        public virtual void Die()
        {
            life = 1f;
        }

        public virtual float CollisionRadius => scale.x;
        public virtual float LastCollisionRadius => scale.x;


        public float maxLife = 1f;

        public FAtlasElement Element
        {
            get => elements[0];
            set => elements[0] = value;
        }

        public FShader Shader
        {
            get => shaders[0];
            set => shaders[0] = value;
        }

        public FAtlasElement[] elements;
        public FShader[] shaders;

        public SpriteData[] spriteDatas;

        public static readonly SpriteData DefaultSpriteData = new();

        public float life;
        public float lastLife;

        public Vector2 scale;
        public Vector2 lastScale;

        public Vector2 pos;
        public Vector2 lastPos;

        public float rotation;
        public float lastRotation;

        public Color color;
        public Color lastColor;

        public Vector2 vel;


        public Vector2 initScale;
        public Color initColor;
        private Vector2 initPos;
        
        public Room room = null;

        public Vector2 InitPos => initPos;



        public int counter;

        public bool isLocal;


        protected readonly Dictionary<string, object> customObjects = new();


        public bool TryGetData<T>(string name, out T data)
        {
            var re = customObjects.TryGetValue(name, out var r);
            data = (T)r;
            return re;
        }

        public T GetData<T>(string name, T defaultValue = default)
        {
            if(customObjects.TryGetValue(name, out var data))
                return (T)data;
            return defaultValue;
        }

        public void SetData<T>(string name, T data)
        {
            customObjects[name]= data;
        }

        public virtual void DrawSprites(FContainer container, Vector2 centerPos, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            for (int i = 0; i < spriteCount; i++)
            {
                var sprite = container._childNodes[i] as FSprite;
                var element = elements[i] ?? Element;
                var shader = shaders[i] ?? Shader;
                if(sprite!.shader != shader)
                    sprite.shader = shader;
                if(sprite.element != element)
                    sprite.element = element;

                var data = spriteDatas[i] ?? DefaultSpriteData;

                sprite.color = Color.Lerp(lastColor, color, timeStacker) * data.color.CloneWithNewAlpha(1);
                sprite.alpha = Color.Lerp(lastColor, color, timeStacker).a * data.color.a;
                sprite.scaleX = data.scaleX;
                sprite.scaleY = data.scaleY;

            }


            container.SetPosition(
                Vector2.Lerp(lastPos, pos, timeStacker) + (isLocal ? centerPos : initPos) - camPos);
            container.scaleX = Vector2.Lerp(lastScale, scale, timeStacker).x;
            container.scaleY = Vector2.Lerp(lastScale, scale, timeStacker).y;
            container.rotation = Mathf.Lerp(lastRotation, rotation, timeStacker);
        }

  

    }
}
