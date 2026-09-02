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
    }

    public static class Train
    {
        public const string Depart = "Sfx/Train/Depart";
        public const string Arrive = "Sfx/Train/Arrive";
        public const string Drive = "Sfx/Train/Drive";
        public const string Repair = "Sfx/Train/Repair";
    }

    public static class Monster
    {
        public const string LaserFire = "Sfx/Monster/LaserFire";
        public const string MetalHit = "Sfx/Monster/MetalHit";
        public const string MachineBreak = "Sfx/Monster/MachineBreak";
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
    public const string Lobby = "Bgm/Lobby";
    public const string InGame = "Bgm/InGame";
}
