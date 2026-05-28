using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public static class NumberFormatter
    {
        public static string Format(int value)
        {
            return value.ToString("N0", cultureInvariant);
        }

        public static string Format(double value)
        {
            return Math.Round(value).ToString("N0", cultureInvariant);
        }

        public static string Format(double value, int decimals)
        {
            return value.ToString($"F{decimals}", cultureInvariant);
        }

        public static string Format(float value, int decimals = 0)
        {
            if (decimals == 0)
                return Mathf.RoundToInt(value).ToString("N0", cultureInvariant);
            return value.ToString($"F{decimals}", cultureInvariant);
        }

        public static string FormatWithSuffix(double value)
        {
            if (value < 1000)
                return Math.Round(value).ToString(cultureInvariant);

            string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };
            int suffixIndex = 0;
            double displayValue = value;

            while (Math.Abs(displayValue) >= 1000 && suffixIndex < suffixes.Length - 1)
            {
                displayValue /= 1000;
                suffixIndex++;
            }

            return $"{displayValue:F1}{suffixes[suffixIndex]}";
        }

        private static readonly System.Globalization.CultureInfo cultureInvariant =
            System.Globalization.CultureInfo.InvariantCulture;
    }
}