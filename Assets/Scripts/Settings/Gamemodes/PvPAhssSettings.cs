﻿using Assets.Scripts.Gamemode.Options;
using Assets.Scripts.Settings.Titans;

namespace Assets.Scripts.Settings.Gamemodes
{
    public class PvPAhssSettings : GamemodeSettings
    {
        public PvPAhssSettings()
        {
            GamemodeType = GamemodeType.PvpAhss;
            Pvp = new PvPSettings
            {
                Cannons = false,
                Mode = PvpMode.AhssVsBlades
            };
            Titan = new SettingsTitan
            {
                Start = -1
            };
            AhssAirReload = false;
            PlayerShifters = false;
            Horse = false;
            TitansEnabled = false;
        }
    }
}