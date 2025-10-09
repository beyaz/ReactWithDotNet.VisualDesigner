using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    
    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        var query = 
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from api in config.ApiList
            from modelTypeDefinition in getModelTypeDefinition(assemblyDefinition, api)
            from controllerTypeDefinition in getControllerTypeDefinition(assemblyDefinition,api)
            from method in getExportablePublicMethods(controllerTypeDefinition)
            from modelFilePath in getModelOutputTsFilePath(config, modelTypeDefinition)
            let requestType = getMethodRequest(method)
            let responseType = getMethodResponseType(method)
            let serviceWrapper = getServiceWrapper(method)
            let serviceWrapperByModel = getServiceWrapperByModel(method,modelTypeDefinition)
            let modelTsType = TsModelCreator.CreateFrom(config.ExternalTypes, modelTypeDefinition)
            let modelFile = new FileModel
            {
                Path    = modelFilePath,
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(modelTsType))
            }
            from syncedFile in trySyncTsTypeWithLocalFileSystem(modelFile)
            select new
            {
                modelTypeDefinition,
                requestType,
                responseType,
                serviceWrapper,
                serviceWrapperByModel,
                modelFile
            };

        
        return from item in query
               select FileSystem.Save(item.modelFile);


        static Result<FileModel> getServiceFile(ApiInfo apiInfo, TypeDefinition controllerTypeDefinition)
        {
            
        }

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
            
            var fullTypeName = $"{assemblyDefinition.Name.Name}.Models.{apiInfo.Name}Model";

            return CecilHelper.GetType(assemblyDefinition, fullTypeName);
        }
        
        static Result<TypeDefinition> getControllerTypeDefinition(AssemblyDefinition assemblyDefinition, ApiInfo apiInfo)
        {
            // sample: BOA.InternetBanking.Payments.API -> BOA.InternetBanking.Payments.API.Controllers.GsmPrePaidController
            
            var fullTypeName = $"{assemblyDefinition.Name.Name}.Controllers.{apiInfo.Name}Controller";

            return CecilHelper.GetType(assemblyDefinition, fullTypeName);
        }

        static string getSolutionName(string projectDirectory)
        {
            return  Path.GetFileName(projectDirectory);
        }
        static Result<string> getWebProjectFolderPath(string projectDirectory)
        {
            var webProjectName = "OBA.Web." + getSolutionName(projectDirectory).RemoveFromStart("BOA.");
            
            var directory = Path.Combine(projectDirectory, "OBAWeb", webProjectName);
            if (!Directory.Exists(directory))
            {
                return new IOException($"DirectoryNotFound: {directory}");
            }

            return directory;
        }
        
        static Result<string> getModelOutputTsFilePath(Config config, TypeDefinition typeDefinition)
        {
            return 
            from webProjectPath in getWebProjectFolderPath(config.ProjectDirectory)
            select Path.Combine(webProjectPath,  "ClientApp","models",  $"{typeDefinition.Name}.ts");
        }
        
        static async Task<Result<FileModel>> trySyncTsTypeWithLocalFileSystem(FileModel file)
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