namespace PeekPin;

public readonly record struct PixelPoint(int X, int Y);

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool Contains(PixelPoint point)
    {
        return point.X >= X && point.Y >= Y && point.X < Right && point.Y < Bottom;
    }

    public PixelRect Inflate(int pixels)
    {
        return new PixelRect(X - pixels, Y - pixels, Width + pixels * 2, Height + pixels * 2);
    }
}
