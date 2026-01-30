using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.ParticleSystem
{
    public struct XorShift128Plus
    {
        private ulong _s0;
        private ulong _s1;

        public XorShift128Plus(ulong seed)
        {
            _s0 = SplitMix64(ref seed);
            _s1 = SplitMix64(ref seed);
            if ((_s0 | _s1) == 0) _s1 = 0x9E3779B97F4A7C15UL; // 防止全 0
        }

        public ulong NextULong()
        {
            ulong s1 = _s0;
            ulong s0 = _s1;
            _s0 = s0;
            s1 ^= s1 << 23;
            _s1 = s1 ^ s0 ^ (s1 >> 17) ^ (s0 >> 26);
            return _s1 + s0;
        }

        public double NextDouble01()
        {
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        public float NextFloat01()
        {
            return (float)NextDouble01();
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat01();
        }

        private static ulong SplitMix64(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public struct XorShift32
    {
        // 不能为 0
        private uint _state;

        public XorShift32(uint seed)
        {
            _state = seed != 0 ? seed : 0x6D2B79F5u;
        }

        /// <summary>返回 [0, 2^32) 的 uint</summary>
        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>返回 [0, 1) 的 float（24bit 精度）</summary>
        public float NextFloat01()
        {
            // 取高 24 位映射到 [0,1)
            return (NextUInt() >> 8) * (1f / 16777216f); // 2^24
        }

        /// <summary>返回 [min, max) 的 float</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat01();
        }

        /// <summary>返回 [min, max) 的 int</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            uint span = (uint)(max - min);
            return min + (int)(NextUInt() % span);
        }

        public Vector2 InsideUnitCircle()
        {
            while (true)
            {
                float x = Range(-1f, 1f);
                float y = Range(-1f, 1f);
                float r2 = x * x + y * y;
                if (r2 <= 1f) return new Vector2(x, y);
            }
        }
    }
}
