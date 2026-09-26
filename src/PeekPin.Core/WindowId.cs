namespace PeekPin;

public readonly record struct WindowId(nint Value)
{
    public bool IsEmpty => Value == 0;

    public override string ToString() => Value.ToString("X");
}
