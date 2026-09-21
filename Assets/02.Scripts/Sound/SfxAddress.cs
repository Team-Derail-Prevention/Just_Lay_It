using System.Collections.Generic;

public static class SfxAddress
{
    public static class Ui
    {
        public const string Hover = "Sfx/Ui/Hover";
        public const string Click = "Sfx/Ui/Click";
        public const string GachaSpin = "Sfx/Ui/GachaSpin";
        public const string WeaponPickCommon = "Sfx/Ui/WeaponPickCommon";
        public const string WeaponPickRare = "Sfx/Ui/WeaponPickRare";
        public const string WeaponPickEpic = "Sfx/Ui/WeaponPickEpic";
        public const string WeaponPickLegendary = "Sfx/Ui/WeaponPickLegendary";
        public const string WeaponEquip = "Sfx/Ui/WeaponEquip";
        public const string WeaponUnEquip = "Sfx/Ui/WeaponUnEquip";
        public const string Denied = "Sfx/Ui/Denied";
        public const string Purchase = "Sfx/Ui/Purchase";
        public const string Sell = "Sfx/Ui/Sell";
    }

    public static class Train
    {
        public const string Depart = "Sfx/Train/Depart";
        public const string Stop = "Sfx/Train/Stop";
        public const string Arrive = "Sfx/Train/Arrive";
        public const string Hit = "Sfx/Train/Hit";
        public const string Drive = "Sfx/Train/Drive";
        public const string Repair = "Sfx/Train/Repair";
        public const string HpWarning50 = "Sfx/Train/HpWarning50";
        public const string HpWarning25 = "Sfx/Train/HpWarning25";
        public const string StopWarning = "Sfx/Train/StopWarning";
    }

    public static class Sandstorm
    {
        public const string Ambience = "Sfx/Sandstorm/Ambience";
        public const string Countdown = "Sfx/Sandstorm/Countdown";
    }

    public static class Weapon
    {
        public const string Prefix = "Sfx/Weapon/";
        public const string Fire = "Sfx/Weapon/Fire";
        public const string Reload = "Sfx/Weapon/Reload";
    }

    public static class Monster
    {
        public const string Prefix = "Sfx/Monster/";
        public const string Attack = "Sfx/Monster/Attack";
        public const string Hit = "Sfx/Monster/Hit";
        public const string Die = "Sfx/Monster/Die";
    }

    public static class Drone
    {
        public const string Propeller = "Sfx/Drone/Propeller";
        public const string MineRock = "Sfx/Drone/MineRock";
        public const string ChopTree = "Sfx/Drone/ChopTree";
        public const string RailDrop = "Sfx/Drone/RailDrop";
    }

    public static class Resource
    {
        public const string Collected = "Sfx/Resource/Collected";
    }

    public static class Game
    {
        public const string Clear = "Sfx/Game/Clear";
        public const string Over = "Sfx/Game/Over";
    }

    private static readonly Dictionary<string, int> VariantCounts = new Dictionary<string, int>
    {
        { Ui.Click, 2 },
        { Ui.GachaSpin, 2 },
        { Ui.WeaponPickLegendary, 5 },
        { Train.Hit, 8 },
        { Drone.MineRock, 8 },
        { Drone.ChopTree, 8 },
        { Resource.Collected, 2 },
        { Game.Over, 5 },
    };

    public static int GetVariantCount(string address)
    {
        if (address == null || VariantCounts.TryGetValue(address, out int count) == false)
        {
            return 1;
        }

        return count;
    }

    public static string GetVariant(string address, int number)
    {
        if (GetVariantCount(address) <= 1)
        {
            return address;
        }

        return $"{address}_{number}";
    }

    public static string PickRandom(string address)
    {
        int number = UnityEngine.Random.Range(1, GetVariantCount(address) + 1);

        return GetVariant(address, number);
    }

    public static string Resolve(string prefix, string dataSoundName, string fallbackAddress)
    {
        if (string.IsNullOrEmpty(dataSoundName))
        {
            return fallbackAddress;
        }

        return prefix + dataSoundName;
    }

    public static readonly string[] All =
    {
        Drone.Propeller,
        Drone.RailDrop,
        Ui.Hover,
        Ui.Click,
        Train.Depart,
        Train.Arrive,
        Train.HpWarning50,
        Train.HpWarning25,
        Train.StopWarning,
        Sandstorm.Countdown,
    };
}

public static class BgmAddress
{
    public const string Lobby = "Bgm/OutGame";
    public const string InGame = "Bgm/InGame";
}
