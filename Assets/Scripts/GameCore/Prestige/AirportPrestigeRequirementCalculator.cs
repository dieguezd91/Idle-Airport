using System;
using UnityEngine;

namespace IdleAirport.GameCore.Prestige
{
    public static class AirportPrestigeRequirementCalculator
    {
        public static int CalculateRequiredPassports(
            int baseRequiredPassports,
            float growthMultiplier,
            int roundStep,
            int prestigeCount)
        {
            int safeBase = Mathf.Max(1, baseRequiredPassports);
            float safeGrowth = Mathf.Max(1f, growthMultiplier);
            int safeRoundStep = Mathf.Max(1, roundStep);
            int safePrestigeCount = Mathf.Max(0, prestigeCount);

            double rawRequired = safeBase * Math.Pow(safeGrowth, safePrestigeCount);
            int roundedRequired = (int)(Math.Ceiling(rawRequired / safeRoundStep) * safeRoundStep);
            return Mathf.Max(1, roundedRequired);
        }
    }
}