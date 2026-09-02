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
        public const string Drive = "Sfx/Train/Drive";
        public const string Repair = "Sfx/Train/Repair";
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
    };
}

public static class BgmAddress
{
    public const string Lobby = "Bgm/OutGame";
    public const string InGame = "Bgm/InGame";
}
