using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    
    static IAsyncEnumerable<Result<FileModel>> ExportController()
    {

                 
        
        var temp =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from api in config.ApiList
            from modelTypeDefinition in getModelTypeDefinition(assemblyDefinition, api)
            from controllerTypeDefinition in getControllerTypeDefinition(assemblyDefinition,api)
            from method in getExportablePublicMethods(controllerTypeDefinition)
            from modelFilePath in getOutputFilePath(config, modelTypeDefinition)
            let requestType = getMethodRequest(method)
            let responseType = getMethodResponseType(method)
            let serviceWrapper = getServiceWrapper(method)
            let serviceWrapperByModel = getServiceWrapperByModel(method,modelTypeDefinition)
            let modelFile = new FileModel
            {
                Path    = modelFilePath,
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreateFrom(modelTypeDefinition)))
            }
            from syncedFile in trySyncWithLocalFileSystem(modelFile)
            select new
            {
                modelTypeDefinition,
                requestType,
                responseType,
                serviceWrapper,
                serviceWrapperByModel
            };
        
        
        // modelType = GetModel(controllerDefinition)
        // publicMethod = GetPublicMethods(controllerDefinition)
        // requestType = GetRequest(publicMethod)
        // responseType = GetResponse(publicMethod)
        // serviceWrapper = GetServiceWrapper(publicMethod)
        // serviceWrapperByModel = getWrapperByModel(serviceWrapper)
        // return model

        return null;

        IReadOnlyList<string> getServiceWrapper(MethodDefinition methodDefinition)
        {
            // todo:  implement
            return [];
        }
        
        IReadOnlyList<string> getServiceWrapperByModel(MethodDefinition methodDefinition, TypeDefinition modelTypeDefinition)
        {
            // todo:  implement
            return [];
        }
        
        
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
        
        static IEnumerable<MethodDefinition> getExportablePublicMethods(TypeDefinition controllerTypeDefinition)
        {
            return from method in controllerTypeDefinition.Methods where method.IsPublic select method;
        }
        
        static Result<TypeDefinition> getModelTypeDefinition(AssemblyDefinition assemblyDefinition, ApiInfo apiInfo)
        {
            // sample: BOA.InternetBanking.Payments.API -> BOA.InternetBanking.Payments.API.Models.GsmPrePaidModel
            
            var fullTypeName = $"{assemblyDefinition.FullName}.Models.{apiInfo.Name}Model";

            return CecilHelper.GetType(assemblyDefinition, fullTypeName);
        }
        
        static Result<TypeDefinition> getControllerTypeDefinition(AssemblyDefinition assemblyDefinition, ApiInfo apiInfo)
        {
            // sample: BOA.InternetBanking.Payments.API -> BOA.InternetBanking.Payments.API.Controllers.GsmPrePaidController
            
            var fullTypeName = $"{assemblyDefinition.FullName}.Controllers.{apiInfo.Name}Controller";

            return CecilHelper.GetType(assemblyDefinition, fullTypeName);
        }
        
        static Result<string> getOutputFilePath(Config config, TypeDefinition typeDefinition)
        {
            return Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts");
        }
        
        static async Task<Result<FileModel>> trySyncWithLocalFileSystem(FileModel file)
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