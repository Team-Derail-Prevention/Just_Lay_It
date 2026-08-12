using System;

public struct CellPos : IEquatable<CellPos>
{
    public int X;
    public int Y;

    public CellPos(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(CellPos other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        if (obj is CellPos other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (X * 397) ^ Y;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public static bool operator ==(CellPos left, CellPos right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CellPos left, CellPos right)
    {
        return left.Equals(right) == false;
    }
}
