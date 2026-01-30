using Newtonsoft.Json.Linq;
using RWCustom;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.ParticleSystem
{
    


    public interface IParticleValue<out TValueType>
    {
        TValueType GetValue(float time, XorShift32? rnd);
    }


    #region Float

    public class ConstFloat : IParticleValue<float>
    {
        public static implicit operator ConstFloat(float b) => new(b);


        private float value;
        public ConstFloat(float f) => value = f;
        public float GetValue(float time, XorShift32? rnd) => value;

    }

    public class UniformFloat : IParticleValue<float>
    {
        private readonly float min;
        private readonly float max;
        public UniformFloat(float min, float max) => (this.min, this.max) = (min, max);

        public float GetValue(float time, XorShift32? rnd)
        {
            if (rnd != null)
            {
                var re = rnd.Value.Range(min, max);
                return re;
            }
            return Random.Range(min, max);
        }

    }

    public class ConstCurveFloat : IParticleValue<float>
    {
        private readonly Func<float, float> curveOutput;
        public ConstCurveFloat(Func<float, float> curveOutput) => this.curveOutput = curveOutput;
        public float GetValue(float time, XorShift32? rnd)
        {
            return curveOutput(time);
        }
    }

    public class UniformCurveFloat : IParticleValue<float>
    {
        private readonly Func<float, float> curveOutputA;
        private readonly Func<float, float> curveOutputB;

        public UniformCurveFloat(Func<float, float> curveOutputA, Func<float, float> curveOutputB)
            => (this.curveOutputA, this.curveOutputB) = (curveOutputA, curveOutputB);
        public float GetValue(float time, XorShift32? rnd)
        {
            if (rnd != null)
            {      
                var re = Mathf.Lerp(curveOutputA(time), curveOutputB(time), rnd.Value.NextFloat01());
                return re;
            }
            return Mathf.Lerp(curveOutputA(time), curveOutputB(time), Random.value);
        }
    }

    #endregion


    #region Vector2

    public class ConstVector2 : IParticleValue<Vector2>
    {
        public static implicit operator ConstVector2(Vector2 b) => new(b);

        private Vector2 value;
        public ConstVector2(Vector2 f) => value = f;
        public Vector2 GetValue(float time, XorShift32? rnd) => value;

    }

    public class UniformVector2 : IParticleValue<Vector2>
    {
        private readonly Vector2 min;
        private readonly Vector2 max;
        private readonly bool dirRandom;
        public UniformVector2(Vector2 min, Vector2 max, bool dirRandom = false) => 
            (this.min, this.max, this.dirRandom) = (min, max, dirRandom);

        public Vector2 GetValue(float time, XorShift32? rnd)
        {
            if (rnd != null)
            {
          
                Vector2 re = dirRandom
                    ? Vector3.Slerp(min, max, rnd.Value.NextFloat01()) * rnd.Value.Range(min.magnitude, max.magnitude)
                    : Helper.RandomRange(min, max, rnd.Value);
                return re;
            }
            return dirRandom
                    ? Vector3.Slerp(min, max, Random.value) * Random.Range(min.magnitude, max.magnitude)
                    : Helper.RandomRange(min, max);
        }

    }

    public class ConstCurveVector2 : IParticleValue<Vector2>
    {
        private readonly Func<float, Vector2> curveOutput;
        public ConstCurveVector2(Func<float, Vector2> curveOutput) => this.curveOutput = curveOutput;
        public Vector2 GetValue(float time, XorShift32? rnd)
        {
            return curveOutput(time);
        }
    }

    public class UniformCurveVector2 : IParticleValue<Vector2>
    {
        private readonly Func<float, Vector2> curveOutputA;
        private readonly Func<float, Vector2> curveOutputB;

        public UniformCurveVector2(Func<float, Vector2> curveOutputA, Func<float, Vector2> curveOutputB)
            => (this.curveOutputA, this.curveOutputB) = (curveOutputA, curveOutputB);
        public Vector2 GetValue(float time, XorShift32? rnd)
        {
            if (rnd != null)
            {
                var re = Vector2.Lerp(curveOutputA(time), curveOutputB(time), rnd.Value.NextFloat01());
                return re;
            }
            return Vector2.Lerp(curveOutputA(time), curveOutputB(time), Random.value);
        }
    }

    #endregion


    #region Color

    public class ConstColor : IParticleValue<Color>
    {
        public static implicit operator ConstColor(Color b) => new(b);

        private readonly Color value;
        public ConstColor(Color f) => value = f;
        public Color GetValue(float time, XorShift32? rnd) => value;

    }

    public class UniformColor : IParticleValue<Color>
    {
        private readonly Color min;
        private readonly Color max;
        private readonly bool useLerp;
        public UniformColor(Color min, Color max,bool useLerp = false) => (this.min, this.max,  this.useLerp) = (min, max, useLerp);

        public Color GetValue(float time, XorShift32? rnd)
        {
            if (rnd != null)
            {
                Color re = useLerp ? Color.Lerp(min, max, rnd.Value.NextFloat01()) : Helper.RandomRange(min, max, rnd.Value);
                return re;
            }
            
            return useLerp ? Color.Lerp(min,max,Random.value) : Helper.RandomRange(min, max);
        }

    }

    public class ConstCurveColor : IParticleValue<Color>
    {
        private readonly Func<float, Color> curveOutput;
        public ConstCurveColor(Func<float, Color> curveOutput) => this.curveOutput = curveOutput;
        public Color GetValue(float time, XorShift32? rnd)
        {
            return curveOutput(time);
        }
    }

    public class UniformCurveColor : IParticleValue<Color>
    {
        private readonly Func<float, Color> curveOutputA;
        private readonly Func<float, Color> curveOutputB;


        public UniformCurveColor(Func<float, Color> curveOutputA, Func<float, Color> curveOutputB)
            => (this.curveOutputA, this.curveOutputB) = (curveOutputA, curveOutputB);
        public Color GetValue(float time, XorShift32? rnd)
        {
            if (rnd != null)
            {
                var re = Color.Lerp(curveOutputA(time), curveOutputB(time), rnd.Value.NextFloat01());
                return re;
            }
            return Color.Lerp(curveOutputA(time), curveOutputB(time), Random.value);
        }
    }

    #endregion

    #region Int

      public class ConstInt : IParticleValue<int>
    {
        public static implicit operator ConstInt(int b) => new(b);


        private int value;
        public ConstInt(int f) => value = f;
        public int GetValue(float time, XorShift32? rnd) => value;

    }

    public class UniformInt : IParticleValue<int>
    {
        private readonly int min;
        private readonly int max;
        public UniformInt(int min, int max) => (this.min, this.max) = (min, max);

        public int GetValue(float time, XorShift32? rnd)
        {
            if (rnd != null)
            {
                var re = rnd.Value.Range(min, max);
                return re;
            }
            return Random.Range(min, max);
        }

    }



    #endregion


    public static partial class Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color RandomRange(in Color a, in Color b)
        {
            return new Color(Random.Range(a.r, b.r), Random.Range(a.g, b.g), Random.Range(a.b, b.b),
                Random.Range(a.a, b.a));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color RandomRange(in Color a, in Color b, in XorShift32 rnd)
        {
            return new Color(rnd.Range(a.r, b.r), rnd.Range(a.g, b.g), rnd.Range(a.b, b.b),
                rnd.Range(a.a, b.a));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RandomRange(in Vector2 a, in Vector2 b)
        {
            return new Vector2(Random.Range(a.x, b.x), Random.Range(a.y, b.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RandomRange(in Vector2 a, in Vector2 b, in XorShift32 rnd)
        {
            return new Vector2(rnd.Range(a.x, b.x), rnd.Range(a.y, b.y));
        }
    }

}
