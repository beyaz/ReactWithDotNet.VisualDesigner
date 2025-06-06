﻿using Mono.Cecil;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace ReactWithDotNet.VisualDesigner.Views;

static class CecilHelper
{
    // ReSharper disable once UnusedParameter.Local
    static AssemblyDefinition GetAssemblyDefinition(ApplicationState state)
    {
        return AssemblyDefinition.ReadAssembly(typeof(CecilHelper).Assembly.Location);
    }
    
    public static TypeDefinition GetPropsTypeDefinition(ApplicationState state)
    {
        var searchTypeName = "todo";
        
        var assembly = GetAssemblyDefinition(state);
        
        foreach (var moduleDefinition in assembly.Modules)
        {
            foreach (var typeDefinition in moduleDefinition.Types)
            {
                
                if (typeDefinition.Name == searchTypeName)
                {
                    return typeDefinition;
                }
            }
        }

        return null;
    }
    
    public static TypeDefinition GetStateTypeDefinition(ApplicationState state)
    {
        var searchTypeName = "todo";
        
        var assembly = GetAssemblyDefinition(state);
        
        foreach (var moduleDefinition in assembly.Modules)
        {
            foreach (var typeDefinition in moduleDefinition.Types)
            {
                if (typeDefinition.Name == searchTypeName)
                {
                    return typeDefinition;
                }
            }
        }

        return null;
    }
    
    public static ICollection<FieldDefinition> GetDefinedProps(ApplicationState state)
    {
        var typeDefinition = GetPropsTypeDefinition(state);

        if (typeDefinition is null)
        {
            return [];
        }

        return typeDefinition.Fields;
    }
    
    static bool IsDelegate(TypeDefinition type)
    {
        return type.BaseType != null &&
               (type.BaseType.FullName == "System.MulticastDelegate" || type.BaseType.FullName == "System.Delegate");
    }
    
    static bool IsDelegate(TypeReference type)
    {
        return IsDelegate(type.Resolve());
    }
    
    public static IEnumerable<string> GetPropsSuggestions(TypeDefinition selectedTag, ApplicationState state)
    {
        foreach (var targetField in selectedTag.Fields)
        {
            if (targetField.FieldType.FullName == typeof(bool).FullName)
            {
                foreach (var fieldDefinition in GetDefinedProps(state))
                {
                    if (fieldDefinition.FieldType.FullName == typeof(bool).FullName)
                    {
                        yield return $"{targetField.Name}: props.{fieldDefinition.Name}";
                    }
                }
            }
            
            if (IsDelegate(targetField.FieldType) )
            {
                foreach (var fieldDefinition in GetDefinedProps(state))
                {
                    if (IsDelegate(fieldDefinition.FieldType))
                    {
                        yield return $"{targetField.Name}: props.{fieldDefinition.Name}";
                    }
                }
            }
        }
    }
    
    public static IEnumerable<string> GetPropsSuggestions(ApplicationState state)
    {
        TypeDefinition definition = null;
        
        string tag = null;
        if (state.Selection.VisualElementTreeItemPath.HasValue())
        {
            var selectedVisualItem = FindTreeNodeByTreePath(state.ComponentRootElement, state.Selection.VisualElementTreeItemPath);

            tag = selectedVisualItem.Tag;
        }
        
        foreach (var moduleDefinition in GetAssemblyDefinition(state).Modules)
        {
            foreach (var typeDefinition in moduleDefinition.Types)
            {
                if (typeDefinition.Name == tag || typeDefinition.Name == tag+"Props")
                {
                    definition = typeDefinition;
                    break;
                }
            }
        }

        if (definition is null)
        {
            return [];
        }
        return GetPropsSuggestions(definition, state);
    }
}