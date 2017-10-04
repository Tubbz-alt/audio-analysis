﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Acoustics.Shared.Extensions
{
    public static class RandomExtensions
    {
        public static Guid NextGuid(this Random random)
        {
            var uuid = new byte[16];
            random.NextBytes(uuid);

            return new Guid(uuid);
        }

        public static int NextInSequence(this Random random, int minimum, int maximum, int step)
        {
            var steps = (maximum - minimum) / step;
            return minimum + (random.Next(0, steps) * step);
        }

        public static long NextInSequence(this Random random, long minimum, long maximum, long step)
        {
            var steps = (maximum - minimum) / step;
            return minimum + (random.NextLong(0, steps) * step);
        }

        public static T NextChoice<T>(this Random random, params T[] choices)
        {
            return choices[random.Next(0, choices.Length)];
        }

        public static long NextLong(this Random random)
        {
            return NextLong(random, long.MinValue, long.MaxValue);
        }

        public static long NextLong(this Random random, long min, long max)
        {
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }

        public static DateTimeOffset NextDate(this Random random, DateTimeOffset? minimum = null, DateTimeOffset? maximum = null)
        {
            minimum = minimum ?? DateTimeOffset.MinValue;
            maximum = maximum ?? DateTimeOffset.MaxValue;

            var randomTick = random.NextLong(minimum.Value.Ticks, maximum.Value.Ticks);

            return new DateTimeOffset(randomTick, (TimeSpan)minimum?.Offset);
        }

        public static Range<double> NextRange(this Random random, double min = 0, double max = 1.0)
        {
            var delta = max - min;
            var a = (random.NextDouble() * delta) + min;
            var b = (random.NextDouble() * delta) + min;

            if (a < b)
            {
                return new Range<double>(a, b);
            }
            else
            {
                return new Range<double>(b, a);
            }
        }
    }
}
