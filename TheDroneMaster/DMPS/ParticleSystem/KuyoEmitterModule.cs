
using System;
using System.Collections.Generic;
using System.Linq;
using RWCustom;
using Unity.Mathematics;
using UnityEngine;

namespace TheDroneMaster.DMPS.ParticleSystem
{
    public interface IEmitterModule
    {
        public void UpdateEmitter(KuyoParticleEmitter emitter);

        public bool UpdateWhenDie { get; }
        public bool UpdateWhenPaused { get; }
    }
    

    public interface IEmitterSpawnModule;

    public class SpawnModule(IParticleValue<float> spawnRate) : IEmitterModule, IEmitterSpawnModule
    {
        private float timeCost;

        public void UpdateEmitter(KuyoParticleEmitter emitter)
        {
            timeCost += 1 / 40f;
            if (timeCost > 1 / spawnRate.GetValue(emitter.LifeTime, null))
            {
                timeCost = 0;
                emitter.SpawnParticle();
            }

        }

        public bool UpdateWhenDie => false;
        public bool UpdateWhenPaused => false;
    }
    public class SpawnModuleByLength(IParticleValue<float> spawnRate) : IEmitterModule, IEmitterSpawnModule
    {
        private float length;

        public void UpdateEmitter(KuyoParticleEmitter emitter)
        {
            length += (emitter.lastPos - emitter.pos).magnitude;
            if (length > spawnRate.GetValue(emitter.LifeTime, null))
            {
                length = 0;
                emitter.SpawnParticle();
            }
        }
        public bool UpdateWhenDie => false;
        public bool UpdateWhenPaused => false;
    }
    public class SpawnModuleByEmitter(Func<KuyoParticleEmitter,float> spawnRateFunc) : IEmitterModule, IEmitterSpawnModule
    {
        private float timeCost;

        public void UpdateEmitter(KuyoParticleEmitter emitter)
        {
            timeCost += 1 / 40f;
            if (timeCost > 1 / spawnRateFunc(emitter))
            {
                timeCost = 0;
                emitter.SpawnParticle();
            }

        }
        public bool UpdateWhenDie => false;
        public bool UpdateWhenPaused => false;
    }

    public class BurstModule : IEmitterModule, IEmitterSpawnModule
    {
        private IParticleValue<float> spawnCount;
        private float timeDelay;
        private bool hasBurst = false;
        public BurstModule(IParticleValue<float> spawnCount,float timeDelay)
        {
            this.spawnCount = spawnCount;
            this.timeDelay = timeDelay;
        }

        public void UpdateEmitter(KuyoParticleEmitter emitter)
        {
            if (!hasBurst && emitter.LifeTime > timeDelay)
            {
                var value = spawnCount.GetValue(emitter.LifeTime, null);
                for (int i = 0; i < value; i++)
                    emitter.SpawnParticle();
                hasBurst = true;
            }
        }
        
        public bool UpdateWhenDie => false;
        public bool UpdateWhenPaused => false;
    }

    public class DegEmitterModule(KuyoParticleEmitter.ModifyEmitterDelegate deg, bool updateWhenDie = false, bool updateWhenPaused = false) : IEmitterModule
    {
        public void UpdateEmitter(KuyoParticleEmitter emitter)
        {
            deg(emitter);
        }
        public bool UpdateWhenDie => updateWhenDie;
        public bool UpdateWhenPaused => updateWhenPaused;
    }

    public class BindPositionModule(PhysicalObject obj,int chunkIndex = 0, bool autoPaused = true, bool updateWhenDie = false, bool updateWhenPaused = false) : IEmitterModule
    {
        
        public void UpdateEmitter(KuyoParticleEmitter emitter)
        {
            if (obj?.room != emitter.room)
            {
                emitter.Die();
                return;
            }

            if (obj is Creature crit && autoPaused)
            {
                emitter.Paused = crit.inShortcut;
            }
            emitter.pos = obj.bodyChunks[chunkIndex].pos;
            emitter.lastPos = obj.bodyChunks[chunkIndex].lastPos;
            emitter.vel = obj.bodyChunks[chunkIndex].vel;
           
        }
        
        public bool UpdateWhenDie => updateWhenDie;
        public bool UpdateWhenPaused => updateWhenPaused;
    }
    
     public class CollisionObjectModule(
        CollisionObjectModule.CollideHandler collideHandler,
        CollisionObjectModule.CollisionObjectOption option = null)
        : IEmitterModule
    {
        public class CollisionObjectOption
        {
            public Func<PhysicalObject,bool> ignoreThisHandler = null;
            
            public bool onlyCreature = true;
            public int collisionInterval = 4;
            public int collisionLayer = -1;
            
            public bool countCollision = false;
            
            public bool simpleCollision = false;
            public bool calcHalfForSimple = false;
            
            public float minRad = 1;
            public bool debugVis = false;
        }
        public delegate void CollideHandler(PhysicalObject obj, int collisionCount);
        public class SinlgTileCollision
        {
            public HashSet<SimpleParticle> particles = new();
        }

        private class CollisionData
        {
            public readonly Dictionary<int, SinlgTileCollision> fullCollisionData = new();
            public readonly Dictionary<int,SinlgTileCollision> halfCollisionData = new();
        }

        private class ParticleData
        {
            public List<int> fullCollisionData = new();
            public List<int> halfCollisionData = new();
        }

        private enum CollisionType
        {
            None,
            Half,
            Full,
        }
        
        private Dictionary<SimpleParticle, ParticleData> allParticleData = new();

        private CollisionData tileCollisionData = new();

        private CollisionObjectOption option = option ?? new CollisionObjectOption();
        
        private HashSet<int> tmpIndex = new();

        public void UpdateParticle(SimpleParticle particle)
        {
    
            if (!allParticleData.TryGetValue(particle, out var data))
                allParticleData.Add(particle, data = new());
            
            if (particle.CollisionRadius < option.minRad && (
                    data.fullCollisionData.Count >0 ||
                    data.halfCollisionData.Count >0))
            {
                RemoveParticle(particle);
                return;
            }
            tmpIndex.Clear();
            
            var rad = particle.CollisionRadius;
            var absPos = particle.AbstractPos;
            
            var top = absPos.y + rad;
            var bottom = absPos.y - rad;
            var left = absPos.x - rad;
            var right = absPos.x + rad;
            
            IntVector2 start = particle.room.GetTilePosition(new(left, bottom));
            IntVector2 end = particle.room.GetTilePosition(new(right, top));
            

            for(int i = data.fullCollisionData.Count - 1; i >= 0; i--)
            {
                var index = data.fullCollisionData[i];
                tmpIndex.Add(index);
                switch (GetCollision(absPos, index%particle.room.Width, index/particle.room.Width, rad))
                {
                    case CollisionType.Half:
                        AddCollisionData(particle, data, CollisionType.Half, index);
                        RemoveCollisionData(particle, data, CollisionType.Full, index);
                        break;
                    case CollisionType.None:
                        RemoveCollisionData(particle, data, CollisionType.Full, index);
                        break;
                }
            }
            for(int i = data.halfCollisionData.Count - 1; i >= 0; i--)
            {
                var index = data.halfCollisionData[i];
                switch (GetCollision(absPos, index%particle.room.Width, index/particle.room.Width, rad))
                {
                    case CollisionType.Full:
                        AddCollisionData(particle, data, CollisionType.Full, index);
                        RemoveCollisionData(particle, data, CollisionType.Half, index);
                        break;
                    case CollisionType.None:
                        RemoveCollisionData(particle, data, CollisionType.Half, index);
                        break;
                }
            }
            for (int x = start.x; x <= end.x; x++)
            {
                for (int y = start.y; y <= end.y; y++)
                {
                    var index = x + y * particle.room.Width;
                    if(tmpIndex.Contains(index))
                        continue;
                    
                    switch (GetCollision(absPos, x, y, rad))
                    {
                        case CollisionType.Half:
                            AddCollisionData(particle, data, CollisionType.Half, index);
                            break;
                        case CollisionType.Full:
                            AddCollisionData(particle, data, CollisionType.Full, index);
                            break;
                    }
                }
            }
        }
        
        public void RemoveParticle(SimpleParticle particle)
        {
            if (allParticleData.TryGetValue(particle, out var data))
            {
                for(int i = data.halfCollisionData.Count - 1; i >= 0; i--)
                {
                    var p  = data.halfCollisionData[i];
                    RemoveCollisionData(particle, data, CollisionType.Half, p);
                }

                for(int i = data.fullCollisionData.Count - 1; i >= 0; i--)
                {
                    var p = data.fullCollisionData[i];
                    RemoveCollisionData(particle, data, CollisionType.Full, p);
                }
            }
        }

        private static CollisionType GetCollision(Vector2 pos, int x, int y, float radius)
        {
            var inCounter = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if(Custom.DistLess(new Vector2((x+i)*20, (y+j)*20),pos, radius))
                        inCounter++;
                }
            }

            if (inCounter >= 4)
                return CollisionType.Full;

            // 检查圆形是否与矩形相交
            float rectX = x * 20;
            float rectY = y * 20;
            float rectWidth = 20;
            float rectHeight = 20;

            // 计算矩形到圆形的最近点
            float closestX = Mathf.Clamp(pos.x, rectX, rectX + rectWidth);
            float closestY = Mathf.Clamp(pos.y, rectY, rectY + rectHeight);
            float dx = pos.x - closestX;
            float dy = pos.y - closestY;
            float distanceSquared = dx * dx + dy * dy;

            bool intersects = distanceSquared < radius * radius;

            // 根据相交情况返回类型
            if (inCounter > 0 || intersects)
            {
                return inCounter >= 4 ? CollisionType.Full : CollisionType.Half;
            }
            else
            {
                return CollisionType.None;
            }
        }

        private void AddCollisionData(SimpleParticle particle, ParticleData data, CollisionType type, int index)
        {
            if (option.simpleCollision && !option.calcHalfForSimple && type == CollisionType.Half)
                return;
            
            switch (type)
            {
                case CollisionType.Half:
                    if (data.halfCollisionData.Contains(index))
                        break;
                    data.halfCollisionData.Add(index);
                    if(!tileCollisionData.halfCollisionData.TryGetValue(index,out var halfCollisionData))
                        tileCollisionData.halfCollisionData[index] = halfCollisionData = new();
                    halfCollisionData.particles.Add(particle);
                    break;
                case CollisionType.Full:
                    if (data.fullCollisionData.Contains(index))
                        break;
                    data.fullCollisionData.Add(index);
                    if (!tileCollisionData.fullCollisionData.TryGetValue(index, out var fullCollisionData))
                        tileCollisionData.fullCollisionData[index] = fullCollisionData = new();
                    fullCollisionData.particles.Add(particle);
                    break;
            }
        }

        private void RemoveCollisionData(SimpleParticle particle, ParticleData data, CollisionType type, int index)
        {
            switch (type)
            {
                case CollisionType.Half:
                    tileCollisionData.halfCollisionData[index].particles.Remove(particle);
                    if (tileCollisionData.halfCollisionData[index].particles.Count == 0)
                        tileCollisionData.halfCollisionData.Remove(index);
                    data.halfCollisionData.Remove(index);
                    break;
                case CollisionType.Full:
                    tileCollisionData.fullCollisionData[index].particles.Remove(particle);
                    if (tileCollisionData.fullCollisionData[index].particles.Count == 0)
                        tileCollisionData.fullCollisionData.Remove(index);
                    data.fullCollisionData.Remove(index);
                    
                    break;
            }
        }


        private bool isInited = false;
        
        private int intervalCounter = 0;

        private Texture2D debugVisTex;

        public void UpdateEmitter(KuyoParticleEmitter emitter)
        {
            if (!isInited)
            {
                isInited = true;
        
                if(emitter.particleUpdateList.All(i => i is not UpdateParticleCollisionModule)) 
                    throw new EmitterException("A UpdateParticleCollisionModule is required for CollisionObjectModule.");
                if(emitter.particleDieList.All(i => i is not RemoveParticleCollisionModule)) 
                    throw new EmitterException("A RemoveParticleCollisionModule is required for CollisionObjectModule.");

                if (option.debugVis)
                {
                    debugVisTex = new Texture2D(emitter.room.Width, emitter.room.Height);
                    emitter.room.AddObject(new CollisionObjectModuleVis(debugVisTex,emitter));
                }

            }
            
            if (option.debugVis)
            {
                for (int i = 0; i < emitter.room.Width; i++)
                {
                    for (int j = 0; j < emitter.room.Height; j++)
                    {
                        if (tileCollisionData.fullCollisionData.TryGetValue(i + j * emitter.room.Width,out var datg))
                            debugVisTex.SetPixel(i, j, Color.red);
                        else if (tileCollisionData.halfCollisionData.TryGetValue(i + j * emitter.room.Width,out var data1))
                            debugVisTex.SetPixel(i, j,Color.green);
                        else
                            debugVisTex.SetPixel(i, j, Color.black);
                    }
                }
                debugVisTex.Apply(false);

            }
            
            intervalCounter++;
            if (intervalCounter > option.collisionInterval)
            {
                intervalCounter = 0;
        
                var objs =  option.collisionLayer switch
                {
                    -1 when ! option.onlyCreature => emitter.room.physicalObjects.SelectMany(i => i),
                    -1 when  option.onlyCreature => emitter.room.abstractRoom.creatures.Select(i => i.realizedCreature)
                        .Where(i => i != null),
                    > -1 when  option.onlyCreature => emitter.room.physicalObjects[option.collisionLayer].Where(i => i is Creature),
                    > -1 when ! option.onlyCreature => emitter.room.physicalObjects[option.collisionLayer],
                    _ => null
                };
                if (objs == null)
                    throw new ArgumentException($"Invalid collision layer for CollisionObjectModule: {option.collisionLayer}");
                
                foreach (var obj in objs)
                {
                    if( option.ignoreThisHandler != null && option.ignoreThisHandler(obj))
                        continue;
                    
                    if (option.countCollision)
                    {
                        var result = obj.bodyChunks.Sum(i => CollideCount(emitter.room, i.pos, i.rad));
                        if(result != 0)
                            collideHandler(obj,result);
                    }
                    else
                    {
                        if (obj.bodyChunks.Any(i => CollideCheck(emitter.room, i.pos, i.rad)))
                            collideHandler(obj, 1);
                        
                    }
                }
            }

            int CollideCount(Room room, Vector2 pos, float rad)
            {
                var top = pos.y + rad;
                var bottom = pos.y - rad;
                var left = pos.x - rad;
                var right = pos.x + rad;
                IntVector2 start = room.GetTilePosition(new(left, bottom));
                IntVector2 end = room.GetTilePosition(new(right, top));

                int result = 0;

                for (int x = start.x; x <= end.x; x++)
                {
                    for (int y = start.y; y <= end.y; y++)
                    {
                        var index = x + y * room.Width;
                        switch (GetCollision(pos, x, y, rad))
                        {
                            case CollisionType.Full:
                            {
                                if (tileCollisionData.fullCollisionData.TryGetValue(index, out var value))
                                    result += value.particles.Count;
                                if (tileCollisionData.halfCollisionData.TryGetValue(index, out var data))
                                    result += data.particles.Count;
                                break;
                            }
                            case CollisionType.Half:
                            {
                                if (tileCollisionData.fullCollisionData.TryGetValue(index, out var value))
                                    result += value.particles.Count;
                                if (tileCollisionData.halfCollisionData.TryGetValue(index, out var data))
                                {
                                    if (option.simpleCollision && option.calcHalfForSimple)
                                    { 
                                        result += data.particles.Count;
                                        continue;
                                    }
                                    result += data.particles.Count(i =>
                                        Custom.DistLess(i.AbstractPos, pos, rad + i.CollisionRadius));

                                }
                            }
                                break;
                        }
                    }
                }

                return result;
            }
            
            bool CollideCheck(Room room, Vector2 pos, float rad)
            {
                var top = pos.y + rad;
                var bottom = pos.y - rad;
                var left = pos.x - rad;
                var right = pos.x + rad;
                IntVector2 start = room.GetTilePosition(new(left, bottom));
                IntVector2 end = room.GetTilePosition(new(right, top));
                

                for (int x = start.x; x <= end.x; x++)
                {
                    for (int y = start.y; y <= end.y; y++)
                    {
                        var index = x + y * room.Width;
                        switch (GetCollision(pos, x, y, rad))
                        {
                            case CollisionType.Full:
                                if (tileCollisionData.fullCollisionData.ContainsKey(index) ||
                                    tileCollisionData.halfCollisionData.ContainsKey(index))
                                    return true;
                                break;
                            case CollisionType.Half:
                                if(tileCollisionData.fullCollisionData.ContainsKey(index))        
                                    return true;
                                if (tileCollisionData.halfCollisionData.TryGetValue(index,out var data))
                                {
                                    if (option.simpleCollision)
                                    {
                                        return option.calcHalfForSimple;
                                    }
                                    if (data.particles.Any(i =>
                                            Custom.DistLess(i.AbstractPos, pos, rad + i.CollisionRadius)))
                                        return true;
                                }

                                break;
                        }
                    }
                }

                return false;

            }
        }

        public bool UpdateWhenDie => true;
        public bool UpdateWhenPaused => true;
    }
     
    internal class CollisionObjectModuleVis(Texture2D texture, KuyoParticleEmitter emitter) : UpdatableAndDeletable, IDrawable
    {

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FTexture(texture)
            {
                scale = 200f / texture.width,
                anchorX = 0,
                anchorY = 0,
            };
            AddToContainer(sLeaser,rCam,rCam.ReturnFContainer("HUD"));
        }
        
        

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (updateCounter == 1)
            {
                ((FTexture)sLeaser.sprites[0]).SetTexture(texture);
                updateCounter = 0;
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public override void Destroy()
        {
            base.Destroy();
            GameObject.Destroy(texture);
        }

        private int updateCounter = 0;
        public override void Update(bool eu)
        {
            if (emitter.slatedForDeletetion)
            {
                updateCounter = 0;
                Destroy();
            }

            updateCounter++;
            base.Update(eu);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner.AddChild(sLeaser.sprites[0]);
        }
    }
}
