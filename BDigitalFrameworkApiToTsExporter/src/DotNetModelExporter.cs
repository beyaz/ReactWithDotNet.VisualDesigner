using Mono.Cecil;
using System.Linq;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    const string Tab = "    ";

    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        var files =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from api in config.ApiList
            let scopeApi = new ScopeApi
            {
                AssemblyDefinition = assemblyDefinition, 
                ApiInfo = api
            }
            from modelTypeDefinition in getModelTypeDefinition(scopeApi)
            from controllerTypeDefinition in getControllerTypeDefinition(scopeApi)
            from modelFile in getModelFile(config, assemblyDefinition, api, controllerTypeDefinition)
            from serviceFile in getServiceFile(config, api, controllerTypeDefinition)
            from serviceModelIntegrationFile in getServiceAndModelIntegrationFile(config, api, controllerTypeDefinition, modelTypeDefinition)
            from file in new[] { modelFile, serviceFile, serviceModelIntegrationFile }
            select file;

        return from file in files select FileSystem.Save(file);

        static Task<Result<FileModel>> getModelFile(Config config, AssemblyDefinition assemblyDefinition, ApiInfo apiInfo, TypeDefinition controllerTypeDefinition)
        {
            return
                from modelTypeDefinition in getModelTypeDefinition(assemblyDefinition, apiInfo)
                from modelFilePath in getOutputTsFilePath(config, modelTypeDefinition)
                let modelTsType = TsModelCreator.CreateFrom(config.ExternalTypes, modelTypeDefinition)
                select new FileModel
                {
                    Path    = modelFilePath,
                    Content = TsOutput.LinesToString(TsOutput.GetTsCode(modelTsType))
                };

            static Result<string> getOutputTsFilePath(Config config, TypeDefinition modelTypeDefinition)
            {
                return
                    from webProjectPath in getWebProjectFolderPath(config.ProjectDirectory)
                    select Path.Combine(webProjectPath, "ClientApp", "models", $"{modelTypeDefinition.Name}.ts");
            }
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

                var inputOutputTypes
                    = from methodDefinition in getExportablePublicMethods(controllerTypeDefinition)
                      from typeName in new[]
                      {
                          methodDefinition.Parameters[0].ParameterType.Name,
                          getReturnType(methodDefinition).Name
                      }
                      where typeName != "BaseClientRequest"
                      select Tab + typeName;

                lines.AddRange(inputOutputTypes.AppendBetween(","));

                lines.Add("} from \"../types\";");

                lines.Add(string.Empty);

                var basePath = getSolutionName(config.ProjectDirectory).RemoveFromStart("BOA.InternetBanking.").ToLower();

                lines.Add($"export const use{apiInfo.Name}Service = () => {{");

                lines.Add(string.Empty);
                lines.Add($"const basePath = \"/{basePath}/{apiInfo.Name}\";");

                foreach (var methodDefinition in getExportablePublicMethods(controllerTypeDefinition))
                {
                    lines.Add(string.Empty);
                    lines.Add(Tab + $"const {GetTsVariableName(methodDefinition.Name)} = useExecuter<{methodDefinition.Parameters[0].ParameterType.Name}, {getReturnType(methodDefinition).Name}>(basePath + \"/{methodDefinition.Name}\", \"POST\");");
                }

                lines.Add(string.Empty);
                lines.Add(Tab + "return {");

                var serviceNames = from m in getExportablePublicMethods(controllerTypeDefinition)
                                   select GetTsVariableName(m.Name);
                lines.AddRange
                    (
                     (from serviceName in serviceNames select Tab + Tab + serviceName).AppendBetween("," + Environment.NewLine)
                    );

                lines.Add(Tab + "};");
                lines.Add("}");

                return lines;
            }

            static Result<string> getOutputTsFilePath(Config config, ApiInfo apiInfo)
            {
                return
                    from webProjectPath in getWebProjectFolderPath(config.ProjectDirectory)
                    select Path.Combine(webProjectPath, "ClientApp", "services", $"use{apiInfo.Name}Service.ts");
            }
        }

        static IEnumerable<MethodDefinition> getExportablePublicMethods(TypeDefinition controllerTypeDefinition)
        {
            return from method in controllerTypeDefinition.Methods where method.IsPublic && method.Parameters.Count == 1 && method.ReturnType.Name != "Void" select method;
        }

        static Result<TypeDefinition> getModelTypeDefinition(ScopeApi scope)
        {
            // sample: BOA.InternetBanking.Payments.API -> BOA.InternetBanking.Payments.API.Models.GsmPrePaidModel

            var fullTypeName = $"{scope.AssemblyDefinition.Name.Name}.Models.{scope.ApiInfo.Name}Model";

            return CecilHelper.GetType(scope.AssemblyDefinition, fullTypeName);
        }

        static Result<TypeDefinition> getControllerTypeDefinition(ScopeApi scope)
        {
            // sample: BOA.InternetBanking.Payments.API -> BOA.InternetBanking.Payments.API.Controllers.GsmPrePaidController

            var fullTypeName = $"{scope.AssemblyDefinition.Name.Name}.Controllers.{scope.ApiInfo.Name}Controller";

            return CecilHelper.GetType(scope.AssemblyDefinition, fullTypeName);
        }

        static string getSolutionName(string projectDirectory)
        {
            return Path.GetFileName(projectDirectory);
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

        static Result<FileModel> getServiceAndModelIntegrationFile(Config config, ApiInfo apiInfo, TypeDefinition controllerTypeDefinition, TypeDefinition modelTypeDefinition)
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
                    "import { BaseClientRequest, BaseClientResponse, useStore } from \"b-digital-framework\";",
                    "import {",
                ];

                var inputOutputTypes
                    = from methodDefinition in getExportablePublicMethods(controllerTypeDefinition)
                      from typeName in new[]
                      {
                          methodDefinition.Parameters[0].ParameterType.Name,
                          getReturnType(methodDefinition).Name
                      }
                      where typeName != "BaseClientRequest"
                      select Tab + typeName;

                lines.AddRange(inputOutputTypes.AppendBetween(","));

                lines.Add("} from \"../types\";");

                lines.Add(string.Empty);
                lines.Add($"import {{ use{apiInfo.Name}Service }} from \"../services/use{apiInfo.Name}Service\"");

                lines.Add(string.Empty);
                lines.Add($"import {{ {apiInfo.Name}Model }} from \"../models/{apiInfo.Name}Model\"");

                lines.Add(string.Empty);
                var basePath = getSolutionName(config.ProjectDirectory).RemoveFromStart("BOA.InternetBanking.").ToLower();

                lines.Add($"export const use{apiInfo.Name} = () => {{");

                lines.Add(string.Empty);
                lines.Add(Tab + "const store = useStore();");
                lines.Add(string.Empty);
                lines.Add(Tab + $"const service = use{apiInfo.Name}Service();");

                foreach (var methodDefinition in getExportablePublicMethods(controllerTypeDefinition))
                {
                    lines.Add(string.Empty);

                    lines.Add(Tab + $"const {GetTsVariableName(methodDefinition.Name)} = async (model: {modelTypeDefinition.Name}) : Promise<boolean> => {{");

                    lines.Add(string.Empty);
                    lines.Add(Tab + Tab + "const abortController = new AbortController();");

                    lines.Add(string.Empty);
                    lines.Add(Tab + Tab + $"let request: {methodDefinition.Parameters[0].ParameterType.Name} = {{");

                    var mappingLines = from property in getMappingPropertyList(modelTypeDefinition, methodDefinition.Parameters[0].ParameterType.Resolve())
                                       let name = GetTsVariableName(property.Name)
                                       select Tab + Tab + Tab + $"{name}: model.{name}";

                    lines.AddRange(mappingLines.AppendBetween("," + Environment.NewLine));

                    lines.Add(Tab + Tab + "};");
                    lines.Add(string.Empty);

                    lines.Add(Tab + Tab + $"const response = await service.{GetTsVariableName(methodDefinition.Name)}.send(request, abortController.signal);");
                    lines.Add(Tab + Tab + "if(!response.success) {");
                    lines.Add(Tab + Tab + Tab + "store.setMessage({ content: response.result.errorMessage });");
                    lines.Add(Tab + Tab + Tab + "return false;");
                    lines.Add(Tab + Tab + "}");

                    lines.Add(string.Empty);

                    var responseMapping = from property in getMappingPropertyList(modelTypeDefinition, methodDefinition.ReturnType.Resolve())
                                          let name = GetTsVariableName(property.Name)
                                          select Tab + Tab + $"model.{name}: response.{name}";

                    lines.AddRange(responseMapping.AppendBetween("," + Environment.NewLine));

                    lines.Add(string.Empty);

                    lines.Add(Tab + Tab + "return true");

                    lines.Add(Tab + "};");
                }

                lines.Add(string.Empty);
                lines.Add(Tab + "return {");

                var serviceNames = from m in getExportablePublicMethods(controllerTypeDefinition)
                                   select GetTsVariableName(m.Name);
                lines.AddRange
                    (
                     (from serviceName in serviceNames select Tab + Tab + serviceName).AppendBetween("," + Environment.NewLine)
                    );

                lines.Add(Tab + "};");
                lines.Add("}");

                return lines;
            }

            static Result<string> getOutputTsFilePath(Config config, ApiInfo apiInfo)
            {
                return
                    from webProjectPath in getWebProjectFolderPath(config.ProjectDirectory)
                    select Path.Combine(webProjectPath, "ClientApp", "services", $"use{apiInfo.Name}.ts");
            }

            static IEnumerable<PropertyDefinition> getMappingPropertyList(TypeDefinition model, TypeDefinition apiParameter)
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
        
        static IEnumerable<Result<FileModel>> getTypeFiles(Config config, ApiInfo apiInfo, TypeDefinition controllerTypeDefinition)
        {
            return 
            from methodDefinition in getExportablePublicMethods(controllerTypeDefinition)
                from files in getTypeFilesRelatedMethod(config, apiInfo, methodDefinition)
                from file in files
                select file;
        }

        
        static Result<FileModel[]> getTypeFilesRelatedMethod(Config config, ApiInfo apiInfo, MethodDefinition methodDefinition)
            {
                
                var returnTypeFile = getTypeFileRelatedMethod(config,apiInfo , getReturnType(methodDefinition));
                
                
                if (methodDefinition.Parameters[0].ParameterType.Name !="BaseClientRequest")
                {
                    var inputResponse = getTypeFileRelatedMethod(config,apiInfo , methodDefinition.Parameters[0].ParameterType);

                    return from input in inputResponse
                                             from output in returnTypeFile
                                             select new[] { input, output };
                }
                
                return from output in returnTypeFile
                       select new[] {  output };
                
                static Result<FileModel> getTypeFileRelatedMethod(Config config, ApiInfo apiInfo, TypeReference typeReference)
                {
                    return
                        from filePath in getOutputTsFilePath(config, apiInfo,typeReference)
                    
                        let tsType = TsModelCreator.CreateFrom(config.ExternalTypes, typeReference.Resolve())
                        select new FileModel
                        {
                            Path    = filePath,
                            Content = TsOutput.LinesToString(TsOutput.GetTsCode(tsType))
                        };


                    static Result<string> getOutputTsFilePath(Config config, ApiInfo apiInfo, TypeReference typeReference)
                    {
                        return
                            from webProjectPath in getWebProjectFolderPath(config.ProjectDirectory)
                            select Path.Combine(webProjectPath, "ClientApp", "types", apiInfo.Name, $"{typeReference.Name}.ts");
                    }
                }
            }
        
       
    }


}