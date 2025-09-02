namespace ReactWithDotNet.VisualDesigner;

public sealed record StyleAttribute
{
    public string Name { get; init; }
    public string Pseudo { get; init; }
    public string Value { get; init; }

    public static implicit operator (string name, string value, string pseudo)(StyleAttribute item)
    {
        return (item.Name, item.Value, item.Pseudo);
    }

    public void Deconstruct(out string name, out string value, out string pseudo)
    {
        name   = Name;
        value  = Value;
        pseudo = Pseudo;
    }
}