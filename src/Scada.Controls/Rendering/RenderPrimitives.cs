namespace Scada.Controls.Rendering;

public readonly record struct RenderPoint(double X, double Y);

public readonly record struct RenderSize
{
    public RenderSize(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Render dimensions must be finite and positive.");
        }

        Width = width;
        Height = height;
    }

    public double Width { get; }

    public double Height { get; }
}

public readonly record struct RenderRect(double X, double Y, double Width, double Height);

public readonly record struct RenderTransform(double Rotation, double ScaleX = 1, double ScaleY = 1);

public abstract record RenderPrimitive
{
    protected RenderPrimitive(string partId, string token, string? animationName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partId);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        PartId = partId;
        Token = token;
        AnimationName = animationName;
    }

    public string PartId { get; }

    public string Token { get; }

    public string? AnimationName { get; }
}

public sealed record RenderLine : RenderPrimitive
{
    public RenderLine(string partId, RenderPoint Start, RenderPoint End, string token, string? animationName = null)
        : base(partId, token, animationName)
    {
        this.Start = Start;
        this.End = End;
    }

    public RenderPoint Start { get; }

    public RenderPoint End { get; }
}

public sealed record RenderRectangle : RenderPrimitive
{
    public RenderRectangle(string partId, RenderRect Bounds, string token, string? animationName = null)
        : base(partId, token, animationName)
    {
        this.Bounds = Bounds;
    }

    public RenderRect Bounds { get; }
}

public sealed record RenderEllipse : RenderPrimitive
{
    public RenderEllipse(string partId, RenderRect Bounds, string token, string? animationName = null)
        : base(partId, token, animationName)
    {
        this.Bounds = Bounds;
    }

    public RenderRect Bounds { get; }
}

public abstract record PathCommand;

public sealed record MoveTo(RenderPoint Point) : PathCommand;

public sealed record LineTo(RenderPoint Point) : PathCommand;

public sealed record CubicTo(RenderPoint Control1, RenderPoint Control2, RenderPoint Point) : PathCommand;

public sealed record ClosePath : PathCommand;

public sealed record RenderPath : RenderPrimitive
{
    public RenderPath(string partId, IReadOnlyList<PathCommand> Commands, string token, string? animationName = null)
        : base(partId, token, animationName)
    {
        ArgumentNullException.ThrowIfNull(Commands);
        if (Commands.Count == 0)
        {
            throw new ArgumentException("A path must contain at least one command.", nameof(Commands));
        }

        this.Commands = Commands;
    }

    public IReadOnlyList<PathCommand> Commands { get; }
}

public sealed record RenderGroup : RenderPrimitive
{
    public RenderGroup(string partId, IReadOnlyList<RenderPrimitive> Children, string token = "transparent", string? animationName = null)
        : base(partId, token, animationName)
    {
        ArgumentNullException.ThrowIfNull(Children);
        this.Children = Children;
    }

    public IReadOnlyList<RenderPrimitive> Children { get; }
}
