using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    
    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        var files =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from api in config.ApiList
            from modelTypeDefinition in getModelTypeDefinition(assemblyDefinition, api)
            from controllerTypeDefinition in getControllerTypeDefinition(assemblyDefinition, api)
            from serviceFile in getServiceFile(config, api, controllerTypeDefinition)


            from modelFile in getModelFile(config, assemblyDefinition, api, controllerTypeDefinition)

            from file in new[] { modelFile, serviceFile}
            select file;

        
        return from file in files  select FileSystem.Save(file);


        static Task<Result<FileModel>> getModelFile(Config config, AssemblyDefinition assemblyDefinition, ApiInfo apiInfo, TypeDefinition controllerTypeDefinition)
        {
            return 
            from modelTypeDefinition in getModelTypeDefinition(assemblyDefinition, apiInfo)
                from modelFilePath in getModelOutputTsFilePath(config, modelTypeDefinition)


                let modelTsType = TsModelCreator.CreateFrom(config.ExternalTypes, modelTypeDefinition)
                let modelFile = new FileModel
                {
                    Path    = modelFilePath,
                    Content = TsOutput.LinesToString(TsOutput.GetTsCode(modelTsType))
                }
                from syncedFile in trySyncTsTypeWithLocalFileSystem(modelFile)
                select syncedFile;
        }

        static TypeReference getReturnType(MethodDefinition methodDefinition)
        {
            if (methodDefinition.ReturnType is GenericInstanceType genericInstanceType)
            {
                return genericInstanceType.GenericArguments[0];
            }

            return methodDefinition.ReturnType;
        }
        
        static Result<FileModel> getServiceFile(Config config, ApiInfo apiInfo, TypeDefinition controllerTypeDefinition)
        {

            return 
            from filePath in getOutputTsFilePath(config, apiInfo)
                select new FileModel
                {
                    Path    = filePath,
                    Content = string.Join(Environment.NewLine, getFileContent())
                };
            
            
            
            IReadOnlyList<string> getFileContent()
            {
                LineCollection lines =
                [
                    "import { BaseClientRequest, BaseClientResponse, useExecuter } from \"b-digital-framework\";",
                    "import {",
                    
                ];

              
                lines.AddRange(from methodDefinition in getExportablePublicMethods(controllerTypeDefinition)
                               from typeName in new[] { 
                                   methodDefinition.Parameters[0].ParameterType.Name, 
                                   getReturnType(methodDefinition).Name }
                               select typeName);
                
                lines.Add("} from \"../types\";");
                
                
                        lines.Add("export const useOsymService = () => {");
                        lines.Add("const basePath = \"/payments/osym\";");
                    
                foreach (var methodDefinition in getExportablePublicMethods(controllerTypeDefinition))
                {
                   lines.Add("    const startPreData = useExecuter<BaseClientRequest, OsymStartPreDataClientResponse>(basePath + \"/OsymStartPreData\", \"POST\");");
                }
                
                lines.Add("return {");
                lines.Add("startPreData, startPostData, getSessionList, execute, confirmPreData");
                lines.Add("};");
                lines.Add("}");
                
                return lines;
            }
            
            static Result<string> getOutputTsFilePath(Config config, ApiInfo apiInfo)
            {
                return 
                    from webProjectPath in getWebProjectFolderPath(config.ProjectDirectory)
                    select Path.Combine(webProjectPath,  "ClientApp","services",  $"use{apiInfo.Name}Service.ts");
            }
        }

      
        
        
     
        
        
        static IEnumerable<MethodDefinition> getExportablePublicMethods(TypeDefinition controllerTypeDefinition)
        {
            return from method in controllerTypeDefinition.Methods where method.IsPublic && method.Parameters.Count==1 && method.ReturnType.Name != "Void" select method;
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