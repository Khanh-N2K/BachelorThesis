using System;

public struct Int2 : IEquatable<Int2>
{
    public int x, y;
    public Int2(int x, int y) { this.x = x; this.y = y; }
    public static Int2 operator +(Int2 a, Int2 b) => new Int2(a.x + b.x, a.y + b.y);
    public static bool operator ==(Int2 a, Int2 b) => a.x == b.x && a.y == b.y;
    public static bool operator !=(Int2 a, Int2 b) => !(a == b);
    public override int GetHashCode() => HashCode.Combine(x, y);
    public override bool Equals(object obj) => obj is Int2 o && Equals(o);
    public bool Equals(Int2 other) => x == other.x && y == other.y;
    public override string ToString() => $"({x},{y})";
}