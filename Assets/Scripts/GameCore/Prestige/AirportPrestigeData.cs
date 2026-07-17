using System;

namespace IdleAirport.GameCore.Prestige
{
    [Serializable]
    public sealed class AirportPrestigeMultiplierConfig
    {
        public double BaseMultiplier = 1d;
        public double MultiplierPerPrestigeLevel = 2d;

        public double CalculateMultiplier(int prestigeCount)
        {
            return Math.Max(1d, BaseMultiplier) * Math.Pow(Math.Max(1d, MultiplierPerPrestigeLevel), Math.Max(0, prestigeCount));
        }

        public void Validate()
        {
            BaseMultiplier = Math.Max(1d, BaseMultiplier);
            MultiplierPerPrestigeLevel = Math.Max(1d, MultiplierPerPrestigeLevel);
        }
    }

    public static class AirportPrestigeRewardCalculator
    {
        public static double CalculateFinalPassengerReward(double basePassengerReward, double permanentPrestigeMultiplier)
        {
            if (basePassengerReward < 0d)
                throw new ArgumentOutOfRangeException(nameof(basePassengerReward));

            return basePassengerReward * Math.Max(1d, permanentPrestigeMultiplier);
        }
    }

    [Serializable]
    public sealed class AirportPrestigeData
    {
        public int PrestigeCount;
        public int PassportsScannedThisRun;
        public double GlobalPrestigeMultiplier = 1d;
    }
}