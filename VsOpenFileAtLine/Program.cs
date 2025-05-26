using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;

namespace VsOpenFileAtLine;

class Program
{
    static DTE2 GetActiveVisualStudioInstance()
    {
        DTE2 dte = null;

        // Aşağıdaki örnek Visual Studio 2022 içindir, ihtiyaç halinde versiyonu değiştir
        var progId = "VisualStudio.DTE.17.0";

        try
        {
            var runningObject = Marshal.GetActiveObject(progId);
            dte = runningObject as DTE2;
        }
        catch (COMException)
        {
            // Visual Studio bulunamadı
        }

        return dte;
    }

    static void Main(string[] args)
    {
        
        File.WriteAllText(@"C:\Users\beyaz\OneDrive\Documents\a.txt",args.Length.ToString());
        
        //args = new string[]
        //{
        //    "C:\\github\\hopgogo\\web\\enduser-ui\\src\\components\\PackageListPage\\PackageDetailView.tsx", "153"
        //};
        if (args.Length < 2)
        {
            Console.WriteLine("Kullanım: VsOpenFileAtLine.exe <DosyaYolu> <SatirNumarasi>");
            return;
        }

        var filePath = args[0];
        if (!int.TryParse(args[1], out var lineNumber))
        {
            Console.WriteLine("Geçersiz satır numarası.");
            return;
        }

        try
        {
            var dte = GetActiveVisualStudioInstance();

            if (dte == null)
            {
                Console.WriteLine("Açık Visual Studio örneği bulunamadı.");
                return;
            }

            dte.MainWindow.Activate();
            dte.ItemOperations.OpenFile(filePath);

            var sel = (TextSelection)dte.ActiveDocument.Selection;
            sel.GotoLine(lineNumber, true);

            Console.WriteLine($"Dosya açıldı: {filePath} satır: {lineNumber}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
        }
    }
}