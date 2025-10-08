using System.Text;

namespace BDigitalFrameworkApiToTsExporter;

static class TsOutput
{
    public static string GetTsCode(TsFieldDefinition field)
    {
        return $"{field.Name}{(field.IsNullable ? '?' : string.Empty)} : {field.Type.Name};";
    }

    public static IReadOnlyList<string> GetTsCode(TsTypeDefinition tsTypeDefinition)
    {
        List<string> lines = [];

        var allImports = from importInfo in tsTypeDefinition.BaseType.Imports.Concat
                             (
                              from tsFieldDefinition in tsTypeDefinition.Fields
                              from importForField in tsFieldDefinition.Type.Imports
                              select importForField
                             )
                         select importInfo;

        foreach (var importInfo in allImports.DistinctBy(x => x.LocalName))
        {
            lines.Add($"import {{{{ {importInfo.LocalName} }} from \"{importInfo.Source}\";");
        }

        if (tsTypeDefinition.IsEnum)
        {
            lines.Add($"export enum {tsTypeDefinition.Name}");
        }
        else
        {
            var extends = " extends ";
            if (string.IsNullOrWhiteSpace(tsTypeDefinition.BaseType.Name))
            {
                extends = "";
            }
            else
            {
                extends += tsTypeDefinition.BaseType.Name;
            }

            lines.Add($"export interface {tsTypeDefinition.Name}" + extends);
        }

        lines.Add("{");

        if (tsTypeDefinition.IsEnum)
        {
            var definitionLines =
                from field in tsTypeDefinition.Fields
                let line = $"{field.Name} = {field.ConstantValue}"
                let isLast = field == tsTypeDefinition.Fields[^1]
                select isLast switch
                {
                    true  => line,
                    false => line + ","
                };

            lines.AddRange(definitionLines);
        }
        else
        {
            foreach (var field in tsTypeDefinition.Fields)
            {
                lines.Add(GetTsCode(field));
            }
        }

        lines.Add("}");

        return lines;
    }

    public static string LinesToString(IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();

        var indentCount = 0;

        foreach (var line in lines)
        {
            var padding = string.Empty.PadRight(indentCount * 4, ' ');

            if (line == "{")
            {
                sb.AppendLine(padding + line);
                indentCount++;
                continue;
            }

            if (line == "}")
            {
                indentCount--;

                padding = string.Empty.PadRight(indentCount * 4, ' ');
            }

            sb.AppendLine(padding + line);
        }

        return sb.ToString();
    }
}