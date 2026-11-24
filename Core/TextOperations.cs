namespace Mnotas.Core;

/// <summary>Resultado de una edición: texto resultante y nueva posición del caret.</summary>
public readonly record struct TextEditResult(string Text, int CaretOffset);

/// <summary>Operaciones de texto compartidas por la edición WPF, el visor portable y la CLI.</summary>
public static class TextOperations
{
    /// <summary>
    /// Duplica la línea donde está el caret (o la primera línea seleccionada) y coloca
    /// el caret al principio de la copia. Equivale a Ctrl+D en editores habituales.
    /// </summary>
    public static TextEditResult DuplicateLine(string text, int caretOffset)
    {
        var value = text ?? string.Empty;
        var offset = Math.Clamp(caretOffset, 0, value.Length);

        var start = value.LastIndexOf('\n', Math.Max(0, offset - 1)) + 1;
        var newlineIndex = value.IndexOf('\n', offset);
        var end = newlineIndex < 0 ? value.Length : newlineIndex + 1;

        var contentEnd = newlineIndex < 0 ? value.Length : newlineIndex;
        if (contentEnd > start && value[contentEnd - 1] == '\r') contentEnd--;

        var line = value[start..contentEnd];
        var newline = newlineIndex < 0 ? Environment.NewLine : value[contentEnd..end];
        // Si la línea ya termina con salto, se copia después de él; si no, hay que añadirlo.
        var insertion = newlineIndex < 0 ? newline + line : line + newline;
        var caret = newlineIndex < 0 ? end + newline.Length : end;
        var edited = value.Insert(end, insertion);
        return new TextEditResult(edited, caret);
    }

    public static TextStatistics GetStatistics(string text)
    {
        var value = text ?? string.Empty;
        var lines = value.Length == 0 ? 1 : value.Split('\n').Length;
        if (value.EndsWith("\n", StringComparison.Ordinal)) lines--;
        var words = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return new TextStatistics(lines, words, value.Length, value.Count(c => !char.IsWhiteSpace(c)));
    }

    public static string ReplaceAll(string text, string find, string replacement, bool ignoreCase = true)
    {
        if (string.IsNullOrEmpty(find)) return text;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var result = text;
        var index = 0;
        while ((index = result.IndexOf(find, index, comparison)) >= 0)
        {
            result = result.Remove(index, find.Length).Insert(index, replacement);
            index += replacement.Length;
        }

        return result;
    }

    public static int FindNext(string text, string find, int fromOffset, bool ignoreCase = true)
    {
        if (string.IsNullOrEmpty(find)) return -1;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var start = Math.Clamp(fromOffset, 0, (text ?? string.Empty).Length);
        var index = (text ?? string.Empty).IndexOf(find, start, comparison);
        return index < 0 ? (text ?? string.Empty).IndexOf(find, comparison) : index;
    }

    public static string ChangeCase(string text, CaseMode mode) => mode switch
    {
        CaseMode.Upper => (text ?? string.Empty).ToUpperInvariant(),
        CaseMode.Lower => (text ?? string.Empty).ToLowerInvariant(),
        CaseMode.Title => string.Concat((text ?? string.Empty).Select((c, i) =>
            i == 0 || char.IsWhiteSpace((text ?? string.Empty)[i - 1]) ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c))),
        CaseMode.Sentence => Sentence(text),
        _ => text ?? string.Empty
    };

    private static string Sentence(string? text)
    {
        var value = text ?? string.Empty;
        if (value.Length == 0) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}

public enum CaseMode
{
    Upper,
    Lower,
    Title,
    Sentence
}

public readonly record struct TextStatistics(int Lines, int Words, int Characters, int CharactersNoSpaces);
