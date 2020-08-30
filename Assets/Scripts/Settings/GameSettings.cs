﻿using Assets.Scripts.Settings.Gamemodes;
using Assets.Scripts.Settings.Titans;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.Settings
{
    public class GameSettings
    {
        public static PvPSettings PvP { get; private set; }
        public static GamemodeSettings Gamemode { get; private set; }

        public static T DerivedGamemode<T>() where T : GamemodeSettings
        {
            return Gamemode as T;
        }
        public static SettingsTitan Titan { get; private set; }
        public static HorseSettings Horse { get; private set; }
        public static RespawnSettings Respawn { get; private set; }

        [JsonProperty("Gamemodes")]
        private List<GamemodeSettings> ConfigGamemodes { get; set; }

        [JsonProperty("PvP")]
        private PvPSettings ConfigPvP { get; set; }

        [JsonProperty("Titan")]
        private SettingsTitan ConfigTitan { get; set; }

        [JsonProperty("Horse")]
        private HorseSettings ConfigHorse { get; set; }

        [JsonProperty("Respawn")]
        private RespawnSettings ConfigRespawn { get; set; }

        public void Initialize(GamemodeType type)
        {
            PvP = ConfigPvP;
            Titan = ConfigTitan;
            Gamemode = ConfigGamemodes.Single(x => x.GamemodeType == type);
            Horse = ConfigHorse;
            Respawn = ConfigRespawn;
        }

        public void Initialize(List<GamemodeSettings> gamemodes, PvPSettings pvp, SettingsTitan titan, HorseSettings horse, RespawnSettings respawn)
        {
            PvP = ConfigPvP = pvp;
            Titan = ConfigTitan = titan;
            ConfigGamemodes = gamemodes;
            Horse = ConfigHorse = horse;
            Respawn = ConfigRespawn = respawn;
        }

        public void ChangeSettings(GamemodeSettings levelGamemode)
        {
            var playerGamemodeSettings = ConfigGamemodes.Single(x => x.GamemodeType == levelGamemode.GamemodeType);
            switch (levelGamemode.GamemodeType)
            {
                case GamemodeType.Titans:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as KillTitansSettings, levelGamemode as KillTitansSettings);
                    break;
                case GamemodeType.Endless:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as EndlessSettings, levelGamemode as EndlessSettings);
                    break;
                case GamemodeType.Capture:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as CaptureGamemodeSettings, levelGamemode as CaptureGamemodeSettings);
                    break;
                case GamemodeType.Wave:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as WaveGamemodeSettings, levelGamemode as WaveGamemodeSettings);
                    break;
                case GamemodeType.Racing:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as RacingSettings, levelGamemode as RacingSettings);
                    break;
                case GamemodeType.Trost:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as TrostSettings, levelGamemode as TrostSettings);
                    break;
                case GamemodeType.TitanRush:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as RushSettings, levelGamemode as RushSettings);
                    break;
                case GamemodeType.PvpAhss:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as PvPAhssSettings, levelGamemode as PvPAhssSettings);
                    break;
                case GamemodeType.Infection:
                    Gamemode = CreateFromObjects(playerGamemodeSettings as InfectionGamemodeSettings, levelGamemode as InfectionGamemodeSettings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            PvP = CreateFromObjects(ConfigPvP, playerGamemodeSettings.Pvp, levelGamemode.Pvp);
            Titan = CreateFromObjects(ConfigTitan, playerGamemodeSettings.Titan, levelGamemode.Titan);
            Horse = CreateFromObjects(ConfigHorse, playerGamemodeSettings.Horse, levelGamemode.Horse);
            Respawn = CreateFromObjects(ConfigRespawn, playerGamemodeSettings.Respawn, levelGamemode.Respawn);
        }

        public T CreateFromObjects<T>(params T[] sources)
            where T : new()
        {
            var ret = new T();
            MergeObjects(ret, sources);

            return ret;
        }

        public void MergeObjects<T>(T target, params T[] sources)
        {
            Func<PropertyInfo, T, bool> predicate = (p, s) =>
            {
                try
                {
                    if (p.GetValue(s).Equals(GetDefault(p.PropertyType)))
                    {
                        return false;
                    }

                    return true;
                }
                catch (NullReferenceException e)
                {
                    Debug.LogWarning($"{p.Name} of {p.PropertyType} is unassigned in {s.ToString()}");
                    return false;
                }
            };
            MergeObjects(target, predicate, sources);
        }

        public void MergeObjects<T>(T target, Func<PropertyInfo, T, bool> predicate, params T[] sources)
        {
            foreach (var propertyInfo in typeof(T).GetProperties().Where(prop => prop.CanRead && prop.CanWrite))
            {
                foreach (var source in sources)
                {
                    if (source == null)
                        continue;
                    if (predicate(propertyInfo, source))
                    {
                        propertyInfo.SetValue(target, propertyInfo.GetValue(source));
                    }
                }
            }
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}
