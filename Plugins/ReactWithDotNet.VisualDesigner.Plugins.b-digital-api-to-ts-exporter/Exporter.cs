using Mono.Cecil;
using System.Collections.Immutable;
using System.IO;

namespace BDigitalFrameworkApiToTsExporter;

static class Exporter
{
    const string Tab = "    ";

    public static IAsyncEnumerable<Result<FileModel>> CalculateFiles(string projectDirectory, string apiName)
    {
        return
            from scope in Scope.Create(new()
                                {
                                    { ProjectDirectory, projectDirectory },
                                    { ExternalTypes, ExternalTypeList.Value },
                                    { ApiName, apiName }
                                })
                               .With(Assembly, ReadAPIAssembly(projectDirectory))
                               .With(ModelTypeDefinition, getModelTypeDefinition)
                               .With(ControllerTypeDefinition, getControllerTypeDefinition)
            from modelTypeDefinition in getModelTypeDefinition(scope)
            from controllerTypeDefinition in getControllerTypeDefinition(scope)
            from modelFile in getModelFile(scope)
            from serviceFile in getServiceFile(scope)
            from serviceModelIntegrationFiles in getServiceAndModelIntegrationFiles(scope).AsResult()
            from typeFiles in getTypeFiles(scope).AsResult()
            from extraFilesInModel in ExportExtraTypes(scope, modelTypeDefinition).AsResult()
            from file in new List<FileModel>
            {
                modelFile,
                serviceFile,
                typeFiles,
                serviceModelIntegrationFiles,
                extraFilesInModel
            }
            where file is not null
            select file;
    }

    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        const string projectDirectory = "D:\\work\\BOA.BusinessModules\\Dev\\BOA.InternetBanking.Payments";
        const string apiName = "Religious";

        return from file in CalculateFiles(projectDirectory, apiName) select FileSystem.Save(file);
    }

    static IEnumerable<Result<FileModel>> ExportExtraTypes(Scope scope, TypeDefinition containerTypeDefinition)
    {
        if (containerTypeDefinition is null)
        {
            return [];

        }
        return from type in GetExtraTypes(ExternalTypes[scope], containerTypeDefinition)
               select ExportExtraType(scope, type);

        static IReadOnlyList<TypeDefinition> GetExtraTypes(IReadOnlyList<ExternalTypeInfo> externalTypes, TypeDefinition modelTypeDefinition)
        {
            return new List<TypeDefinition>
            {
                from propertyDefinition in modelTypeDefinition.Properties
                let propertyType = propertyDefinition.PropertyType
                from extraType in  CollectExtraTypes(externalTypes, propertyType,[])
                select extraType
            };

            static IReadOnlyList<TypeDefinition> CollectExtraTypes(IReadOnlyList<ExternalTypeInfo> externalTypes, TypeReference typeReference, IReadOnlyList<TypeDefinition> collectedTypeDefinitions)
            {
                if (CecilHelper.IsNullableType(typeReference) || typeReference.IsCollectionType)
                {
                    return CollectExtraTypes(externalTypes, ((GenericInstanceType)typeReference).GenericArguments[0], collectedTypeDefinitions);
                }
                
                foreach (var externalType in externalTypes)
                {
                    if (externalType.DotNetFullTypeName == typeReference.FullName)
                    {
                        return [];
                    }
                }
                
                if (typeReference.IsString || typeReference.IsNumber || typeReference.IsBoolean || typeReference.IsDateTime || typeReference.IsObject)
                {
                    return [];
                }

                
                
                TypeDefinition typeDefinition;

                try
                {
                    typeDefinition = typeReference.Resolve();
                }
                catch (Exception)
                {
                    return [];
                }

                var needToExport = typeDefinition.IsEnum || typeDefinition.BaseType?.FullName == "System.Object";

                if (needToExport)
                {
                    if (collectedTypeDefinitions.All(x => x.FullName != typeDefinition.FullName))
                    {
                                
                        return new List<TypeDefinition>
                        {
                            collectedTypeDefinitions,
                            typeDefinition,
                            
                            from propertyDefinition in typeDefinition.Properties
                            let propertyType = propertyDefinition.PropertyType
                            from extraType in  CollectExtraTypes(externalTypes, propertyType,[])
                            select extraType
                        };
                    }
                    
                }
                
                return collectedTypeDefinitions;
            }

            
        }

        static Result<FileModel> ExportExtraType(Scope scope, TypeDefinition extraTypeDefinition)
        {
            var projectDirectory = ProjectDirectory[scope];

            var externalTypes = ExternalTypes[scope];

            return
                from outputFilePath in getOutputTsFilePath()
                let tsType = TsModelCreator.CreateFrom(externalTypes, extraTypeDefinition, ApiName[scope])
                select new FileModel
                {
                    Path    = outputFilePath,
                    Content = TsOutput.LinesToString(TsOutput.GetTsCode(tsType))
                };

            Result<string> getOutputTsFilePath()
            {
                return
                    from webProjectPath in getWebProjectFolderPath(projectDirectory)
                    let fileName = TsModelCreator.GetExtraClassFileName(extraTypeDefinition, ApiName[scope])
                    select Path.Combine(webProjectPath, "ClientApp", "types", ApiName[scope], $"{fileName}.ts");
            }
        }
    }

    static Result<TypeDefinition> getControllerTypeDefinition(Scope scope)
    {
        var assemblyDefinition = Assembly[scope];

        // sample: BOA.InternetBanking.Payments.API -> BOA.InternetBanking.Payments.API.Controllers.GsmPrePaidController

        var fullTypeName = $"{assemblyDefinition.Name.Name}.Controllers.{ApiName[scope]}Controller";

        return CecilHelper.GetType(assemblyDefinition, fullTypeName);
    }

    static IReadOnlyList<MethodDefinition> getExportablePublicMethods(TypeDefinition controllerTypeDefinition)
    {
        return (
            from method in controllerTypeDefinition.Methods
            where method.IsPublic && method.Parameters.Count == 1 &&
                  method.ReturnType.Name != "Void"
            select method
        ).ToList();
    }

    static Task<Result<FileModel>> getModelFile(Scope scope)
    {
        var projectDirectory = ProjectDirectory[scope];

        var externalTypes = ExternalTypes[scope];

        var modelTypeDefinitionResult = getModelTypeDefinition(scope);
        if (modelTypeDefinitionResult.Value is null)
        {
            return Result.From<FileModel>(null);
        }
        

        return
            from modelTypeDefinition in getModelTypeDefinition(scope)
            from modelFilePath in getOutputTsFilePath(modelTypeDefinition)
            let modelTsType = TsModelCreator.CreateFrom(externalTypes, modelTypeDefinition, ApiName[scope])
            select new FileModel
            {
                Path    = modelFilePath,
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(modelTsType))
            };

        Result<string> getOutputTsFilePath(TypeDefinition modelTypeDefinition)
        {
            return
                from webProjectPath in getWebProjectFolderPath(projectDirectory)
                select Path.Combine(webProjectPath, "ClientApp", "models", $"{modelTypeDefinition.Name}.ts");
        }
    }

    static Result<TypeDefinition> getModelTypeDefinition(Scope scope)
    {
        var assemblyDefinition = Assembly[scope];

        // sample: BOA.InternetBanking.Payments.API -> BOA.InternetBanking.Payments.API.Models.GsmPrePaidModel

        var fullTypeName = $"{assemblyDefinition.Name.Name}.Models.{ApiName[scope]}Model";

        return CecilHelper.GetType(assemblyDefinition, fullTypeName).Value;
    }

    static TypeReference getReturnType(MethodDefinition methodDefinition)
    {
        if (methodDefinition.ReturnType is GenericInstanceType genericInstanceType)
        {
            return genericInstanceType.GenericArguments[0];
        }

        return methodDefinition.ReturnType;
    }

    static IEnumerable<Result<FileModel>> getServiceAndModelIntegrationFiles(Scope scope)
    {
        var projectDirectory = ProjectDirectory[scope];
        var controllerTypeDefinition = ControllerTypeDefinition[scope];
        var modelTypeDefinition = ModelTypeDefinition[scope];
        var apiName = ApiName[scope];

        if (modelTypeDefinition is null)
        {
            return [];
        }

        return from methodGroup in GroupControllerMethods(getExportablePublicMethods(controllerTypeDefinition))
               from filePath in getOutputTsFilePath(methodGroup)
               select new FileModel
               {
                   Path    = filePath,
                   Content = string.Join(Environment.NewLine, getFileContent(methodGroup))
               };

        IReadOnlyList<string> getFileContent(MethodGroup methodGroup)
        {
            var directoryPath = (methodGroup.FolderName == "Shared") switch
            {
                true  => "../../",
                false => "../../../"
            };

            LineCollection lines =
            [
                "import { useStore } from \"b-digital-framework\";",
                string.Empty,
                $"import {{ use{ApiName[scope]}Service }} from \"{directoryPath}services/use{ApiName[scope]}Service\";",
                string.Empty,
                $"import {{ {ApiName[scope]}Model }} from \"{directoryPath}models/{ApiName[scope]}Model\";",
                string.Empty,
                $"export const use{methodGroup.FolderName} = () => {{",
                string.Empty,
                Tab + "const store = useStore();",
                string.Empty,
                Tab + $"const service = use{ApiName[scope]}Service();"
            ];

            foreach (var methodDefinition in methodGroup.ControllerMethods)
            {
                lines.Add(string.Empty);

                lines.Add(Tab + $"const {GetTsVariableName(methodDefinition.Name)} = async (model: {modelTypeDefinition.Name}) : Promise<boolean> => {{");

                lines.Add(string.Empty);
                lines.Add(Tab + Tab + "const abortController = new AbortController();");

                // define request
                {
                    lines.Add(string.Empty);

                    var mappingLines = ListFrom
                    (from property in getMappingPropertyList(modelTypeDefinition, methodDefinition.Parameters[0].ParameterType.Resolve())
                     let name = GetTsVariableName(property.Name)
                     select Tab + Tab + Tab + $"{name}: model.{name}"
                    );

                    if (mappingLines.Count > 0)
                    {
                        lines.Add(Tab + Tab + "const request = {");

                        lines.AddRange(mappingLines.AppendBetween("," + Environment.NewLine));

                        lines.Add(Tab + Tab + "};");
                    }
                    else
                    {
                        lines.Add(Tab + Tab + "const request = { };");
                    }
                }

                lines.Add(string.Empty);

                lines.Add(Tab + Tab + $"const response = await service.{GetTsVariableName(methodDefinition.Name)}.send(request, abortController.signal);");
                lines.Add(Tab + Tab + "if(!response.success) {");
                lines.Add(Tab + Tab + Tab + "store.setMessage({ content: response.result.errorMessage });");
                lines.Add(Tab + Tab + Tab + "return false;");
                lines.Add(Tab + Tab + "}");

                lines.Add(string.Empty);

                var responseMapping = from property in getMappingPropertyList(modelTypeDefinition, getReturnType(methodDefinition).Resolve())
                                      let name = GetTsVariableName(property.Name)
                                      select Tab + Tab + $"model.{name} = response.{name};";

                lines.AddRange(responseMapping.AppendBetween(Environment.NewLine));

                lines.Add(string.Empty);

                lines.Add(Tab + Tab + "return true");

                lines.Add(Tab + "};");
            }

            lines.Add(string.Empty);
            lines.Add(Tab + "return {");

            var serviceNames = from m in methodGroup.ControllerMethods
                               select GetTsVariableName(m.Name);
            lines.AddRange
            (
                (from serviceName in serviceNames select Tab + Tab + serviceName).AppendBetween("," + Environment.NewLine)
            );

            lines.Add(Tab + "};");
            lines.Add("}");

            return lines;
        }

        Result<string> getOutputTsFilePath(MethodGroup methodGroup)
        {
            return
                from webProjectPath in getWebProjectFolderPath(projectDirectory)
                select (methodGroup.FolderName == "Shared") switch
                {
                    true  => Path.Combine(webProjectPath, "ClientApp", "views", apiName.ToLowerFirstCharInvariant(), $"use{methodGroup.FolderName}.ts"),
                    false => Path.Combine(webProjectPath, "ClientApp", "views", apiName.ToLowerFirstCharInvariant(), methodGroup.FolderName.ToLowerFirstCharInvariant(), $"use{methodGroup.FolderName}.ts")
                };
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

    static Result<FileModel> getServiceFile(Scope scope)
    {
        var projectDirectory = ProjectDirectory[scope];

        var controllerTypeDefinition = ControllerTypeDefinition[scope];

        return
            from filePath in getOutputTsFilePath()
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

            var methods = (from methodGroup in GroupControllerMethods(getExportablePublicMethods(controllerTypeDefinition))
                           from method in methodGroup.ControllerMethods
                           select method).ToImmutableList();

            var inputOutputTypes
                = from methodDefinition in methods
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

            var basePath = getSolutionName(projectDirectory).RemoveFromStart("BOA.InternetBanking.").ToLower();

            lines.Add($"export const use{ApiName[scope]}Service = () => {{");

            lines.Add(string.Empty);
            lines.Add($"const basePath = \"/{basePath}/{ApiName[scope]}\";");

            foreach (var methodDefinition in methods)
            {
                lines.Add(string.Empty);
                lines.Add(Tab + $"const {GetTsVariableName(methodDefinition.Name)} = useExecuter<{methodDefinition.Parameters[0].ParameterType.Name}, {getReturnType(methodDefinition).Name}>(basePath + \"/{methodDefinition.Name}\", \"POST\");");
            }

            lines.Add(string.Empty);
            lines.Add(Tab + "return {");

            var serviceNames = from m in methods
                               select GetTsVariableName(m.Name);
            lines.AddRange
            (
                (from serviceName in serviceNames select Tab + Tab + serviceName).AppendBetween("," + Environment.NewLine)
            );

            lines.Add(Tab + "};");
            lines.Add("}");

            return lines;
        }

        Result<string> getOutputTsFilePath()
        {
            return
                from webProjectPath in getWebProjectFolderPath(projectDirectory)
                select Path.Combine(webProjectPath, "ClientApp", "services", $"use{ApiName[scope]}Service.ts");
        }
    }

    static string getSolutionName(string projectDirectory)
    {
        return Path.GetFileName(projectDirectory);
    }

    static IEnumerable<Result<FileModel>> getTypeFiles(Scope scope)
    {
        var controllerTypeDefinition = ControllerTypeDefinition[scope];

        return new List<Result<FileModel>>
        {
            // r e q u e s t  -  r e s p o n s e   t y p e s
            from methodDefinition in getExportablePublicMethods(controllerTypeDefinition)
            from file in getMethodRequestResponseTypesInFile(scope, methodDefinition)
            select file,

            // e x t r a   t y p e s
            from methodDefinition in getExportablePublicMethods(controllerTypeDefinition)
            from extraTypes in ExportExtraTypes(scope, getReturnType(methodDefinition).Resolve()).AsResult()
            from x in extraTypes
            select x
        };

        static Result<FileModel> getMethodRequestResponseTypesInFile(Scope scope, MethodDefinition methodDefinition)
        {
            var projectDirectory = ProjectDirectory[scope];
            var externalTypes = ExternalTypes[scope];

            if (methodDefinition.Parameters[0].ParameterType.Name != "BaseClientRequest")
            {
                return
                    from webProjectPath in getWebProjectFolderPath(projectDirectory)
                    let returnTypeDefinition = getReturnType(methodDefinition).Resolve()
                    let requestTypeDefinition = methodDefinition.Parameters[0].ParameterType.Resolve()
                    let tsRequest = TsModelCreator.CreateFrom(externalTypes, requestTypeDefinition, ApiName[scope])
                    let tsResponse = TsModelCreator.CreateFrom(externalTypes, returnTypeDefinition, ApiName[scope])
                    select new FileModel
                    {
                        Path    = getOutputFilePath(webProjectPath),
                        Content = TsOutput.LinesToString(TsOutput.GetTsCode(tsRequest, tsResponse))
                    };
            }

            return
                from webProjectPath in getWebProjectFolderPath(projectDirectory)
                let returnTypeDefinition = getReturnType(methodDefinition).Resolve()
                let tsResponse = TsModelCreator.CreateFrom(externalTypes, returnTypeDefinition, ApiName[scope])
                select new FileModel
                {
                    Path    = getOutputFilePath(webProjectPath),
                    Content = TsOutput.LinesToString(TsOutput.GetTsCode(tsResponse))
                };

            string getOutputFilePath(string webProjectPath)
            {
                return Path.Combine(webProjectPath, "ClientApp", "types", ApiName[scope].ToLowerFirstCharInvariant(), $"{methodDefinition.Name}.ts");
            }
        }
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

    static IReadOnlyList<MethodGroup> GroupControllerMethods(IReadOnlyList<MethodDefinition> controllerMethods)
    {
        var returnList = new List<MethodGroup>();

        var methodDefinitions = controllerMethods.ToList();

        (string FolderName, Func<MethodDefinition, bool> IsMethodMatchFolderFunc)[] splitters =
        [
            ("Start", x => IsStartMethod(x, "")),
            ("Start1", x => IsStartMethod(x, "1")),
            ("Start2", x => IsStartMethod(x, "2")),
            ("Start3", x => IsStartMethod(x, "3")),
            ("Start4", x => IsStartMethod(x, "4")),
            ("Start5", x => IsStartMethod(x, "5")),
            ("Confirm", IsInConfirmMethod),
        ];

        foreach (var (folderName, matchFunc) in splitters)
        {
            if (methodDefinitions.Count(matchFunc) > 0)
            {
                returnList.Add(new MethodGroup
                {
                    FolderName = folderName,

                    ControllerMethods = methodDefinitions.Where(matchFunc).ToList()
                });

                methodDefinitions.RemoveAll(x => matchFunc(x));
            }
        }

        if (methodDefinitions.Count > 0)
        {
            returnList.Add(new MethodGroup
            {
                FolderName = "Shared",

                ControllerMethods = methodDefinitions
            });
        }

        foreach (var methodGroup in returnList)
        {
            methodGroup.ControllerMethods =
            (
                from md in methodGroup.ControllerMethods
                orderby md.Name.Contains("pre", StringComparison.OrdinalIgnoreCase) descending
                select md
            ).ToList();
        }

        return returnList;

        static bool IsInConfirmMethod(MethodDefinition methodDefinition)
        {
            var methodName = methodDefinition.Name;

            if (methodName.Contains("Execute") ||
                methodName.Contains("ConfirmPreData"))
            {
                return true;
            }

            return false;
        }

        static bool IsStartMethod(MethodDefinition methodDefinition, string startNumber)
        {
            var methodName = methodDefinition.Name;

            if (methodName.Contains($"Start{startNumber}PreData") ||
                methodName.Contains($"Start{startNumber}PostData"))
            {
                return true;
            }

            return false;
        }
    }

    static Result<AssemblyDefinition> ReadAPIAssembly(string projectDirectory)
    {
        var solutionName = getSolutionName(projectDirectory);

        var filePath = Path.Combine(projectDirectory, "API", $"{solutionName}.API", "bin", "debug", "net8.0", $"{solutionName}.API.dll");

        const string secondarySearchDirectoryPath = @"d:\boa\server\bin";

        return CecilAssemblyReader.ReadAssembly(filePath, secondarySearchDirectoryPath);
    }

    class MethodGroup
    {
        public IReadOnlyList<MethodDefinition> ControllerMethods { get; set; }
        public string FolderName { get; set; }
    }
}