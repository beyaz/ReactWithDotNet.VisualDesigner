using System.Text;

namespace BDigitalFrameworkApiToTsExporter;

static class TsOutput
{
    public static string GetTsCode(TsFieldDefinition field)
    {
        return $"{field.Name}{(field.IsNullable ? '?' : string.Empty)} : {field.Type.Name};";
    }

    public static IReadOnlyList<string> GetTsCode(TsTypeDefinition type)
    {
        List<string> lines = [];

        if (type.IsEnum)
        {
            lines.Add($"export enum {type.Name}");
        }
        else
        {
            var extends = " extends ";
            if (string.IsNullOrWhiteSpace(type.BaseType.Name))
            {
                extends = "";
            }
            else
            {
                extends += type.BaseType.Name;
            }

            lines.Add($"export interface {type.Name}" + extends);
        }

        lines.Add("{");

        if (type.IsEnum)
        {
            var definitionLines =
                from field in type.Fields
                let line = $"{field.Name} = {field.ConstantValue}"
                let isLast = field == type.Fields[^1]
                select isLast switch
                {
                    true  => line,
                    false => line + ","
                };
            
            lines.AddRange(definitionLines);
        }
        else
        {
            foreach (var field in type.Fields)
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