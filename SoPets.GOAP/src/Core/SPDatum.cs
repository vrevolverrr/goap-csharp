namespace SoPets.GOAP;

public abstract record Datum
{
    public static BoolDatum TRUE => new(true);
    public static BoolDatum FALSE => new(false);
    public static FloatDatum FZERO => new(0.0f);
    public static IntDatum IZERO => new(0);
    public static Vector2Datum CreateVector2(float x, float y) => new(x, y);

    // Distance implementation for Datum types
    public abstract float DistanceTo(Datum other);

    public static Datum Midpoint(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => new IntDatum((a.Value + b.Value) / 2),
        (FloatDatum a, FloatDatum b) => new FloatDatum((a.Value + b.Value) / 2),
        (Vector2Datum a, Vector2Datum b) => new Vector2Datum((a.X + b.X) / 2, (a.Y + b.Y) / 2),
        _ => throw new InvalidOperationException("Can only calculate midpoint between same numeric Datum types"),
    };

    public bool IsNumeric => this is IntDatum or FloatDatum or Vector2Datum;

    public static Datum operator +(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => a + b,
        (FloatDatum a, FloatDatum b) => a + b,
        (Vector2Datum a, Vector2Datum b) => a + b,
        // Mixed numeric operations
        (IntDatum a, FloatDatum b) => new FloatDatum(a.Value + b.Value),
        (FloatDatum a, IntDatum b) => new FloatDatum(a.Value + b.Value),
        // String concatenation
        (StringDatum a, StringDatum b) => new StringDatum(a.Value + b.Value),
        _ => throw new InvalidOperationException($"Cannot add {left.GetType().Name} and {right.GetType().Name}")
    };

    public static Datum operator -(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => a - b,
        (FloatDatum a, FloatDatum b) => a - b,
        (Vector2Datum a, Vector2Datum b) => a - b,
        // Mixed numeric operations
        (IntDatum a, FloatDatum b) => new FloatDatum(a.Value - b.Value),
        (FloatDatum a, IntDatum b) => new FloatDatum(a.Value - b.Value),
        _ => throw new InvalidOperationException($"Cannot subtract {right.GetType().Name} from {left.GetType().Name}")
    };

    public static Datum operator -(Datum value) => value switch
    {
        IntDatum a => -a,
        FloatDatum a => -a,
        Vector2Datum a => -a,
        _ => throw new InvalidOperationException($"Cannot negate {value.GetType().Name}")
    };

    public static bool operator >(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => a > b,
        (FloatDatum a, FloatDatum b) => a > b,
        (Vector2Datum a, Vector2Datum b) => a > b,
        // Mixed numeric comparisons
        (IntDatum a, FloatDatum b) => a.Value > b.Value,
        (FloatDatum a, IntDatum b) => a.Value > b.Value,
        _ => throw new InvalidOperationException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
    };

    public static bool operator <(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => a < b,
        (FloatDatum a, FloatDatum b) => a < b,
        (Vector2Datum a, Vector2Datum b) => a < b,
        // Mixed numeric comparisons
        (IntDatum a, FloatDatum b) => a.Value < b.Value,
        (FloatDatum a, IntDatum b) => a.Value < b.Value,
        _ => throw new InvalidOperationException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
    };

    public static bool operator >=(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => a >= b,
        (FloatDatum a, FloatDatum b) => a >= b,
        (Vector2Datum a, Vector2Datum b) => a >= b,
        // Mixed numeric comparisons
        (IntDatum a, FloatDatum b) => a.Value >= b.Value,
        (FloatDatum a, IntDatum b) => a.Value >= b.Value,
        _ => throw new InvalidOperationException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
    };

    public static bool operator <=(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => a <= b,
        (FloatDatum a, FloatDatum b) => a <= b,
        (Vector2Datum a, Vector2Datum b) => a <= b,
        // Mixed numeric comparisons
        (IntDatum a, FloatDatum b) => a.Value <= b.Value,
        (FloatDatum a, IntDatum b) => a.Value <= b.Value,
        _ => throw new InvalidOperationException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
    };

    // Min/Max operations
    public static Datum Min(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => IntDatum.Min(a, b),
        (FloatDatum a, FloatDatum b) => FloatDatum.Min(a, b),
        (Vector2Datum a, Vector2Datum b) => Vector2Datum.Min(a, b),
        _ => throw new InvalidOperationException($"Cannot find min between {left.GetType().Name} and {right.GetType().Name}")
    };

    public static Datum Max(Datum left, Datum right) => (left, right) switch
    {
        (IntDatum a, IntDatum b) => IntDatum.Max(a, b),
        (FloatDatum a, FloatDatum b) => FloatDatum.Max(a, b),
        (Vector2Datum a, Vector2Datum b) => Vector2Datum.Max(a, b),
        _ => throw new InvalidOperationException($"Cannot find max between {left.GetType().Name} and {right.GetType().Name}")
    };
}

public record IntDatum(int Value) : Datum
{
    public static IntDatum operator +(IntDatum left, IntDatum right) => new(left.Value + right.Value);
    public static IntDatum operator -(IntDatum left, IntDatum right) => new(left.Value - right.Value);
    public static IntDatum operator -(IntDatum value) => new(-value.Value);
    public static bool operator >(IntDatum left, IntDatum right) => left.Value > right.Value;
    public static bool operator <(IntDatum left, IntDatum right) => left.Value < right.Value;
    public static bool operator >=(IntDatum left, IntDatum right) => left.Value >= right.Value;
    public static bool operator <=(IntDatum left, IntDatum right) => left.Value <= right.Value;

    public static IntDatum Min(IntDatum left, IntDatum right) => new(Math.Min(left.Value, right.Value));
    public static IntDatum Max(IntDatum left, IntDatum right) => new(Math.Max(left.Value, right.Value));

    public override float DistanceTo(Datum other) => other switch
    {
        IntDatum otherInt => Math.Abs(Value - otherInt.Value),
        _ => throw new InvalidOperationException("Cannot calculate distance between different Datum types")
    };

    public override string ToString() => Value.ToString();
}

public record FloatDatum(float Value) : Datum
{
    public static FloatDatum operator +(FloatDatum left, FloatDatum right) => new(left.Value + right.Value);
    public static FloatDatum operator -(FloatDatum left, FloatDatum right) => new(left.Value - right.Value);
    public static FloatDatum operator -(FloatDatum value) => new(-value.Value);
    public static bool operator >(FloatDatum left, FloatDatum right) => left.Value > right.Value;
    public static bool operator <(FloatDatum left, FloatDatum right) => left.Value < right.Value;
    public static bool operator >=(FloatDatum left, FloatDatum right) => left.Value >= right.Value;
    public static bool operator <=(FloatDatum left, FloatDatum right) => left.Value <= right.Value;

    public static FloatDatum Min(FloatDatum left, FloatDatum right) => new(Math.Min(left.Value, right.Value));
    public static FloatDatum Max(FloatDatum left, FloatDatum right) => new(Math.Max(left.Value, right.Value));

    public override float DistanceTo(Datum other) => other switch
    {
        FloatDatum otherFloat => Math.Abs(Value - otherFloat.Value),
        _ => throw new InvalidOperationException("Cannot calculate distance between different Datum types")
    };

    public override string ToString() => Value.ToString("F2");
}

public record Vector2Datum(float X, float Y) : Datum
{
    public static Vector2Datum operator +(Vector2Datum left, Vector2Datum right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static Vector2Datum operator -(Vector2Datum left, Vector2Datum right) =>
        new(left.X - right.X, left.Y - right.Y);

    public static Vector2Datum operator -(Vector2Datum value) =>
        new(-value.X, -value.Y);

    public static Vector2Datum operator *(Vector2Datum vector, float scalar) =>
        new(vector.X * scalar, vector.Y * scalar);

    public static Vector2Datum operator /(Vector2Datum vector, float scalar) =>
        new(vector.X / scalar, vector.Y / scalar);

    public static bool operator >(Vector2Datum left, Vector2Datum right) =>
        left.Magnitude() > right.Magnitude();

    public static bool operator <(Vector2Datum left, Vector2Datum right) =>
        left.Magnitude() < right.Magnitude();

    public static bool operator >=(Vector2Datum left, Vector2Datum right) =>
        left.Magnitude() >= right.Magnitude();

    public static bool operator <=(Vector2Datum left, Vector2Datum right) =>
        left.Magnitude() <= right.Magnitude();

    public static Vector2Datum Min(Vector2Datum left, Vector2Datum right) =>
        left.Magnitude() < right.Magnitude() ? left : right;

    public static Vector2Datum Max(Vector2Datum left, Vector2Datum right) =>
        left.Magnitude() > right.Magnitude() ? left : right;

    // Vector-specific methods
    public float Magnitude() => MathF.Sqrt(X * X + Y * Y);
    public float MagnitudeSquared() => X * X + Y * Y;
    public Vector2Datum Normalized() => this / Magnitude();
    public float Dot(Vector2Datum other) => X * other.X + Y * other.Y;

    public override float DistanceTo(Datum other) => other switch
    {
        Vector2Datum otherVector => MathF.Sqrt(
            (X - otherVector.X) * (X - otherVector.X) +
            (Y - otherVector.Y) * (Y - otherVector.Y)
        ),
        _ => throw new InvalidOperationException("Cannot calculate distance between different Datum types")
    };

    public override string ToString() => $"({X:F2}, {Y:F2})";
}

public record BoolDatum(bool Value) : Datum
{
    public override float DistanceTo(Datum other) => other switch
    {
        BoolDatum otherBool => Value == otherBool.Value ? 0 : 1,
        _ => throw new InvalidOperationException("Cannot calculate distance between different Datum types")
    };

    public override string ToString() => Value.ToString();
}

public record StringDatum(string Value) : Datum
{
    public override float DistanceTo(Datum other) => other switch
    {
        StringDatum otherString => Value == otherString.Value ? 0 : 1,
        _ => throw new InvalidOperationException("Cannot calculate distance between different Datum types")
    };

    public override string ToString() => Value;
}