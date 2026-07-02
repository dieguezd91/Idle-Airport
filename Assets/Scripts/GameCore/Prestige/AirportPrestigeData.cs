using System;

namespace IdleAirport.GameCore.Prestige
{
    [Serializable]
    public sealed class AirportPrestigeData
    {
        public int PrestigeCount;
        public int PassportsScannedThisRun;
        public double GlobalPrestigeMultiplier = 1d;
    }
}