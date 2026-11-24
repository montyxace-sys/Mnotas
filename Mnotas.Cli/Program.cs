using System.Reflection;
using System.Text;
using Mnotas.Core;

namespace Mnotas.Cli;

/// <summary>Utilidades de Mnotas para terminal en cualquier plataforma.</summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Help();
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "help" or "--help" or "-h" => Help(),
                "version" or "--version" => Version(),
                "stats" => Stats(args),
                "duplicate" => Duplicate(args),
                "case" => ChangeCase(args),
                "find" => Find(args),
                "replace" => Replace(args),
                _ => Unknown(args[0])
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or FormatException)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int Help()
    {
        Console.WriteLine("Mnotas 1.1.0 — utilidades de texto (mismo núcleo que la app portable)");
        Console.WriteLine();
        Console.WriteLine("  mnotas version");
        Console.WriteLine("  mnotas stats <archivo>");
        Console.WriteLine("  mnotas duplicate <archivo> <posición> [--out ARCHIVO]");
        Console.WriteLine("      Duplica la línea de la posición; sin --out imprime el resultado.");
        Console.WriteLine("  mnotas case <upper|lower|title|sentence> <archivo> [--out ARCHIVO]");
        Console.WriteLine("  mnotas find <archivo> <texto> [posición]");
        Console.WriteLine("  mnotas replace <archivo> <texto> <reemplazo> [--out ARCHIVO]");
        return 0;
    }

    private static int Version()
    {
        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0");
        return 0;
    }

    private static int Stats(string[] args)
    {
        if (args.Length < 2) return Usage("Uso: mnotas stats <archivo>");
        var stats = TextOperations.GetStatistics(Read(args[1]));
        Console.WriteLine($"Líneas: {stats.Lines}");
        Console.WriteLine($"Palabras: {stats.Words}");
        Console.WriteLine($"Caracteres: {stats.Characters}");
        Console.WriteLine($"Sin espacios: {stats.CharactersNoSpaces}");
        return 0;
    }

    private static int Duplicate(string[] args)
    {
        if (args.Length < 3) return Usage("Uso: mnotas duplicate <archivo> <posición> [--out ARCHIVO]");
        if (!int.TryParse(args[2], out var offset)) return Usage("La posición debe ser un número.");
        var result = TextOperations.DuplicateLine(Read(args[1]), offset);
        Write(args, result.Text);
        return 0;
    }

    private static int ChangeCase(string[] args)
    {
        if (args.Length < 3) return Usage("Uso: mnotas case <upper|lower|title|sentence> <archivo> [--out ARCHIVO]");
        var mode = args[1].ToLowerInvariant() switch
        {
            "upper" => CaseMode.Upper,
            "lower" => CaseMode.Lower,
            "title" => CaseMode.Title,
            "sentence" => CaseMode.Sentence,
            _ => (CaseMode?)null
        };
        if (mode is null) return Usage("Modo no válido. Usa upper, lower, title o sentence.");
        Write(args, TextOperations.ChangeCase(Read(args[2]), mode.Value));
        return 0;
    }

    private static int Find(string[] args)
    {
        if (args.Length < 3) return Usage("Uso: mnotas find <archivo> <texto> [posición]");
        var from = args.Length > 3 && int.TryParse(args[3], out var parsed) ? parsed : 0;
        var index = TextOperations.FindNext(Read(args[1]), args[2], from);
        Console.WriteLine(index < 0 ? "No encontrado." : $"Posición: {index}");
        return index < 0 ? 1 : 0;
    }

    private static int Replace(string[] args)
    {
        if (args.Length < 4) return Usage("Uso: mnotas replace <archivo> <texto> <reemplazo> [--out ARCHIVO]");
        Write(args, TextOperations.ReplaceAll(Read(args[1]), args[2], args[3]));
        return 0;
    }

    private static string Read(string path) => File.ReadAllText(path, Encoding.UTF8);

    private static void Write(string[] args, string text)
    {
        var output = Option(args, "--out");
        if (string.IsNullOrEmpty(output))
        {
            Console.Write(text);
            if (!text.EndsWith('\n')) Console.WriteLine();
        }
        else
        {
            File.WriteAllText(output, text, Encoding.UTF8);
            Console.WriteLine($"Escrito: {output}");
        }
    }

    private static string? Option(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        }
        return null;
    }

    private static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Comando desconocido: {command}. Ejecuta «mnotas help».");
        return 2;
    }
}
