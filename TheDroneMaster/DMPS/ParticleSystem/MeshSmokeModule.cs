using System;
using System.Linq;
using JetBrains.Annotations;
using RWCustom;
using Smoke;
using UnityEngine;

namespace TheDroneMaster.DMPS.ParticleSystem;




public class WindAndDragModule(WindAndDragModule.WindAndDragHandler windAndDragHandler = null)
    : IParticleNeedInitUpdateModule
{
    private bool hasInitialized = false;
    public delegate void WindAndDragHandler(Room room, ref Vector2 vel, Vector2 pos);

    public WindAndDragHandler WindAndDrag { get; } = windAndDragHandler ?? DefaultWindAndDrag;
    public void ParticleFunction(SimpleParticle particle)
    {
        if (!hasInitialized)
        {
            particle.emitter.SetData("WindAndDrag", WindAndDrag);
            hasInitialized = true;
        }

        WindAndDrag(particle.room, ref particle.vel, particle.AbstractPos);
    }

    public static void DefaultWindAndDrag(Room rm, ref Vector2 v, Vector2 p)
    {
        if (v.magnitude > 0f)
        {
            v *= 0.5f + 0.5f / Mathf.Pow(v.magnitude, 0.5f);
        }

        v += SmokeSystem.PerlinWind(p, rm) * 2f;
        if (rm.readyForAI && rm.aimap.getTerrainProximity(p) < 3)
        {
            int terrainProximity = rm.aimap.getTerrainProximity(p);
            Vector2 vector = default(Vector2);
            for (int i = 0; i < 8; i++)
            {
                if (rm.aimap.getTerrainProximity(p + Custom.eightDirections[i].ToVector2() * 20f) > terrainProximity)
                {
                    vector += Custom.eightDirections[i].ToVector2();
                }
            }

            v += Vector2.ClampMagnitude(vector, 1f) * 0.035f;
        }
    }
}

//实现粒子间的速度平滑，根据粒子的“推动力”权重和相互距离，将相邻粒子的速度向它们的平均值调整，从而模拟群体行为或避免碰撞
public class PushParticleVelModule([NotNull] Func<SimpleParticle,float> pushPow) : IEmitterModule
{
    public void UpdateEmitter(KuyoParticleEmitter emitter)
    {
        for (int i = 0; i < emitter.particles.Count; i++)
        {
            var iPushPow = pushPow(emitter.particles[i]);
            if (iPushPow > 0f)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var jPushPow = pushPow(emitter.particles[j]);
                    if (Custom.DistLess(emitter.particles[i].AbstractPos, emitter.particles[j].AbstractPos, 60f))
                    {
                        float pushWeightJ = jPushPow / (iPushPow + jPushPow);
                        Vector2 averageVelocity = (emitter.particles[i].vel + emitter.particles[j].vel) / 2f;
                        float proximityFactor = Mathf.InverseLerp(60f, 30f,
                            Vector2.Distance(emitter.particles[i].AbstractPos, emitter.particles[j].AbstractPos));

                        emitter.particles[i].vel = Vector2.Lerp(emitter.particles[i].vel, averageVelocity,
                            pushWeightJ * proximityFactor);
                        emitter.particles[j].vel = Vector2.Lerp(emitter.particles[j].vel, averageVelocity,
                            (1f - pushWeightJ) * proximityFactor);
                    }
                }
            }
        }
    }

    public bool UpdateWhenDie => false;
    public bool UpdateWhenPaused => false;
}



