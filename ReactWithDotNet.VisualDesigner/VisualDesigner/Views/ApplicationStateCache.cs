//using System.IO;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace ReactWithDotNet.VisualDesigner.Views;

//static class ApplicationStateCache
//{
//    public static readonly string CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ReactVisualDesigner") +
//                                            Path.DirectorySeparatorChar;

//    static readonly object fileLock = new();

//    static string StateFilePath => Path.Combine(CacheDirectory, $"{nameof(ApplicationState)}.json");

//    public static ApplicationState ReadState()
//    {
//        if (File.Exists(StateFilePath))
//        {
//            var json = File.ReadAllText(StateFilePath);

//            try
//            {
//                return JsonSerializer.Deserialize<ApplicationState>(json);
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }

//        return null;
//    }

//    public static Task Save(ApplicationState state)
//    {
//        lock (fileLock)
//        {
//            var jsonContent = JsonSerializer.Serialize(state, new JsonSerializerOptions
//            {
//                WriteIndented          = true,
//                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//            });

//            return WriteAllText(StateFilePath, jsonContent);
//        }
//    }

//    static Task WriteAllText(string path, string contents)
//    {
//        var directoryName = Path.GetDirectoryName(path);

//        if (!string.IsNullOrWhiteSpace(directoryName))
//        {
//            if (!Directory.Exists(directoryName))
//            {
//                Directory.CreateDirectory(directoryName);
//            }
//        }

//        return File.WriteAllTextAsync(path, contents, Encoding.UTF8);
//    }
//}