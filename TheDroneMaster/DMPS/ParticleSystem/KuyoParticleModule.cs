using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RWCustom;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.ParticleSystem
{
    public interface IParticleModule
    {
        public void ParticleFunction(SimpleParticle particle);
    }
    public interface IParticleInitModule : IParticleModule { }

    public interface IParticleUpdateModule : IParticleModule { }

    public interface IParticleDieModule : IParticleModule { }

    public interface IParticleNeedInitUpdateModule : IParticleUpdateModule { }

    public abstract class SimpleParModule<TValueType>
    {
        private readonly bool onlyRandAtSpawn;
        private TValueType initValue;
        protected SimpleParModule(bool onlyRandAtSpawn) => this.onlyRandAtSpawn = onlyRandAtSpawn;

        protected TValueType GetValue(SimpleParticle particle, IParticleValue<TValueType> value)
        {
            if(!(onlyRandAtSpawn && particle.counter > 1))
            {
                return initValue = value.GetValue(particle.emitter.LifeTime, particle.RandModule);
            }
            return initValue;
        }

    }


    public class DegParticleModule(KuyoParticleEmitter.ModifyParticleDelegate deg) : IParticleModule
    {
        public void ParticleFunction(SimpleParticle emitter)
        {
            deg(emitter);
        }
    }

    public class ChunksInitPosModule : IParticleInitModule
    {
        private readonly BodyChunk[] chunks;
        private readonly bool autoRad = false;
        private readonly bool onSurface;

        private readonly IParticleValue<float> radValue;
        private readonly IParticleValue<float> vel;

        public ChunksInitPosModule(IParticleValue<float> rad, IParticleValue<float> vel, bool autoRad = true, bool onSurface = false, params BodyChunk[] chunks)
        {
            this.autoRad = autoRad;
            this.radValue = rad;
            this.chunks = chunks;
            this.onSurface = onSurface;
            this.vel = vel;

        }

        public void ParticleFunction(SimpleParticle particle)
        {
            var index = Random.Range(0, chunks.Length);
            particle.pos = chunks[index].pos - particle.emitter.pos;
            var dir = Custom.RNV();
            particle.vel = vel.GetValue(particle.emitter.LifeTime, particle.RandModule) * dir;
            if (autoRad)
            {
                particle.pos += chunks[index].rad * (onSurface ? 1 : Random.value) *
                                radValue.GetValue(particle.emitter.LifeTime, particle.RandModule) * dir;
            }
            else
            {
                particle.pos += radValue.GetValue(particle.emitter.LifeTime, particle.RandModule) * (onSurface ? 1 : Random.value) * dir;
            }
            particle.lastPos = particle.pos;

        }
    }
    public class ChunkConnectionsInitPosModule : IParticleInitModule
    {
        private readonly PhysicalObject.BodyChunkConnection[] connections;
        private readonly bool autoRad = false;
        private readonly bool onSurface;
        private readonly IParticleValue<float> radValue;
        private readonly IParticleValue<float> vel;

        public ChunkConnectionsInitPosModule(IParticleValue<float> rad, IParticleValue<float> vel, bool autoRad = true, bool onSurface = false, params PhysicalObject.BodyChunkConnection[] connections)
        {
            this.autoRad = autoRad;
            this.radValue = rad;
            this.connections = connections;
            this.onSurface = onSurface;
            this.vel = vel;
        }

        public void ParticleFunction(SimpleParticle particle)
        {
            var index = Random.Range(0, connections.Length);
            var lerp = Random.value; 
            particle.pos = Vector2.Lerp(connections[index].chunk1.pos, connections[index].chunk2.pos, lerp) - particle.emitter.pos;
            var dir = Custom.RNV();
            particle.vel = vel.GetValue(particle.emitter.LifeTime, particle.RandModule) * dir;
            if (autoRad)
            {
                particle.pos += Mathf.Lerp(connections[index].chunk1.rad , connections[index].chunk2.rad, lerp) * (onSurface ? 1 : Random.value) *
                                radValue.GetValue(particle.emitter.LifeTime, particle.RandModule) * dir;
            }
            else
            {
                particle.pos += radValue.GetValue(particle.emitter.LifeTime, particle.RandModule) * (onSurface ? 1 : Random.value)  * dir;
            }
            particle.lastPos = particle.pos;
        }
    }

    public class CircleInitPosModule(IParticleValue<float> rad, bool onSurface, IParticleValue<float> vel)
        : IParticleInitModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            var r = rad.GetValue(particle.emitter.LifeTime, particle.RandModule);
            particle.pos = particle.lastPos = (onSurface ? r : Random.Range(0, r)) * Custom.RNV();
            particle.vel = particle.pos.normalized * vel.GetValue(particle.emitter.LifeTime, particle.RandModule);
        }
    }
    public class InitVelocityModule(IParticleValue<Vector2> velocity) : IParticleInitModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.vel = velocity.GetValue(particle.emitter.LifeTime, particle.RandModule);
        }
    }

    public class InitScaleModule(IParticleValue<Vector2> scale, bool quad) : IParticleInitModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            var scale1 = scale.GetValue(particle.emitter.LifeTime, particle.RandModule);
            particle.lastScale.x = particle.scale.x = scale1.x;
            particle.lastScale.y = particle.scale.y = quad ? scale1.x : scale1.y;
        }
    }

    public class InitElementModule : IParticleInitModule
    {
        private readonly FAtlasElement element;
        private readonly FShader shader;
        private readonly int index;
        private readonly SimpleParticle.SpriteData data;

        public InitElementModule(string elementName,string shaderName = null,SimpleParticle.SpriteData spriteData = null, int index = 0)
        {
            element = Futile.atlasManager.GetElementWithName(elementName);
            this.index = index;
            data = spriteData;
            if (shaderName != null && !Custom.rainWorld.Shaders.ContainsKey(shaderName))
            {
                Plugin.LoggerLog($"[Particle System]\tShader Not found : {shaderName}");
                shader = FShader.defaultShader;
                return;
            }
            shader = shaderName == null ? FShader.defaultShader : Custom.rainWorld.Shaders[shaderName];
        }
        public InitElementModule(string elementName, int index)
        {
            element = Futile.atlasManager.GetElementWithName(elementName);
            shader = FShader.defaultShader;
            this.index = index;
        }

        public void ParticleFunction(SimpleParticle particle)
        {
            particle.shaders[index] = shader;
            particle.elements[index] = element;
            particle.spriteDatas[index] = data ?? particle.spriteDatas[index];
        }
    }



    public class InitRotationModule(IParticleValue<float> rotation) : IParticleInitModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.lastRotation = particle.rotation = rotation.GetValue(particle.emitter.LifeTime, particle.RandModule);
        }
    }
    public class InitColorModule(IParticleValue<Color> color) : IParticleInitModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.lastColor = particle.color = color.GetValue(particle.emitter.LifeTime, particle.RandModule);
        }
    }

    public class LifeModule(IParticleValue<float> life) : IParticleInitModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.maxLife = life.GetValue(particle.emitter.LifeTime, particle.RandModule);

        }
    }
    public class ScaleRatioOverLifeModule(IParticleValue<Vector2> scale, bool quad, bool onlyRandAtSpawn = false)
        : SimpleParModule<Vector2>(onlyRandAtSpawn), IParticleNeedInitUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            var scale1 = GetValue(particle, scale);
            particle.scale.x = scale1.x * particle.initScale.x;
            particle.scale.y = quad ? scale1.x * particle.initScale.x : scale1.y * particle.initScale.y;
        }
    }

    public class ColorRatioOverLifeModule(IParticleValue<Color> color, bool onlyRandAtSpawn = false)
        : SimpleParModule<Color>(onlyRandAtSpawn), IParticleNeedInitUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.color = GetValue(particle, color) * particle.initColor;
        }
    }

    public class AccelerationModule(IParticleValue<Vector2> force, bool onlyRandAtSpawn = false)
        : SimpleParModule<Vector2>(onlyRandAtSpawn), IParticleUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.vel += GetValue(particle, force);

        }
    }
    public class ResistanceModule(IParticleValue<float> resistance, bool onlyRandAtSpawn = false)
        : SimpleParModule<float>(onlyRandAtSpawn), IParticleUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.vel *= GetValue(particle, resistance);

        }
    }

    public class LockRotationWithVel : IParticleUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.rotation = Custom.VecToDeg(particle.vel);
        }
    }

    public class ScaleRatioOverSpeed(Vector2 maxScale, Vector2 scaleMulti) : IParticleUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.scale = new Vector2(1, 1) + scaleMulti * particle.vel.magnitude;
            particle.scale.x = Mathf.Min(particle.scale.x, maxScale.x);
            particle.scale.y = Mathf.Min(particle.scale.y, maxScale.y);

        }
    }
    public class RotationSpeedModule(IParticleValue<float> speed, bool onlyRandAtSpawn = true)
        : SimpleParModule<float>(onlyRandAtSpawn), IParticleUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            particle.rotation += GetValue(particle, speed);
        }
    }
    
    
    public class CollisionTerrainModule(float bonus = 0.8f) : IParticleUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            if (particle.room.GetTile(particle.AbstractPos).Solid && !particle.room.GetTile(particle.LastAbstractPos).Solid)
            {
                IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(particle.room, 
                    particle.room.GetTilePosition(particle.LastAbstractPos), particle.room.GetTilePosition(particle.AbstractPos));
                FloatRect floatRect = Custom.RectCollision(particle.AbstractPos, particle.LastAbstractPos, particle.room.TileRect(intVector!.Value).Grow(2f));
                particle.AbstractPos = floatRect.GetCorner(FloatRect.CornerLabel.D);
                if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                    particle.vel.x = Mathf.Abs(particle.vel.x) * bonus;
                
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                    particle.vel.x = -Mathf.Abs(particle.vel.x) * bonus;
                
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                    particle.vel.y = Mathf.Abs(particle.vel.y) * bonus;
                
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                    particle.vel.y = -Mathf.Abs(particle.vel.y) * bonus;
                
            }

        }
    }

    
    
    
    public class UpdateParticleCollisionModule(IParticleValue<int> updateInterval, bool onlyRandAtSpawn = true) : SimpleParModule<int>(onlyRandAtSpawn), IParticleUpdateModule
    {
        private CollisionObjectModule collisionModule;
        private bool isInited = false;
     
        public void ParticleFunction(SimpleParticle particle)
        {
            
            if (!isInited)
            {
                collisionModule = particle.emitter.emitterUpdateList.FirstOrDefault(i => i is CollisionObjectModule) as CollisionObjectModule;
                if(collisionModule == null) 
                    throw new EmitterException("A CollisionObjectModule is required for UpdateParticleCollisionModule.");
                isInited = true;
            }

            if (particle.counter % GetValue(particle, updateInterval) == 0)
            {
                collisionModule.UpdateParticle(particle);
   
            }
        }
    }

    public class RemoveParticleCollisionModule : IParticleDieModule
    {
        private CollisionObjectModule collisionModule;
        private bool isInited = false;
        public void ParticleFunction(SimpleParticle particle)
        {
            if (!isInited)
            {
                collisionModule = particle.emitter.emitterUpdateList.FirstOrDefault(i => i is CollisionObjectModule) as CollisionObjectModule;
                if(collisionModule == null) 
                    throw new EmitterException("A CollisionObjectModule is required for RemoveParticleCollisionModule.");
                isInited = true;
            }
            collisionModule.RemoveParticle(particle);
        }
    }
    

    public class DelayInitModule(float startLife, IParticleInitModule module) : IParticleUpdateModule
    {
        private readonly string paramName = module.GetHashCode().ToString();
        public void ParticleFunction(SimpleParticle particle)
        {
            if (!particle.GetData(paramName, false) && particle.life > startLife)
            {
                particle.SetData(paramName, true);
                module.ParticleFunction(particle);
            }
        }
    }
    
    public class ModuleLifeControlModule(IParticleUpdateModule module, float startLife,float toLife) : IParticleUpdateModule
    {
        public void ParticleFunction(SimpleParticle particle)
        {
            if(particle.life > startLife && particle.life < toLife)
                module.ParticleFunction(particle);
        }
    }


}



