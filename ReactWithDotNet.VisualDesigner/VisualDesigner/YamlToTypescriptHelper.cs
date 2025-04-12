using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace ReactWithDotNet.VisualDesigner.Models;

static class YamlToTypescriptHelper
{
    public static (List<string> lines, List<ImportInfo> imports) TryWriteInterfaceBody(string componentName, int indent, string yaml)
    {
        var lines = new List<string>();
        var imports = new List<ImportInfo>();

        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(yaml));

        // Process the YAML document
        foreach (var document in yamlStream.Documents)
        {
            processNode(document.RootNode);
        }

        return (lines, imports);

        void processNode(YamlNode node)
        {
            if (node is YamlMappingNode mappingNode)
            {
                foreach (var entry in mappingNode.Children)
                {
                    var keyNode = entry.Key;
                    var valueNode = entry.Value;

                    var propertyName = keyNode.ToString();
                    var tsTypeName = InferTsTypeName(valueNode.ToString());

                    // Extract comment associated with the property
                    var comment = ExtractComment(propertyName, yaml);

                    var isNullable = propertyName.TrimEnd().EndsWith("?");
                    if (isNullable)
                    {
                        propertyName = propertyName.RemoveFromEnd("?");
                    }

                    if (comment?.EndsWith("Model") is true)
                    {
                        string package;
                        {
                            var modelPath = componentName.Split('/').ToList();
                            if (modelPath.Count <= 1)
                            {
                                throw new NotImplementedException(comment);
                            }

                            modelPath[^1] = "Models";
                            package       = "@/components/" + string.Join("/", modelPath);
                        }

                        imports.Add(new() { ClassName = comment, Package = package, IsNamed = true });
                        tsTypeName = comment;

                        comment = null;
                    }

                    // Adding the property line and comment (if any)
                    lines.Add($"{Indent(indent)}{propertyName}{(isNullable ? "?" : string.Empty)}: {tsTypeName};{(string.IsNullOrEmpty(comment) ? string.Empty : $" // {comment}")}");

                    // If the value is a nested object, process it as well
                    if (valueNode is YamlMappingNode || valueNode is YamlSequenceNode)
                    {
                        //processNode(valueNode);
                    }
                }
            }
        }

        // Extract the comment for the property from the YAML text
        string ExtractComment(string propertyName, string yamlText)
        {
            // Match the property and the comment on the same line
            var regex = new Regex($@"^\s*{Regex.Escape(propertyName)}\s*:\s*(.*)\s*#\s*(.*)$", RegexOptions.Multiline);
            var match = regex.Match(yamlText);
            return match.Success ? match.Groups[2].Value.Trim() : string.Empty;
        }

        // Function to infer the TypeScript type based on the value
        string InferTsTypeName(string value)
        {
            if (value == null)
            {
                return "string";
            }

            value = value.Trim();

            if (bool.TryParse(value, out _))
            {
                return "boolean";
            }

            if (int.TryParse(value, out _))
            {
                return "number";
            }

            if (value.StartsWith("() =>"))
            {
                return value;
            }

            if (value.Equals("ReactNode", StringComparison.OrdinalIgnoreCase))
            {
                imports.Add(new() { ClassName = "React", Package = "react" });
                return "React.ReactNode";
            }

            return "string";
        }
    }

    public static (List<string> lines, List<ImportInfo> imports) TryWriteProps(ComponentEntity cmp, int indent)
    {
        List<string> lines = [];

        List<ImportInfo> imports = [];

        if (cmp.PropsAsYaml.HasNoValue())
        {
            return (lines, imports);
        }

        lines.Add($"{Indent(indent)}export interface Props {{");

        indent++;

        // body
        {
            var result = TryWriteInterfaceBody(cmp.Name, indent, cmp.PropsAsYaml);

            lines.AddRange(result.lines);

            imports.AddRange(result.imports);
        }

        indent--;

        lines.Add($"{Indent(indent)}}}");

        return (lines, imports);
    }

    public static (List<string> lines, List<ImportInfo> imports) TryWriteState(ComponentEntity cmp, int indent)
    {
        List<string> lines = [];

        List<ImportInfo> imports = [];

        if (cmp.StateAsYaml.HasNoValue())
        {
            return (lines, imports);
        }

        lines.Add($"{Indent(indent)}export interface State {{");

        indent++;

        // body
        {
            var result = TryWriteInterfaceBody(cmp.Name, indent, cmp.StateAsYaml);

            lines.AddRange(result.lines);

            imports.AddRange(result.imports);
        }

        indent--;

        lines.Add($"{Indent(indent)}}}");

        return (lines, imports);
    }

    static string Indent(int indentLevel)
    {
        return new(' ', indentLevel * 4);
    }
}