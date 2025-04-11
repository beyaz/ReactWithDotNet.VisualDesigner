﻿using System.IO;
using System.Text;
using Mono.Cecil;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Exporter_For_NextJs_with_Tailwind
{
    const string childrenIdentifier = "[...]";

    public static void Export(ApplicationState state)
    {
        var sourceFile = new ComponentDefinition
        {
            ComponentName = state.ComponentName
        };

        var propsTypeDefinition = GetPropsTypeDefinition(state);
        if (propsTypeDefinition is not null)
        {
            var decleration = ConvertToInterfaceDecleration(propsTypeDefinition);
            
            decleration.Identifier = "Props";

            sourceFile.PropsDecleration = decleration;

            foreach (var import in decleration.Imports)
            {
                sourceFile.Imports.Add(import.ClassName, import.Package, import.IsNamed);
            }

            sourceFile.PropsParameterTypeName = propsTypeDefinition.Name;
        }
        
        var stateTypeDefinition = GetStateTypeDefinition(state);
        if (stateTypeDefinition is not null)
        {
            var decleration = ConvertToInterfaceDecleration(stateTypeDefinition);

            decleration.Identifier = "State";
            
            sourceFile.StateDecleration = decleration;

            foreach (var import in decleration.Imports)
            {
                sourceFile.Imports.Add(import.ClassName, import.Package, import.IsNamed);
            }
            
            sourceFile.Imports.Add("useState","react", true);

            sourceFile.HasState = true;

        }
        
        

        sourceFile.RootNode = ConvertToReactNode(state.ProjectId, state.UserName, sourceFile, state.ComponentRootElement);

        if (stateTypeDefinition is not null)
        {
            
            sourceFile.Imports.Add(new(){ ClassName = "initializeState, logic", Package = $"@/components/{state.ComponentName}.Logic", IsNamed = true});
            
           
            sourceFile.Body.Add("");
            sourceFile.Body.Add("const call = (name: string) =>");
            sourceFile.Body.Add("{");
            sourceFile.Body.Add("    setState((prevState: State) =>");
            sourceFile.Body.Add("    {");
            sourceFile.Body.Add("        logic(props, prevState)[name]();");
            sourceFile.Body.Add("");
            sourceFile.Body.Add("        return { ...prevState };");
            sourceFile.Body.Add("    });");
            sourceFile.Body.Add("};");
            
        }
       
        
        var fileContent = new StringBuilder();

        WriteTo(fileContent, sourceFile);

        File.WriteAllText($"{GetExportFolderPath()}{sourceFile.ComponentName}.tsx", fileContent.ToString());
    }

    static InterfaceDecleration ConvertToInterfaceDecleration(TypeDefinition typeDefinition)
    {
        var decleration = new InterfaceDecleration
        {
            Identifier = typeDefinition.Name
        };

        foreach (var fieldDefinition in typeDefinition.Fields)
        {
            var fieldType = fieldDefinition.FieldType;

            var isNullable = false;

          
            
            if (fieldType is GenericInstanceType genericType &&
                genericType.ElementType.FullName == "System.Nullable`1" &&
                genericType.GenericArguments.Count == 1)
            {
                TypeReference elementType = genericType.GenericArguments[0];
                
                isNullable = true;

                fieldType = elementType;
            }
            

            var typeName = fieldType.Name;
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
                typeName = "React.ReactNode";

                decleration.Imports.Add(new() { ClassName = "React", Package = "react" });
            }

            decleration.PropertySignatures.Add(new() { Identifier = fieldDefinition.Name, Type = typeName, IsNullable = isNullable });
        }

        return decleration;
    }

    static ImportInfo GetTagImportInfo(int projectId, string userName, string tag)
    {
       
        
        var component = GetComponenUserOrMainVersion(projectId, tag, userName);
        if (component is not null)
        {
            return new() { ClassName = tag.Split('/').Last(), Package = $"@/components/{tag}" };
        }

        if (tag == "Popover" || tag == "PopoverTrigger" || tag == "PopoverContent" || tag == "heroui/Checkbox")
        {
            return new() { ClassName = tag.Split('/').Last(), Package = "@heroui/react", IsNamed = true}; 
        }

        return null;
    }
    static ReactNode ConvertToReactNode(int projectId, string userName, ComponentDefinition componentDefinition, VisualElementModel element)
    {
        List<string> classNames = [];

        // Open tag
        var tag = element.Tag;

        if (tag == "a")
        {
            componentDefinition.Imports.Add("Link", "next/link");
            tag = "Link";
        }

        if (tag == "img")
        {
            componentDefinition.Imports.Add("Image", "next/image");
            tag = "Image";
        }

        componentDefinition.Imports.TryAdd(GetTagImportInfo(projectId, userName, tag));
        

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
                    node.Properties.Add(new() { Name = "width", Value = value });
                    continue;
                }

                if (name == "h" || name == "height")
                {
                    node.Properties.Add(new() { Name = "height", Value = value });
                    continue;
                }

                if (tag == "Image")
                {
                    value = "/" + value; // todo: fixme
                }

                if (value.StartsWith("props.") || value.StartsWith("state."))
                {
                    node.Properties.Add(new() { Name = name, Value = value });
                    continue;
                }
                
                var component = GetComponenUserOrMainVersion(projectId, tag, userName);
                if (component is not null)
                {
                    node.Properties.Add(new() { Name = name, Value = $"()=>call('{value}')" });
                    continue;
                }

                node.Properties.Add(new() { Name = name, Value = '"' + value + '"' });
            }
        }

        foreach (var styleGroup in element.StyleGroups)
        {
            foreach (var styleItem in styleGroup.Items)
            {
                var (success, name, value) = TryParsePropertyValue(styleItem);
                if (success)
                {
                    switch (name)
                    {
                        case "W":
                        case "w":
                        case "width":
                        {
                            if (value == "fit-content")
                            {
                                classNames.Add("w-fit");
                                continue;
                            }

                            classNames.Add($"w-[{value}px]");
                            continue;
                        }
                        case "h":
                        case "height":
                        {
                            if (value == "fit-content")
                            {
                                classNames.Add("h-fit");
                                continue;
                            }

                            classNames.Add($"h-[{value}px]");
                            continue;
                        }
                        
                        case "max-width":
                            classNames.Add($"max-w-[{value}px]");
                            continue;
                        case "max-height":
                            classNames.Add($"max-h-[{value}px]");
                            continue;
                        case "min-width":
                            classNames.Add($"min-w-[{value}px]");
                            continue;
                        case "min-height":
                            classNames.Add($"min-h-[{value}px]");
                            continue;
                        case "pt":
                            classNames.Add($"pt-[{value}px]");
                            continue;
                        case "z-index":
                            classNames.Add($"z-[{value}]");
                            continue;
                        case "overflow-y":
                        case "overflow-x":
                            classNames.Add($"{name}-{value}");
                            continue;
                        case "pb":
                            classNames.Add($"pb-[{value}px]");
                            continue;
                        case "pl":
                            classNames.Add($"pl-[{value}px]");
                            continue;
                        case "p":
                            classNames.Add($"p-[{value}px]");
                            continue;
                        case "border-top-left-radius":
                            classNames.Add($"rounded-tl-[{value}px]");
                            continue;
                        case "border-top-right-radius":
                            classNames.Add($"rounded-tr-[{value}px]");
                            continue;
                        case "border-bottom-left-radius":
                            classNames.Add($"rounded-bl-[{value}px]");
                            continue;
                        case "border-bottom-right-radius":
                            classNames.Add($"rounded-br-[{value}px]");
                            continue;
                        case "flex-grow":
                            classNames.Add($"flex-grow-[{value}]");
                            continue;
                        case "border-bottom-width":
                            classNames.Add($"border-b-[{value}px]");
                            continue;
                        case "border-top-width":
                            classNames.Add($"border-t-[{value}px]");
                            continue;
                        case "border-left-width":
                            classNames.Add($"border-l-[{value}px]");
                            continue;
                        case "border-right-width":
                            classNames.Add($"border-r-[{value}px]");
                            continue;
                        case "pr":
                            classNames.Add($"pr-[{value}px]");
                            continue;
                        case "px":
                            classNames.Add($"px-[{value}px]");
                            continue;
                        case "py":
                            classNames.Add($"py-[{value}px]");
                            continue;
                        case "display":
                            classNames.Add($"{value}");
                            continue;
                        case "color":
                        {
                            if (Project.Colors.TryGetValue(value, out var htmlColor))
                            {
                                value = htmlColor;
                            }

                            classNames.Add($"text-[{value}]");
                            continue;
                        }
                        case "gap":
                            classNames.Add($"gap-[{value}px]");
                            continue;
                        case "size":
                            classNames.Add($"size-[{value}px]");
                            continue;
                        case "bottom":
                        case "top":
                        case "left":
                        case "right":
                            classNames.Add($"{name}-[{value}px]");
                            continue;
                        case "flex-direction" when value == "column":
                            classNames.Add("flex-col");
                            continue;
                        case "align-items":
                            classNames.Add($"items-{value.RemoveFromStart("align-")}");
                            continue;
                        case "justify-content":
                            classNames.Add($"justify-{value.Split('-').Last()}");
                            continue;
                        case "border-radius":
                            classNames.Add($"rounded-[{value}px]");
                            continue;
                        case "font-size":
                            classNames.Add($"[font-size:{value}px]");
                            continue;
                        case "border":
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

                            break;
                        }
                        case "background":
                        case "bg":
                        {
                            if (Project.Colors.TryGetValue(value, out var htmlColor))
                            {
                                value = htmlColor;
                            }

                            classNames.Add($"bg-[{value}]");
                            continue;
                        }
                        case "position":
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

        if (element.Text == childrenIdentifier && componentDefinition.HasChildrenDeclerationInProps())
        {
            node.Children.Add(new() { Text = element.Text });

            return node;
        }

        if ((element.Text + string.Empty).StartsWith("props.") || (element.Text + string.Empty).StartsWith("state."))
        {
            node.Children.Add(new() { Text = element.Text });

            return node;
        }

        // Add text content
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            if (componentDefinition.Imports.All(x => x.ClassName != "useTranslations"))
            {
                componentDefinition.Imports.Add("useTranslations", "next-intl", true);

                componentDefinition.Body.Add("");
                componentDefinition.Body.Add("const t = useTranslations();");
            }

            node.Children.Add(new() { Text = $"{{t(\"{element.Text}\")}}" });
        }

        // Add children
        foreach (var child in element.Children)
        {
            node.Children.Add(ConvertToReactNode(projectId, userName, componentDefinition, child));
        }

        return node;
    }

    static string GetExportFolderPath()
    {
        return "C:\\github\\hopgogo\\web\\enduser-ui\\src\\components\\";
    }

    static bool HasChildrenDeclerationInProps(this ComponentDefinition definition)
    {
        return definition.PropsDecleration?.PropertySignatures.Any(x => x.Identifier == "children") is true;
    }

    static string Indent(int indentLevel)
    {
        return new(' ', indentLevel * 4);
    }

    static void WriteTo(this InterfaceDecleration decleration, StringBuilder sb, int indentLevel)
    {
        sb.AppendLine($"{Indent(indentLevel)}export interface {decleration.Identifier} {{");

        indentLevel++;

        foreach (var propertySignature in decleration.PropertySignatures)
        {
            sb.AppendLine($"{Indent(indentLevel)}{propertySignature.Identifier}{(propertySignature.IsNullable ? "?" : "")}: {propertySignature.Type};");
        }

        indentLevel--;

        sb.AppendLine(Indent(indentLevel) + "}");
    }

    static void WriteTo(StringBuilder sb, IReadOnlyList<ImportInfo> imports)
    {
        foreach (var import in imports)
        {
            if (import.IsNamed)
            {
                sb.AppendLine($"import {{ {import.ClassName} }} from \"{import.Package}\";");
            }
            else
            {
                sb.AppendLine($"import {import.ClassName} from \"{import.Package}\";");
            }
        }
    }

    static void WriteTo(StringBuilder file, ComponentDefinition componentDefinition)
    {
        WriteTo(file, componentDefinition.Imports);

        if (componentDefinition.PropsDecleration is not null)
        {
            file.AppendLine();
            componentDefinition.PropsDecleration.WriteTo(file, 0);
        }
        
        if (componentDefinition.StateDecleration is not null)
        {
            file.AppendLine();
            componentDefinition.StateDecleration.WriteTo(file, 0);
        }

        file.AppendLine();
        file.AppendLine($"export default function {componentDefinition.ComponentName.Split('/').Last()}({(componentDefinition.PropsParameterTypeName.HasNoValue() ? string.Empty : "props: Props")}) {{");

        foreach (var line in componentDefinition.Body)
        {
            file.AppendLine($"{Indent(1)}{line}");
        }
        
      

        if (componentDefinition.HasState)
        {
            file.AppendLine();
            file.AppendLine($"{Indent(1)}const [state, setState] = useState<State>(() => initializeState(props));");
        }

        file.AppendLine();
        file.AppendLine($"{Indent(1)}return (");
        WriteTo(file, componentDefinition, componentDefinition.RootNode, 2);
        file.AppendLine($"{Indent(1)});");

        file.AppendLine("}");
    }

    static void WriteTo(StringBuilder sb, ComponentDefinition componentDefinition, ReactNode node, int indentLevel)
    {
        var nodeTag = node.Tag;
        
        if (nodeTag is null)
        {
            if (node.Text.HasValue())
            {
                sb.AppendLine(Indent(indentLevel) + node.Text);
                return;
            }

            throw new ArgumentNullException(nameof(nodeTag));
        }

        var tag = nodeTag.Split('/').Last();
        
        var indent = new string(' ', indentLevel * 4);

        sb.Append($"{indent}<{tag}");

        foreach (var reactProperty in node.Properties)
        {
            var propertyName = reactProperty.Name;

            var propertyValue = reactProperty.Value;

            if (propertyValue != null && propertyValue[0] == '"')
            {
                sb.Append($" {propertyName}={propertyValue}");    
                continue;
            }
            
            sb.Append($" {propertyName}={{{propertyValue}}}");
        }

        var hasSelfClose = node.Children.Count == 0 && node.Text.HasNoValue();
        if (hasSelfClose)
        {
            sb.AppendLine("/>");
            return;
        }
        
        

        // try add as children
        {
            if (componentDefinition.HasChildrenDeclerationInProps())
            {
                if (node.Children.Count == 1 && node.Children[0].Text == childrenIdentifier)
                {
                    sb.AppendLine(">");

                    sb.AppendLine($"{Indent(indentLevel + 1)}{{ props.children }}");

                    // Close tag
                    sb.AppendLine($"{indent}</{tag}>");

                    return;
                }
            }
        }

        // from props
        {
            if (node.Children.Count == 1 && (node.Children[0].Text + string.Empty).StartsWith("props.") || (node.Children[0].Text + string.Empty).StartsWith("state."))
            {
                sb.AppendLine(">");

                sb.AppendLine($"{Indent(indentLevel + 1)}{{ {node.Children[0].Text} }}");

                // Close tag
                sb.AppendLine($"{indent}</{tag}>");

                return;
            }
        }
        
        sb.AppendLine(">");

        // Add text content
        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            sb.AppendLine($"{indent}{{{node.Text})}}");
        }

        // Add children
        foreach (var child in node.Children)
        {
            WriteTo(sb, componentDefinition, child, indentLevel + 1);
        }

        // Close tag
        sb.AppendLine($"{indent}</{tag}>");
    }

    class ComponentDefinition
    {
        public List<string> Body { get; } = [];
        public string ComponentName { get; init; }
        public Imports Imports { get; } = new();

        public InterfaceDecleration PropsDecleration { get; set; }
        
        public InterfaceDecleration StateDecleration { get; set; }

        public string PropsParameterTypeName { get; set; }

        public bool HasState { get; set; }

        public ReactNode RootNode { get; set; }
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

    static void TryAdd(this List<ImportInfo> items, ImportInfo item)
    {
        if (item is null)
        {
            return;
        }
        var import = items.FirstOrDefault(i => i.ClassName == item.ClassName && i.Package == item.Package);
        if (import == null)
        {
            items.Add(item);
        }
        
    }

    class InterfaceDecleration
    {
        public string Identifier { get; set; }
        public Imports Imports { get; } = new();

        public List<PropertySignature> PropertySignatures { get; } = [];
    }

    class PropertySignature
    {
        public string Identifier { get; init; }
        public bool IsNullable { get; init; }
        public string Type { get; init; }
    }

    class ReactNode
    {
        public List<ReactNode> Children { get; } = [];

        public List<ReactProperty> Properties { get; } = [];
        public string Tag { get; init; }

        public string Text { get; init; }
    }

    class ReactProperty
    {
        public string Name { get; init; }
        public string Value { get; init; }
    }

    record ImportInfo
    {
        public string ClassName { get; init; }
        public bool IsNamed { get; init; }
        public string Package { get; init; }
    }
}