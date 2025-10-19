using System.Text;

namespace BDigitalFrameworkApiToTsExporter;

static class TsOutput
{
    public static string GetTsCode(TsFieldDefinition field)
    {
        return $"{field.Name}{(field.IsNullable ? '?' : string.Empty)} : {field.Type.Name};";
    }

    public static IReadOnlyList<string> GetTsCode(params TsTypeDefinition[] tsTypeDefinitions)
    {
        List<string> lines = [];

        List<TsImportInfo> imports = [];

        foreach (var tsTypeDefinition in tsTypeDefinitions)
        {
            imports.AddRange(tsTypeDefinition.BaseType.Imports);

            imports.AddRange(from tsFieldDefinition in tsTypeDefinition.Fields
                             from importForField in tsFieldDefinition.Type.Imports
                             select importForField);
        }

        foreach (var group in imports.DistinctBy(x => x.LocalName).GroupBy(x => x.Source))
        {
            IEnumerable<TsImportInfo> groupedImports = group;

            lines.Add($"import {{ {string.Join(", ", from x in groupedImports select x.LocalName)} }} from \"{group.Key}\";");
        }

        foreach (var tsTypeDefinition in tsTypeDefinitions)
        {
            lines.Add(string.Empty);

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
        }

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