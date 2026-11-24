using Mnotas.Core;

namespace Mnotas.CoreTests;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Check("Duplicar línea en el medio", () =>
        {
            var result = TextOperations.DuplicateLine("uno\ndos\ntres", 5);
            Expect(result.Text == "uno\ndos\ndos\ntres", $"Texto inesperado: {result.Text}");
            Expect(result.Text.Substring(result.CaretOffset).StartsWith("dos"), "El caret no está en la copia.");
        });

        Check("Duplicar última línea sin salto", () =>
        {
            var result = TextOperations.DuplicateLine("uno\ndos", 6);
            Expect(result.Text.StartsWith("uno\ndos"), "Se perdió el original.");
            Expect(result.Text.Split('\n').Length == 3, "No se añadió una tercera línea.");
        });

        Check("Duplicar primera línea", () =>
        {
            var result = TextOperations.DuplicateLine("uno\ndos", 0);
            Expect(result.Text == "uno\nuno\ndos", $"Texto inesperado: {result.Text}");
        });

        Check("Duplicar con CRLF", () =>
        {
            var result = TextOperations.DuplicateLine("uno\r\ndos", 1);
            Expect(result.Text == "uno\r\nuno\r\ndos", $"Texto inesperado: {result.Text.Replace("\r", "\\r")}");
        });

        Check("Duplicar documento vacío", () =>
        {
            var result = TextOperations.DuplicateLine("", 0);
            Expect(result.Text == Environment.NewLine, "El documento vacío no se duplicó.");
        });

        Check("Caret fuera de rango se ajusta", () =>
        {
            var result = TextOperations.DuplicateLine("abc", 999);
            Expect(result.Text == "abc" + Environment.NewLine + "abc", "Caret fuera de rango mal gestionado.");
        });

        Check("Estadísticas de texto", () =>
        {
            var stats = TextOperations.GetStatistics("uno dos\ntres  cuatro\n");
            Expect(stats.Lines == 2, $"Líneas: {stats.Lines}");
            Expect(stats.Words == 4, $"Palabras: {stats.Words}");
            Expect(stats.Characters == 21, $"Caracteres: {stats.Characters}");
            Expect(stats.CharactersNoSpaces == 16, $"Sin espacios: {stats.CharactersNoSpaces}");
        });

        Check("Buscar siguiente coincidencia y envolver", () =>
        {
            Expect(TextOperations.FindNext("uno dos uno", "uno", 4) == 8, "No encontró la segunda coincidencia.");
            Expect(TextOperations.FindNext("uno dos uno", "uno", 10) == 0, "No envolvió al principio.");
            Expect(TextOperations.FindNext("abc", "", 0) == -1, "Una búsqueda vacía debe devolver -1.");
        });

        Check("Reemplazar todo sin distinción de mayúsculas", () =>
        {
            var result = TextOperations.ReplaceAll("Uno uno UNO", "uno", "dos");
            Expect(result == "dos dos dos", $"Resultado: {result}");
        });

        Check("Reemplazar todo respeta mayúsculas", () =>
        {
            var result = TextOperations.ReplaceAll("Uno uno UNO", "uno", "dos", false);
            Expect(result == "Uno dos UNO", $"Resultado: {result}");
        });

        Check("Cuatro modos de mayúsculas", () =>
        {
            Expect(TextOperations.ChangeCase("hola mundo", CaseMode.Upper) == "HOLA MUNDO", "MAYÚSCULAS");
            Expect(TextOperations.ChangeCase("HOLA MUNDO", CaseMode.Lower) == "hola mundo", "minúsculas");
            Expect(TextOperations.ChangeCase("hola mundo", CaseMode.Title) == "Hola Mundo", "iniciales");
            Expect(TextOperations.ChangeCase("hOLA MUNDO", CaseMode.Sentence) == "Hola mundo", "frase");
        });

        Console.WriteLine();
        Console.WriteLine($"{_passed} correctas, {_failed} fallidas.");
        return _failed == 0 ? 0 : 1;
    }

    private static void Check(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"OK   {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"FALLO {name}: {ex.Message}");
        }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
