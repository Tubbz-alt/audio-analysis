﻿// <copyright file="RangeTests.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Acoustics.Shared
{
    public static class RangeExtensions
    {
        /// <summary>
        /// Greedily grow the current range to the new duration without exceeding the limits.
        /// Will grow the range around the center of the current range if possible.
        /// Will not fail, and will return as much range as possible, without exceeding limits.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="limits"></param>
        /// <param name="growAmount"></param>
        /// <param name="roundDigits"></param>
        /// <returns></returns>
        public static Range<double> Grow(
            this Range<double> range,
            Range<double> limits,
            double growAmount,
            int? roundDigits = null)
        {
            var limitMagnitude = limits.Size();

            if (growAmount + range.Size() > limitMagnitude)
            {
                return limits;
            }

            var halfGrow = (growAmount / 2.0);

            var newMin = range.Minimum - halfGrow;

            // round the lower bound
            if (roundDigits.HasValue)
            {
                newMin = Math.Round(newMin, roundDigits.Value, MidpointRounding.AwayFromZero);
            }

            var newMax = range.Maximum + halfGrow;

            // round the upper bound
            if (roundDigits.HasValue)
            {
                newMax = Math.Round(newMax, roundDigits.Value, MidpointRounding.AwayFromZero);
            }

            if (newMin < limits.Minimum)
            {
                newMax += limits.Minimum - newMin;
                newMin = limits.Minimum;
            }
            else if (newMax > limits.Maximum)
            {
                newMin -= newMax - limits.Maximum;
                newMax = limits.Maximum;
            }

            return new Range<double>(newMin, newMax);
        }

        public static double Center(this Range<double> range)
        {
            return range.Minimum + (range.Size() / 2.0);
        }

        public static TimeSpan Center(this Range<TimeSpan> range)
        {
            return range.Minimum.Add(range.Size().Divide(2.0));
        }

        public static double Size(this Range<double> range)
        {
            return Math.Abs(range.Maximum - range.Minimum);
        }

        public static TimeSpan Size(this Range<TimeSpan> range)
        {
            return range.Maximum.Subtract(range.Minimum);
        }

        public static Range<double> Shift(this Range<double> range, double shift)
        {
            return new Range<double>(range.Minimum + shift, range.Maximum + shift);
        }

        public static Range<TimeSpan> Shift(this Range<TimeSpan> range, TimeSpan shift)
        {
            return new Range<TimeSpan>(range.Minimum.Add(shift), range.Maximum.Add(shift));
        }

        public static Range<double> Add(this Range<double> rangeA, Range<double> rangeB)
        {
            // https://en.wikipedia.org/wiki/Interval_arithmetic

            return new Range<double>(
                rangeA.Minimum + rangeB.Minimum,
                rangeA.Maximum + rangeB.Maximum);
        }

        public static Range<double> Subtract(this Range<double> rangeA, Range<double> rangeB)
        {
            // https://en.wikipedia.org/wiki/Interval_arithmetic

            return new Range<double>(
                rangeA.Minimum - rangeB.Maximum,
                rangeA.Maximum - rangeB.Minimum);
        }

        public static Range<double> Multiply(this Range<double> rangeA, Range<double> rangeB)
        {
            // https://en.wikipedia.org/wiki/Interval_arithmetic
            double
                a1b1 = rangeA.Minimum * rangeB.Minimum,
                a1b2 = rangeA.Minimum * rangeB.Maximum,
                a2b1 = rangeA.Maximum * rangeB.Minimum,
                a2b2 = rangeA.Maximum * rangeB.Maximum;

            return new Range<double>(
                Maths.Min(a1b1, a1b2, a2b1, a2b2),
                Maths.Max(a1b1, a1b2, a2b1, a2b2));
        }

        public static Range<double> Divide(this Range<double> rangeA, Range<double> rangeB)
        {
            // https://en.wikipedia.org/wiki/Interval_arithmetic
           
            return rangeA.Multiply(rangeB.Invert());
        }

        public static Range<double> Invert(this Range<double> range)
        {
            // https://en.wikipedia.org/wiki/Interval_arithmetic
            if (range.Maximum == 0)
            {
                return new Range<double>(double.NegativeInfinity, 1 / range.Minimum);
            }

            if (range.Minimum == 0)
            {
                return new Range<double>(1 / range.Maximum, double.PositiveInfinity);
            }

            return new Range<double>(1 / range.Maximum, 1 / range.Minimum);
        }

        public static Range<T> AsRange<T>(this (T Minimum, T Maximum) pair)
            where T : struct, IComparable<T>
        {
            return new Range<T>(pair.Minimum, pair.Maximum);
        }

        public static Range<T> To<T>(this T Minimum, T Maximum)
            where T : struct, IComparable<T>
        {
            return new Range<T>(Minimum, Maximum);
        }

        public static Range<T> AsRangeFromZero<T>(this T maximum)
            where T : struct, IComparable<T>
        {
            return new Range<T>(default(T), maximum);
        }
    }
}
