namespace ReactWithDotNet.VisualDesigner.CSharpTypeExportingToTypeScript;

/*
 * ExportTypeScriptFromCSharp
    variables:
      - ApiDll: 'c:\dd.c.dll'
      - out: d:\boa\tools\
      - ns: BOA.Common.X.Api
     
   
    types:
      - type: {ns}.Models.XModel1
        output: {out}{TypeName}.ts
   
      - type: {ns}.Types.User*
        output: {out}{TypeName}.ts
   	 
      - type: {ns}.Types.User
        output: {out}{TypeName}.ts
   	 includeOnlyProperties:[ 'abc', 'yx']
   	 
      - type: {ns}.Types.User
        output: {out}{TypeName}.ts
   	 excludeProperties:[ 'abc', 'yx']
 */
class CSharpTypeExporterToTypeScript
{
    
}