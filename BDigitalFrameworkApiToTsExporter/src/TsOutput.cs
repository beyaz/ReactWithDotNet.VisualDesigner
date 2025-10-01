using System.Text;

namespace BDigitalFrameworkApiToTsExporter;

static class TsOutput
{
    public static  string GetTsCode(TsFieldDefinition field)
    {
        return $"{field.Name}{(field.IsNullable ? '?' : string.Empty)} : {field.TypeName};";
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
            if (string.IsNullOrWhiteSpace(type.BaseTypeName))
            {
                extends = "";
            }
            else
            {
                extends += type.BaseTypeName;
            }

            lines.Add($"export interface {type.Name}" + extends);
        }

        lines.Add("{");
        
        if (type.IsEnum)
        {
            var fieldDeclarations = new List<string>();
            foreach (var field in type.Fields.Where(f => f.Name != "value__"))
            {
                fieldDeclarations.Add($"{field.Name} = {field.ConstantValue}");
            }

            for (var i = 0; i < fieldDeclarations.Count; i++)
            {
                var declaration = fieldDeclarations[i];

                if (i < fieldDeclarations.Count - 1)
                {
                    lines.Add(declaration + ",");
                }
                else
                {
                    lines.Add(declaration);
                }
            }
        }
        else
        {
            foreach (var field in type.Fields)
            {
                lines.Add(field.ToString());
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