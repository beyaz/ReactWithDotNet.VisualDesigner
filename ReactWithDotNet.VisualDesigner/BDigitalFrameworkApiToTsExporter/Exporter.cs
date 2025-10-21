using Mono.Cecil;
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
                               .With(ControllerTypeDefinition,getControllerTypeDefinition)
                                
            from modelTypeDefinition in getModelTypeDefinition(scope)
            from controllerTypeDefinition in getControllerTypeDefinition(scope)
            from modelFile in getModelFile(scope)
            from serviceFile in getServiceFile(scope)
            from serviceModelIntegrationFiles in getServiceAndModelIntegrationFiles(scope).AsResult()
            from typeFiles in getTypeFiles(scope).AsResult()
            from file in new[] { modelFile, serviceFile }.Concat(typeFiles).Concat(serviceModelIntegrationFiles)
            select file;
    }

    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        const string projectDirectory = "D:\\work\\BOA.BusinessModules\\Dev\\BOA.InternetBanking.Payments";
        const string apiName = "Religious";

        return from file in CalculateFiles(projectDirectory, apiName) select FileSystem.Save(file);
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

        return
            from modelTypeDefinition in getModelTypeDefinition(scope)
            from modelFilePath in getOutputTsFilePath(modelTypeDefinition)
            let modelTsType = TsModelCreator.CreateFrom(externalTypes, modelTypeDefinition)
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

        return CecilHelper.GetType(assemblyDefinition, fullTypeName);
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


        return from methodGroup in GroupControllerMethods(getExportablePublicMethods(controllerTypeDefinition))
               from filePath in getOutputTsFilePath(methodGroup)
               select new FileModel
               {
                   Path    = filePath,
                   Content = string.Join(Environment.NewLine, getFileContent(methodGroup))
               };


        IReadOnlyList<string> getFileContent(MethodGroup methodGroup)
        {
            LineCollection lines =
            [
                "import { BaseClientRequest, BaseClientResponse, useStore } from \"b-digital-framework\";",
                "import {",
            ];

            var inputOutputTypes
                = from methodDefinition in methodGroup.ControllerMethods
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
            lines.Add($"import {{ use{ApiName[scope]}Service }} from \"../services/use{ApiName[scope]}Service\"");

            lines.Add(string.Empty);
            lines.Add($"import {{ {ApiName[scope]}Model }} from \"../models/{ApiName[scope]}Model\"");

            lines.Add(string.Empty);

            lines.Add($"export const use{methodGroup.FolderName} = () => {{");

            lines.Add(string.Empty);
            lines.Add(Tab + "const store = useStore();");
            lines.Add(string.Empty);
            lines.Add(Tab + $"const service = use{ApiName[scope]}Service();");

            foreach (var methodDefinition in methodGroup.ControllerMethods)
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
                    true  => Path.Combine(webProjectPath, "ClientApp", "views", apiName, $"use{methodGroup.FolderName}.ts"),
                    false => Path.Combine(webProjectPath, "ClientApp", "views", apiName, methodGroup.FolderName, $"use{methodGroup.FolderName}.ts")
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

            var basePath = getSolutionName(projectDirectory).RemoveFromStart("BOA.InternetBanking.").ToLower();

            lines.Add($"export const use{ApiName[scope]}Service = () => {{");

            lines.Add(string.Empty);
            lines.Add($"const basePath = \"/{basePath}/{ApiName[scope]}\";");

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

        return
            from methodDefinition in getExportablePublicMethods(controllerTypeDefinition)
            from file in getMethodRequestResponseTypesInFile(scope, methodDefinition)
            select file;

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
                    let tsRequest = TsModelCreator.CreateFrom(externalTypes, requestTypeDefinition)
                    let tsResponse = TsModelCreator.CreateFrom(externalTypes, returnTypeDefinition)
                    select new FileModel
                    {
                        Path    = getOutputFilePath(webProjectPath),
                        Content = TsOutput.LinesToString(TsOutput.GetTsCode(tsRequest, tsResponse))
                    };
            }

            return
                from webProjectPath in getWebProjectFolderPath(projectDirectory)
                let returnTypeDefinition = getReturnType(methodDefinition).Resolve()
                let tsResponse = TsModelCreator.CreateFrom(externalTypes, returnTypeDefinition)
                select new FileModel
                {
                    Path    = getOutputFilePath(webProjectPath),
                    Content = TsOutput.LinesToString(TsOutput.GetTsCode(tsResponse))
                };

            string getOutputFilePath(string webProjectPath)
            {
                return Path.Combine(webProjectPath, "ClientApp", "types", ApiName[scope], $"{methodDefinition.Name}.ts");
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

    static Result<AssemblyDefinition> ReadAPIAssembly(string projectDirectory)
    {
        var solutionName = getSolutionName(projectDirectory);

        var filePath = Path.Combine(projectDirectory, "API", $"{solutionName}.API", "bin", "debug", "net8.0", $"{solutionName}.API.dll");

        return CecilHelper.ReadAssemblyDefinition(filePath);
    }

    class MethodGroup
    {
        public string FolderName { get; set; }
        public IReadOnlyList<MethodDefinition> ControllerMethods { get; set; }
    }

    static IReadOnlyList<MethodGroup> GroupControllerMethods(IReadOnlyList<MethodDefinition> controllerMethods)
    {
        var returnList = new List<MethodGroup>();
        
        var methodDefinitions = controllerMethods.ToList();

        (string FolderName, Func<MethodDefinition, bool> IsMethodMatchFolderFunc)[] splitters =
        [
            ("Confirm", IsInConfirmMethod),
            ("Start5", x => IsStartMethod(x, "5")),
            ("Start4", x => IsStartMethod(x, "4")),
            ("Start3", x => IsStartMethod(x, "3")),
            ("Start2", x => IsStartMethod(x, "2")),
            ("Start1", x => IsStartMethod(x, "1")),
            ("Start", x => IsStartMethod(x, ""))
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

                methodDefinitions.RemoveAll(x=>matchFunc(x));
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
        
        return  returnList;

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
            
            if (methodName.Contains($"Start{startNumber}PreData")||
                methodName.Contains($"Start{startNumber}PostData"))
            {
                return true;
            }
            
            return false;
        }

    }
}