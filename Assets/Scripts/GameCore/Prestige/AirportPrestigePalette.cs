using System;
using UnityEngine;

namespace IdleAirport.GameCore.Prestige
{
    // Color bundle for a single airport room driven by the S_Background shader.
    // Each field maps to one S_Background material property. The alpha channel
    // of each color is applied as authored; configuration must keep alpha at 1
    // so room surfaces never become transparent.
    [Serializable]
    public sealed class AirportPrestigeAreaPalette
    {
        public Color BG1;    // _BG1_Color  - primary background
        public Color BG2;    // _BG2_Color  - secondary background
        public Color Wall;   // _WallColor  - wall tint
        public Color Border; // _Border_Color - border tint
    }

    // One prestige tier. Holds a complete S_Background palette for the four
    // main airport rooms. Selector usage: palettes[prestigeCount % palettes.Count].
    [Serializable]
    public sealed class AirportPrestigePalette
    {
        public AirportPrestigeAreaPalette Entrance;
        public AirportPrestigeAreaPalette Security;
        public AirportPrestigeAreaPalette Shops;
        public AirportPrestigeAreaPalette Boarding;
    }
}