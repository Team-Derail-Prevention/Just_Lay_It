public static class SfxAddress
{
    public static class Ui
    {
        public const string Click = "Sfx/Ui/Click";
        public const string GachaSpin = "Sfx/Ui/GachaSpin";
        public const string WeaponPick = "Sfx/Ui/WeaponPick";
        public const string WeaponEquip = "Sfx/Ui/WeaponEquip";
    }

    public static class Train
    {
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
        public const string Mining = "Sfx/Drone/Mining";
        public const string RailDrop = "Sfx/Drone/RailDrop";
    }

    public static class Game
    {
        public const string Clear = "Sfx/Game/Clear";
    }

    public static readonly string[] All =
    {
        Drone.Propeller,
        Drone.RailDrop,
    };
}

public static class BgmAddress
{
    public const string Lobby = "Bgm/Lobby";
    public const string InGame = "Bgm/InGame";
}
