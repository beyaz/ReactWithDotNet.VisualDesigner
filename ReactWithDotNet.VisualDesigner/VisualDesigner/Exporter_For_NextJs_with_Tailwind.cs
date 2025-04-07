using System.IO;
using System.Text;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Exporter_For_NextJs_with_Tailwind
{
    public static void Export(ApplicationState state)
    {
        var context = new Context();

        const int indentLevel = 1;

        var indent = new string(' ', indentLevel * 4);

        var partRender = GenerateHtml(context, state.ComponentRootElement, indentLevel);

        var file = new StringBuilder();

        foreach (var import in context.Imports)
        {
            if (import.IsNamed)
            {
                file.AppendLine($"import {{ {import.ClassName} }} from \"{import.Package}\";");
            }
            else
            {
                file.AppendLine($"import {import.ClassName} from \"{import.Package}\";");
            }
        }

        file.AppendLine();

        file.AppendLine($"export default function {state.ComponentName}() {{");

        foreach (var line in context.Body)
        {
            file.AppendLine(indent + line);
        }

        file.AppendLine($"{indent}return (");
        file.AppendLine(partRender);
        file.AppendLine($"{indent});");

        file.AppendLine("}");

        File.WriteAllText($"C:\\github\\hopgogo\\web\\enduser-ui\\src\\components\\{state.ComponentName}.tsx", file.ToString());
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