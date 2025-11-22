namespace Dreambit.UI;

public struct UiLength
{
    public float Value;
    public bool IsPercent;

    public UiLength(float value, bool isPercent)
    {
        Value = value;
        IsPercent = isPercent;
    }

    public static UiLength Pixels(float px) => new UiLength(px, false);
    public static UiLength Percent(float px) => new UiLength(px, true);

    public int Resolve(int parentSize)
    {
        if (IsPercent)
            return (int)(parentSize * Value); // Value as 0.0–1.0
        return (int)Value;
    }
}