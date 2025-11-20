namespace ReactWithDotNet.VisualDesigner;

public enum JsType
{
    String,
    Number,
    Date,
    Boolean,
    Array,
    Function
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsTypeInfoAttribute : Attribute
{
    public JsTypeInfoAttribute(JsType jsType)
    {
        JsType = jsType;
    }

    public JsType JsType { get; init; }
}