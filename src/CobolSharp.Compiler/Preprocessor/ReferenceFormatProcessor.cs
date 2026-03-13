namespace CobolSharp.Compiler.Preprocessor;

/// <summary>
/// Detects and converts fixed-form COBOL reference format to free-form.
/// Fixed-form: columns 1-6 sequence, 7 indicator, 8-72 source, 73+ comment.
/// Free-form: no column restrictions.
/// </summary>
public static class ReferenceFormatProcessor
{
    /// <summary>
    /// Auto-detect whether source is fixed-form or free-form, and normalize to free-form.
    /// </summary>
    public static string NormalizeToFreeForm(string sourceText)
    {
        if (IsFixedForm(sourceText))
            return ConvertFixedToFree(sourceText);
        return sourceText;
    }

    /// <summary>
    /// Heuristic detection of fixed-form. Checks:
    /// - Lines are consistently >= 7 chars
    /// - Column 7 often contains space, *, or -
    /// - Columns 1-6 are often digits or spaces
    /// </summary>
    public static bool IsFixedForm(string sourceText)
    {
        var lines = sourceText.Split('\n');
        int fixedIndicators = 0;
        int totalLines = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            totalLines++;

            if (line.Length >= 7)
            {
                char indicator = line[6];
                // Column 7 is typically space, *, /, D, or -
                if (indicator == ' ' || indicator == '*' || indicator == '/' ||
                    indicator == 'D' || indicator == 'd' || indicator == '-')
                {
                    // Check columns 1-6 are digits or spaces
                    bool seqOk = true;
                    for (int i = 0; i < 6 && i < line.Length; i++)
                    {
                        if (!char.IsDigit(line[i]) && line[i] != ' ')
                        {
                            seqOk = false;
                            break;
                        }
                    }
                    if (seqOk) fixedIndicators++;
                }
            }
        }

        // If > 60% of non-empty lines look fixed-form, treat as fixed-form
        return totalLines > 0 && fixedIndicators * 100 / totalLines > 60;
    }

    /// <summary>
    /// Convert fixed-form source to free-form by stripping columns 1-6 and 73+,
    /// handling indicator column (7), and joining continuation lines.
    /// </summary>
    public static string ConvertFixedToFree(string sourceText)
    {
        var lines = sourceText.Split('\n');
        var result = new System.Text.StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            if (line.Length < 7)
            {
                result.AppendLine(); // blank/short line
                continue;
            }

            char indicator = line[6];

            switch (indicator)
            {
                case '*':
                case '/':
                    // Comment line — convert to free-form comment
                    string commentText = line.Length > 7 ? line[7..].TrimEnd() : "";
                    result.AppendLine($"*> {commentText}");
                    break;

                case 'D':
                case 'd':
                    // Debug line — treat as comment for now
                    string debugText = line.Length > 7 ? line[7..].TrimEnd() : "";
                    result.AppendLine($"*> DEBUG: {debugText}");
                    break;

                case '-':
                    // Continuation line — append to previous line (remove leading spaces)
                    string contText = line.Length > 7 ? line[7..] : "";
                    // Truncate at column 72 (index 71)
                    if (contText.Length > 65) contText = contText[..65];
                    // Remove the last newline from result and append continuation
                    if (result.Length > 0)
                    {
                        // Trim trailing newline
                        while (result.Length > 0 && (result[result.Length - 1] == '\n' || result[result.Length - 1] == '\r'))
                            result.Length--;
                    }
                    result.AppendLine(contText.TrimStart());
                    break;

                default:
                    // Normal line — extract columns 8-72
                    string sourceArea = line.Length > 7 ? line[7..] : "";
                    // Truncate at column 72 (index 71, which is char 65 in the source area)
                    if (sourceArea.Length > 65) sourceArea = sourceArea[..65];
                    result.AppendLine(sourceArea.TrimEnd());
                    break;
            }
        }

        return result.ToString();
    }
}
