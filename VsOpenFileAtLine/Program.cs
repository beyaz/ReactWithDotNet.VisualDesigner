using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;

namespace VsOpenFileAtLine;

class Program
{
    static DTE2 GetActiveVisualStudioInstance()
    {
        List<string> progIdList = ["VisualStudio.DTE.18.0", "VisualStudio.DTE.17.0"];

        return ExecUntilNotNull(progIdList, tryGetDteByProgId);

        static DTE2 tryGetDteByProgId(string progId)
        {
            try
            {
                var runningObject = Marshal.GetActiveObject(progId);
                return runningObject as DTE2;
            }
            catch (COMException)
            {
                // Visual Studio bulunamadı
            }

            return null;
        }

        static B ExecUntilNotNull<A, B>(IReadOnlyList<A> items, Func<A, B> func) where B : class
        {
            foreach (var item in items)
            {
                var b = func(item);
                if (b is not null)
                {
                    return b;
                }
            }

            return null;
        }
    }

    static void Main(string[] args)
    {
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