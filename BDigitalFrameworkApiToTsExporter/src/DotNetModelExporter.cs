using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    
    static IAsyncEnumerable<Result<FileModel>> ExportController(TypeDefinition controllerTypeDefinition)
    {

        var temp =
            from modelTypeDefinition in getModelTypeDefinition()
            from method in getExportablePublicMethods()
            let requestType = getMethodRequest(method)
            //let responseType = getMethodResponseType(method)
            select new
            {
                model = modelTypeDefinition
            };
        
        // modelType = GetModel(controllerDefinition)
        // publicMethod = GetPublicMethods(controllerDefinition)
        // requestType = GetRequest(publicMethod)
        // responseType = GetResponse(publicMethod)
        // serviceWrapper = GetServiceWrapper(publicMethod)
        // serviceWrapperByModel = getWrapperByModel(serviceWrapper)
        // return model

        return null;

        ParameterDefinition? getMethodRequest(MethodDefinition methodDefinition)
        {
            if (methodDefinition.Parameters.Count == 1)
            {
                return methodDefinition.Parameters[0];
            }

            return null;
        }
        
        TypeReference getMethodResponseType(MethodDefinition methodDefinition)
        {
            return methodDefinition.ReturnType;
        }
        
        IEnumerable<MethodDefinition> getExportablePublicMethods()
        {
            return from method in controllerTypeDefinition.Methods where method.IsPublic select method;
        }
        
        Result<TypeDefinition> getModelTypeDefinition()
        {
            // todo: find 
            var modelTypeFullName = controllerTypeDefinition.FullName;
            
            var assemblyDefinition = controllerTypeDefinition.Module.Assembly;

            var typeDefinition =
            (
                from module in assemblyDefinition.Modules
                from type in module.Types
                where type.FullName == modelTypeFullName
                select type
            ).FirstOrDefault();

            if (typeDefinition is null)
            {
                return new MissingMemberException(modelTypeFullName);
            }

            return typeDefinition;
        }
    }
    
    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        var fileModels =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
            from outputFilePath in GetOutputFilePath(config, typeDefinition)
            let fileContent = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreateFrom(typeDefinition)))
            select new FileModel
            {
                Path    = outputFilePath,
                Content = fileContent
            };

        return
            from fileModel in fileModels
            from syncedFile in TrySyncWithLocalFileSystem(fileModel)
            select FileSystem.Save(syncedFile);
        
        
        static Result<string> GetOutputFilePath(Config config, TypeDefinition typeDefinition)
        {
            return Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts");
        }
        
        static async Task<Result<FileModel>> TrySyncWithLocalFileSystem(FileModel file)
        {
            if (!File.Exists(file.Path))
            {
                return file;
            }

            return await(
                from fileContentInDirectory in FileSystem.ReadAllText(file.Path)
                let exportIndex = fileContentInDirectory.IndexOf("export ", StringComparison.OrdinalIgnoreCase)
                select (exportIndex > 0) switch
                {
                    true => file with
                    {
                        Content = fileContentInDirectory[..exportIndex] + file.Content
                    },
                    false => file
                });
        }
    }
    
    static IEnumerable<PropertyDefinition> GetMappingPropertyList(TypeDefinition model, TypeDefinition apiParameter)
    {
        return
            from parameterProperty in apiParameter.Properties
            where modelHasNamedProperty(model, parameterProperty)
            select parameterProperty;
        
        static bool modelHasNamedProperty(TypeDefinition model, PropertyDefinition property)
        {
            foreach (var modelProperty in model.Properties)
            {
                if (string.Equals(modelProperty.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    

    
}