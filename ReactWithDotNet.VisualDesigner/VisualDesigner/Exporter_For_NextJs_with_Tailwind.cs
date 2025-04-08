using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Mono.Cecil;
using YamlDotNet.Core.Tokens;
using System.Formats.Tar;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Exporter_For_NextJs_with_Tailwind
{
    class ReactProperty
    {
        public string Name { get; init; }
        public string Value { get; init; }
    }
    
    class ReactNode
    {
        public string Tag { get; init; }
        
        public List<ReactProperty> Properties { get; init; } = [];
        
        public List<ReactNode> Children { get; init; } = [];
        
        public string Text { get; init; }
    }

    class SourceFile
    {
        public Imports Imports { get; } = new();
        
        public ReactNode RootNode { get; init; }

        public List<string> Body { get; init; } = [];

        public List<InterfaceDecleration> InterfaceDeclerations { get; init; } = [];
    }

    class InterfaceDecleration
    {
        public Imports Imports { get; } = new();
        
        public string Identifier { get; init; }
        
        public List<PropertySignature> PropertySignatures { get; init; } = [];
    }
    
    class PropertySignature
    {
        public string Identifier { get; init; }
        public string Type { get; init; }
    }
    
    public static void Export_old(ApplicationState state)
    {
        var context = new Context();

        const int indentLevel = 1;

        var indent = new string(' ', indentLevel * 4);

        var partRender = GenerateHtml(context, state.ComponentRootElement, indentLevel);

        var importLines = new StringBuilder();

        foreach (var import in context.Imports)
        {
            if (import.IsNamed)
            {
                importLines.AppendLine($"import {{ {import.ClassName} }} from \"{import.Package}\";");
            }
            else
            {
                importLines.AppendLine($"import {import.ClassName} from \"{import.Package}\";");
            }
        }

        var file = new StringBuilder();


        string propsParameterTypeName = string.Empty;
        
        var propsTypeDefinition = GetPropsTypeDefinition(state);
        if (propsTypeDefinition is not null)
        {
            file.AppendLine();
            file.AppendLine($"interface {propsTypeDefinition.Name} {{");
            foreach (var fieldDefinition in propsTypeDefinition.Fields)
            {
                var typeName = fieldDefinition.FieldType.Name;
                if (typeName == "Boolean")
                {
                    typeName = "boolean";
                }
                
                if (typeName == "Action")
                {
                    typeName = "() => void";
                }
                
                if (typeName == "Int32")
                {
                    typeName = "number";
                }

                if (typeName == "ReactNode")
                {
                    context.Imports.Add(new (){ ClassName = "React", Package = "react"});
                }
                
                file.AppendLine($"{fieldDefinition.Name} : {typeName}");
            }
            file.AppendLine("}");

            propsParameterTypeName = propsTypeDefinition.Name;
        }

        file.AppendLine();
        file.AppendLine($"export default function {state.ComponentName}({(propsParameterTypeName.HasNoValue() ? string.Empty  : "props: " +propsParameterTypeName )}) {{");

        foreach (var line in context.Body)
        {
            file.AppendLine(indent + line);
        }

        file.AppendLine($"{indent}return (");
        file.AppendLine(partRender);
        file.AppendLine($"{indent});");

        file.AppendLine("}");

        File.WriteAllText($"C:\\github\\hopgogo\\web\\enduser-ui\\src\\components\\{state.ComponentName}.tsx", importLines.ToString() + file.ToString());
    }

    static InterfaceDecleration ConvertToInterfaceDecleration(TypeDefinition typeDefinition)
    {
        var decleration = new InterfaceDecleration
        {
            Identifier = typeDefinition.Name
        };

        foreach (var fieldDefinition in typeDefinition.Fields)
        {
            var typeName = fieldDefinition.FieldType.Name;
            if (typeName == "Boolean")
            {
                typeName = "boolean";
            }
                
            if (typeName == "Action")
            {
                typeName = "() => void";
            }
                
            if (typeName == "Int32")
            {
                typeName = "number";
            }

            if (typeName == "ReactNode")
            {
                decleration.Imports.Add(new (){ ClassName = "React", Package = "react"});
            }
                
            decleration.PropertySignatures.Add( new() { Identifier = fieldDefinition.Name, Type = typeName});
        }

        return decleration;
    }

    static string Indent(int indentLevel)
    {
        return new(' ', indentLevel * 4);
    }
    static void WriteTo(this InterfaceDecleration decleration, StringBuilder sb, int indentLevel)
    {
        sb.AppendLine($"{Indent(indentLevel)}interface {decleration.Identifier} {{");
        
        indentLevel++;
        
        foreach (var propertySignature in decleration.PropertySignatures)
        {
            sb.AppendLine($"{Indent(indentLevel)}{propertySignature.Identifier} : {propertySignature.Type}");
        }

        indentLevel--;
        
        sb.AppendLine(Indent(indentLevel)+"}");
    }
    
    public static void Export(ApplicationState state)
    {
        var tsxFile = new SourceFile();
        
        const int indentLevel = 1;

        var indent = new string(' ', indentLevel * 4);

        var rootNode = ConvertToReactNode(tsxFile, state.ComponentRootElement);

        var importLines = new StringBuilder();

        foreach (var import in tsxFile.Imports)
        {
            if (import.IsNamed)
            {
                importLines.AppendLine($"import {{ {import.ClassName} }} from \"{import.Package}\";");
            }
            else
            {
                importLines.AppendLine($"import {import.ClassName} from \"{import.Package}\";");
            }
        }

        var file = new StringBuilder();


        string propsParameterTypeName = string.Empty;
        
        var propsTypeDefinition = GetPropsTypeDefinition(state);
        if (propsTypeDefinition is not null)
        {
            tsxFile.InterfaceDeclerations.Add(ConvertToInterfaceDecleration(propsTypeDefinition));
            
            file.AppendLine();
            file.AppendLine($"interface {propsTypeDefinition.Name} {{");
            foreach (var fieldDefinition in propsTypeDefinition.Fields)
            {
                var typeName = fieldDefinition.FieldType.Name;
                if (typeName == "Boolean")
                {
                    typeName = "boolean";
                }
                
                if (typeName == "Action")
                {
                    typeName = "() => void";
                }
                
                if (typeName == "Int32")
                {
                    typeName = "number";
                }

                if (typeName == "ReactNode")
                {
                    tsxFile.Imports.Add(new (){ ClassName = "React", Package = "react"});
                }
                
                file.AppendLine($"{fieldDefinition.Name} : {typeName}");
            }
            file.AppendLine("}");

            propsParameterTypeName = propsTypeDefinition.Name;
        }

        file.AppendLine();
        file.AppendLine($"export default function {state.ComponentName}({(propsParameterTypeName.HasNoValue() ? string.Empty  : "props: " +propsParameterTypeName )}) {{");

        foreach (var line in tsxFile.Body)
        {
            file.AppendLine(indent + line);
        }

        file.AppendLine($"{indent}return (");
        WriteTo(file, rootNode,2);
        file.AppendLine($"{indent});");

        file.AppendLine("}");

        File.WriteAllText($"C:\\github\\hopgogo\\web\\enduser-ui\\src\\components\\{state.ComponentName}.tsx", importLines.ToString() + file.ToString());
    }

    static string GenerateHtml(Context context, VisualElementModel element, int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 4);

        var sb = new StringBuilder();

        List<string> classNames = [];

        // Open tag
        var tag = element.Tag;

        if (tag == "a")
        {
            context.Imports.Add("Link", "next/link");
            tag = "Link";
        }
        
        if (tag == "img")
        {
            context.Imports.Add("Image", "next/image");
            tag = "Image";
        }

        sb.Append($"{indent}<{tag}");

        // Add properties
        foreach (var property in element.Properties)
        {
            var (success, name, value) = TryParsePropertyValue(property);
            if (success)
            {
                if (name == "class")
                {
                    classNames.AddRange(value.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                    continue;
                }
                
                if (name == "w" || name == "width")
                {
                    sb.Append($" width={{{value}}}");
                    continue;
                }
                if (name == "h" || name == "height")
                {
                    sb.Append($" height={{{value}}}");
                    continue;
                }

                if (tag == "Image")
                {
                    value = "/"+value; // todo: fixme
                }

                if (value.StartsWith("props."))
                {
                    sb.Append($" {name}={{{value}}}");
                    continue;
                }

                sb.Append($" {name}=\"{value}\"");
            }
        }

        foreach (var styleGroup in element.StyleGroups)
        {
            foreach (var styleItem in styleGroup.Items)
            {
                var (success, name, value) = TryParsePropertyValue(styleItem);
                if (success)
                {
                    if (name == "W" || name == "w" || name == "width")
                    {
                        if (value == "fit-content")
                        {
                            classNames.Add("w-fit");
                            continue;
                        }

                        classNames.Add($"w-[{value}px]");
                        continue;
                    }

                    if (name == "h" || name == "height")
                    {
                        if (value == "fit-content")
                        {
                            classNames.Add("h-fit");
                            continue;
                        }

                        classNames.Add($"h-[{value}px]");
                        continue;
                    }
                    
                    if (name == "max-width")
                    {
                        classNames.Add($"max-w-[{value}px]");
                        continue;
                    }
                    
                    if (name == "max-height")
                    {
                        classNames.Add($"max-h-[{value}px]");
                        continue;
                    }

                    if (name == "pt")
                    {
                        classNames.Add($"pt-[{value}px]");
                        continue;
                    }
                    if (name == "z-index")
                    {
                        classNames.Add($"z-[{value}]");
                        continue;
                    }
                    
                    if (name == "overflow-y" ||name == "overflow-x")
                    {
                        classNames.Add($"{name}-{value}");
                        continue;
                    }

                    if (name == "pb")
                    {
                        classNames.Add($"pb-[{value}px]");
                        continue;
                    }

                    if (name == "pl")
                    {
                        classNames.Add($"pl-[{value}px]");
                        continue;
                    }

                    if (name == "p")
                    {
                        classNames.Add($"p-[{value}px]");
                        continue;
                    }
                    
                    if (name == "border-top-left-radius")
                    {
                        classNames.Add($"rounded-tl-[{value}px]");
                        continue;
                    }
                    if (name == "border-top-right-radius")
                    {
                        classNames.Add($"rounded-tr-[{value}px]");
                        continue;
                    }
                    if (name == "border-bottom-left-radius")
                    {
                        classNames.Add($"rounded-bl-[{value}px]");
                        continue;
                    }
                    if (name == "border-bottom-right-radius")
                    {
                        classNames.Add($"rounded-br-[{value}px]");
                        continue;
                    }
                    
                    if (name == "flex-grow")
                    {
                        classNames.Add($"flex-grow-[{value}]");
                        continue;
                    }
                    
                    if (name == "border-bottom-width")
                    {
                        classNames.Add($"border-b-[{value}px]");
                        continue;
                    }
                    
                    if (name == "border-top-width")
                    {
                        classNames.Add($"border-t-[{value}px]");
                        continue;
                    }
                    if (name == "border-left-width")
                    {
                        classNames.Add($"border-l-[{value}px]");
                        continue;
                    }
                    if (name == "border-right-width")
                    {
                        classNames.Add($"border-r-[{value}px]");
                        continue;
                    }
                    
                    if (name == "pr")
                    {
                        classNames.Add($"pr-[{value}px]");
                        continue;
                    }

                    if (name == "px")
                    {
                        classNames.Add($"px-[{value}px]");
                        continue;
                    }

                    if (name == "py")
                    {
                        classNames.Add($"py-[{value}px]");
                        continue;
                    }

                    if (name == "display")
                    {
                        classNames.Add($"{value}");
                        continue;
                    }

                    if (name == "color")
                    {
                        if (Project.Colors.TryGetValue(value, out var htmlColor))
                        {
                            value = htmlColor;
                        }

                        classNames.Add($"text-[{value}]");
                        continue;
                    }

                    if (name == "gap")
                    {
                        classNames.Add($"gap-[{value}px]");
                        continue;
                    }
                    
                    if (name == "size")
                    {
                        classNames.Add($"size-[{value}px]");
                        continue;
                    }
                    
                    if (name == "bottom" || name == "top" || name == "left" || name == "right")
                    {
                        classNames.Add($"{name}-[{value}px]");
                        continue;
                    }

                    if (name == "flex-direction")
                    {
                        if (value == "column")
                        {
                            classNames.Add("flex-col");
                            continue;
                        }
                    }

                    if (name == "align-items")
                    {
                        classNames.Add($"items-{value.RemoveFromStart("align-")}");
                        continue;
                    }

                    if (name == "justify-content")
                    {
                        if (value == "space-between")
                        {
                            classNames.Add("justify-between");
                            continue;
                        }
                    }

                    if (name == "border-radius")
                    {
                        classNames.Add($"rounded-[{value}px]");
                        continue;
                    }

                    if (name == "font-size")
                    {
                        classNames.Add($"[font-size:{value}px]");
                        continue;
                    }

                    if (name == "border")
                    {
                        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 3)
                        {
                            if (Project.Colors.TryGetValue(parts[2], out var htmlColor))
                            {
                                parts[2] = htmlColor;
                            }

                            if (parts[0] == "1px" && parts[1] == "solid")
                            {
                                classNames.Add("border");
                                classNames.Add($"border-[{parts[2]}]");
                                continue;
                            }

                            classNames.Add($"border-[{parts[0]}]");
                            classNames.Add($"border-[{parts[1]}]");
                            classNames.Add($"border-[{parts[2]}]");

                            continue;
                        }
                    }
                    
                    if (name == "background" || name == "bg")
                    {
                        if (Project.Colors.TryGetValue(value, out var htmlColor))
                        {
                            value = htmlColor;
                        }

                        classNames.Add($"bg-[{value}]");
                        continue;
                    }
                    
                    if (name == "position")
                    {
                        classNames.Add($"{value}");
                        continue;
                    }
                }

                classNames.Add(styleItem);
            }
        }

        if (classNames.Any())
        {
            sb.Append($" className=\"{string.Join(" ", classNames)}\"");
        }

        var hasSelfClose = element.Children.Count == 0 && element.Text.HasNoValue();
        if (hasSelfClose)
        {
            sb.AppendLine(" />");
            return sb.ToString();
        }

        sb.AppendLine(">");

        // Add text content
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            if (context.Imports.All(x => x.ClassName != "useTranslations"))
            {
                context.Imports.Add("useTranslations", "next-intl", true);

                context.Body.Add("const t = useTranslations();");
            }

            sb.AppendLine($"{indent}{{t(\"{element.Text}\")}}");
        }

        // Add children
        foreach (var child in element.Children)
        {
            sb.Append(GenerateHtml(context, child, indentLevel + 1));
        }

        // Close tag
        sb.AppendLine($"{indent}</{tag}>");

        return sb.ToString();
    }

    static void WriteTo(StringBuilder sb, ReactNode node, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        
        sb.Append($"{indent}<{node.Tag}");
        
        foreach (var reactProperty in node.Properties)
        {
            sb.Append($" {reactProperty.Name}={{{reactProperty.Value}}}");
        }
        
        var hasSelfClose = node.Children.Count == 0 && node.Text.HasNoValue();
        if (hasSelfClose)
        {
            sb.AppendLine(" />");
            return;
        }
        
        // Add text content
        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            sb.AppendLine($"{indent}{{{node.Text})}}");
        }
        
        // Add children
        foreach (var child in node.Children)
        {
            WriteTo(sb, child, indentLevel + 1);
        }

        // Close tag
        sb.AppendLine($"{indent}</{node.Tag}>");
    }
    static ReactNode ConvertToReactNode(SourceFile context, VisualElementModel element)
    {
 

        List<string> classNames = [];

        // Open tag
        var tag = element.Tag;

        if (tag == "a")
        {
            context.Imports.Add("Link", "next/link");
            tag = "Link";
        }
        
        if (tag == "img")
        {
            context.Imports.Add("Image", "next/image");
            tag = "Image";
        }

        var node = new ReactNode { Tag = tag };

        // Add properties
        foreach (var property in element.Properties)
        {
            var (success, name, value) = TryParsePropertyValue(property);
            if (success)
            {
                if (name == "class")
                {
                    classNames.AddRange(value.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                    continue;
                }
                
                if (name == "w" || name == "width")
                {
                    node.Properties.Add(new (){ Name = "width", Value = value});
                    continue;
                }
                if (name == "h" || name == "height")
                {
                    node.Properties.Add(new (){ Name = "height", Value = value});
                    continue;
                }

                if (tag == "Image")
                {
                    value = "/"+value; // todo: fixme
                }

                if (value.StartsWith("props."))
                {
                    node.Properties.Add(new (){ Name = name, Value = value});
                    continue;
                }

                node.Properties.Add(new (){ Name = name, Value = '"' + value + '"'});
            }
        }

        foreach (var styleGroup in element.StyleGroups)
        {
            foreach (var styleItem in styleGroup.Items)
            {
                var (success, name, value) = TryParsePropertyValue(styleItem);
                if (success)
                {
                    if (name == "W" || name == "w" || name == "width")
                    {
                        if (value == "fit-content")
                        {
                            classNames.Add("w-fit");
                            continue;
                        }

                        classNames.Add($"w-[{value}px]");
                        continue;
                    }

                    if (name == "h" || name == "height")
                    {
                        if (value == "fit-content")
                        {
                            classNames.Add("h-fit");
                            continue;
                        }

                        classNames.Add($"h-[{value}px]");
                        continue;
                    }
                    
                    if (name == "max-width")
                    {
                        classNames.Add($"max-w-[{value}px]");
                        continue;
                    }
                    
                    if (name == "max-height")
                    {
                        classNames.Add($"max-h-[{value}px]");
                        continue;
                    }

                    if (name == "pt")
                    {
                        classNames.Add($"pt-[{value}px]");
                        continue;
                    }
                    if (name == "z-index")
                    {
                        classNames.Add($"z-[{value}]");
                        continue;
                    }
                    
                    if (name == "overflow-y" ||name == "overflow-x")
                    {
                        classNames.Add($"{name}-{value}");
                        continue;
                    }

                    if (name == "pb")
                    {
                        classNames.Add($"pb-[{value}px]");
                        continue;
                    }

                    if (name == "pl")
                    {
                        classNames.Add($"pl-[{value}px]");
                        continue;
                    }

                    if (name == "p")
                    {
                        classNames.Add($"p-[{value}px]");
                        continue;
                    }
                    
                    if (name == "border-top-left-radius")
                    {
                        classNames.Add($"rounded-tl-[{value}px]");
                        continue;
                    }
                    if (name == "border-top-right-radius")
                    {
                        classNames.Add($"rounded-tr-[{value}px]");
                        continue;
                    }
                    if (name == "border-bottom-left-radius")
                    {
                        classNames.Add($"rounded-bl-[{value}px]");
                        continue;
                    }
                    if (name == "border-bottom-right-radius")
                    {
                        classNames.Add($"rounded-br-[{value}px]");
                        continue;
                    }
                    
                    if (name == "flex-grow")
                    {
                        classNames.Add($"flex-grow-[{value}]");
                        continue;
                    }
                    
                    if (name == "border-bottom-width")
                    {
                        classNames.Add($"border-b-[{value}px]");
                        continue;
                    }
                    
                    if (name == "border-top-width")
                    {
                        classNames.Add($"border-t-[{value}px]");
                        continue;
                    }
                    if (name == "border-left-width")
                    {
                        classNames.Add($"border-l-[{value}px]");
                        continue;
                    }
                    if (name == "border-right-width")
                    {
                        classNames.Add($"border-r-[{value}px]");
                        continue;
                    }
                    
                    if (name == "pr")
                    {
                        classNames.Add($"pr-[{value}px]");
                        continue;
                    }

                    if (name == "px")
                    {
                        classNames.Add($"px-[{value}px]");
                        continue;
                    }

                    if (name == "py")
                    {
                        classNames.Add($"py-[{value}px]");
                        continue;
                    }

                    if (name == "display")
                    {
                        classNames.Add($"{value}");
                        continue;
                    }

                    if (name == "color")
                    {
                        if (Project.Colors.TryGetValue(value, out var htmlColor))
                        {
                            value = htmlColor;
                        }

                        classNames.Add($"text-[{value}]");
                        continue;
                    }

                    if (name == "gap")
                    {
                        classNames.Add($"gap-[{value}px]");
                        continue;
                    }
                    
                    if (name == "size")
                    {
                        classNames.Add($"size-[{value}px]");
                        continue;
                    }
                    
                    if (name == "bottom" || name == "top" || name == "left" || name == "right")
                    {
                        classNames.Add($"{name}-[{value}px]");
                        continue;
                    }

                    if (name == "flex-direction")
                    {
                        if (value == "column")
                        {
                            classNames.Add("flex-col");
                            continue;
                        }
                    }

                    if (name == "align-items")
                    {
                        classNames.Add($"items-{value.RemoveFromStart("align-")}");
                        continue;
                    }

                    if (name == "justify-content")
                    {
                        if (value == "space-between")
                        {
                            classNames.Add("justify-between");
                            continue;
                        }
                    }

                    if (name == "border-radius")
                    {
                        classNames.Add($"rounded-[{value}px]");
                        continue;
                    }

                    if (name == "font-size")
                    {
                        classNames.Add($"[font-size:{value}px]");
                        continue;
                    }

                    if (name == "border")
                    {
                        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 3)
                        {
                            if (Project.Colors.TryGetValue(parts[2], out var htmlColor))
                            {
                                parts[2] = htmlColor;
                            }

                            if (parts[0] == "1px" && parts[1] == "solid")
                            {
                                classNames.Add("border");
                                classNames.Add($"border-[{parts[2]}]");
                                continue;
                            }

                            classNames.Add($"border-[{parts[0]}]");
                            classNames.Add($"border-[{parts[1]}]");
                            classNames.Add($"border-[{parts[2]}]");

                            continue;
                        }
                    }
                    
                    if (name == "background" || name == "bg")
                    {
                        if (Project.Colors.TryGetValue(value, out var htmlColor))
                        {
                            value = htmlColor;
                        }

                        classNames.Add($"bg-[{value}]");
                        continue;
                    }
                    
                    if (name == "position")
                    {
                        classNames.Add($"{value}");
                        continue;
                    }
                }

                classNames.Add(styleItem);
            }
        }

        if (classNames.Any())
        {
            node.Properties.Add(new() { Name = "className", Value = '"' + string.Join(" ", classNames) + '"' });
        }

        var hasSelfClose = element.Children.Count == 0 && element.Text.HasNoValue();
        if (hasSelfClose)
        {
            return node;
        }


        // Add text content
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            if (context.Imports.All(x => x.ClassName != "useTranslations"))
            {
                context.Imports.Add("useTranslations", "next-intl", true);

                context.Body.Add("const t = useTranslations();");
            }
            node.Children.Add(new ReactNode{ Text = $"{{t(\"{element.Text}\")}}"});
        }

        // Add children
        foreach (var child in element.Children)
        {
            node.Children.Add(ConvertToReactNode(context, child));
        }


        return node;
    }

    sealed class Context
    {
        public List<string> Body { get; } = new();
        public Imports Imports { get; } = new();
    }

    class Imports : List<ImportInfo>
    {
        public void Add(string className, string package, bool isNamed = false)
        {
            var import = this.FirstOrDefault(i => i.ClassName == className && i.Package == package);
            if (import == null)
            {
                Add(new() { ClassName = className, Package = package, IsNamed = isNamed });
            }
        }
    }

    record ImportInfo
    {
        public string ClassName { get; init; }
        public bool IsNamed { get; init; }
        public string Package { get; init; }
    }
}