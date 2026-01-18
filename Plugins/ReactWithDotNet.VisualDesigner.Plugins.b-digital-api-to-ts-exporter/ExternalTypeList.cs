namespace BDigitalFrameworkApiToTsExporter;

public sealed record ExternalTypeInfo
{
    public required string DotNetFullTypeName { get; init; }

    public required string LocalName { get; init; }

    public required string Source { get; init; }
}

public class ExternalTypeList
{
    public static readonly IReadOnlyList<ExternalTypeInfo> Value =
    [
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.Account",
            LocalName          = "Account",
            Source             = "b-digital-internet-banking"
        },
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.BaseModel",
            LocalName          = "BaseModel",
            Source             = "b-digital-internet-banking"
        },
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.Models.BaseFinancialModel",
            LocalName          = "BaseFinancialModel",
            Source             = "b-digital-internet-banking"
        },
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.Card",
            LocalName          = "Card",
            Source             = "b-digital-internet-banking"
        },
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.BaseClientRequest",
            LocalName          = "BaseClientRequest",
            Source             = "b-digital-framework"
        },
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.BaseClientResponse",
            LocalName          = "BaseClientResponse",
            Source             = "b-digital-framework"
        },
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.TextValue",
            LocalName          = "TextValuePair as TextValue",
            Source             = "b-digital-internet-banking"
        },
        new()
        {
            DotNetFullTypeName = "BOA.InternetBanking.Common.SecureConfirmAgreementData",
            LocalName          = "SecureConfirmAgreementData",
            Source             = "b-digital-internet-banking"
        },
    ];
}