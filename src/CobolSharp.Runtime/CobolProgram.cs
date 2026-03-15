// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime.Types;

namespace CobolSharp.Runtime;

/// <summary>
/// Base class for all compiled COBOL programs. Each PROGRAM-ID becomes a class
/// that derives from this. Provides runtime services (DISPLAY, MOVE, arithmetic).
/// </summary>
public abstract class CobolProgram
{
    /// <summary>
    /// Execute the procedure division.
    /// </summary>
    public abstract void Run();

    /// <summary>
    /// DISPLAY statement — writes to stdout.
    /// </summary>
    protected static void Display(params object[] values)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var val in values)
        {
            if (val is CobolField field)
                sb.Append(field.GetDisplayValue());
            else
                sb.Append(val?.ToString() ?? "");
        }
        Console.WriteLine(sb.ToString());
    }

    /// <summary>
    /// MOVE numeric value to field.
    /// </summary>
    protected static void MoveNumeric(decimal value, CobolField target)
    {
        target.SetNumericValue(value);
    }

    /// <summary>
    /// MOVE string value to field.
    /// </summary>
    protected static void MoveAlphanumeric(string value, CobolField target)
    {
        if (target.Type == FieldType.Numeric)
        {
            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal num))
            {
                target.SetNumericValue(num);
            }
        }
        else
        {
            target.SetAlphanumericValue(value);
        }
    }

    /// <summary>
    /// MOVE field to field.
    /// </summary>
    protected static void MoveField(CobolField source, CobolField target)
    {
        if (source.Type == FieldType.Numeric && target.Type == FieldType.Numeric)
        {
            target.SetNumericValue(source.GetNumericValue());
        }
        else
        {
            target.SetAlphanumericValue(source.GetDisplayValue());
        }
    }

    /// <summary>
    /// MOVE figurative constant to field.
    /// </summary>
    protected static void MoveSpace(CobolField target) => target.SetSpaces();
    protected static void MoveZero(CobolField target) => target.SetZeros();
    protected static void MoveHighValue(CobolField target) => target.SetHighValues();
    protected static void MoveLowValue(CobolField target) => target.SetLowValues();
    protected static void MoveQuote(CobolField target) => target.SetQuotes();
    protected static void MoveAll(string literal, CobolField target) => target.SetAll(literal);

    /// <summary>
    /// ADD: target = target + value
    /// </summary>
    protected static void AddTo(decimal value, CobolField target)
    {
        target.SetNumericValue(target.GetNumericValue() + value);
    }

    /// <summary>
    /// SUBTRACT: target = target - value
    /// </summary>
    protected static void SubtractFrom(decimal value, CobolField target)
    {
        target.SetNumericValue(target.GetNumericValue() - value);
    }

    /// <summary>
    /// MULTIPLY: target = target * value
    /// </summary>
    protected static void MultiplyBy(decimal value, CobolField target)
    {
        target.SetNumericValue(target.GetNumericValue() * value);
    }

    /// <summary>
    /// DIVIDE: target = target / value (truncated to field's decimal places)
    /// </summary>
    protected static void DivideInto(decimal value, CobolField target)
    {
        if (value == 0)
        {
            // COBOL SIZE ERROR condition — for now, leave target unchanged
            return;
        }
        target.SetNumericValue(target.GetNumericValue() / value);
    }

    /// <summary>
    /// DIVIDE giving quotient and remainder: quotient = dividend / divisor
    /// </summary>
    protected static void DivideGiving(decimal dividend, decimal divisor,
        CobolField quotient, CobolField? remainder)
    {
        if (divisor == 0) return;
        decimal q = Math.Truncate(dividend / divisor);
        quotient.SetNumericValue(q);
        if (remainder != null)
        {
            remainder.SetNumericValue(dividend - (q * divisor));
        }
    }

    // ── File I/O helpers ──

    /// <summary>
    /// Read the next sequential record into the record field, returning the file status.
    /// </summary>
    protected static string FileReadNext(IO.CobolFileManager fm, string fileName,
        byte[] buffer, CobolField recordField)
    {
        string status = fm.ReadNext(fileName, buffer);
        if (status == IO.FileStatus.Success)
            recordField.SetFromBytes(buffer);
        return status;
    }

    /// <summary>
    /// Write the record field to a file, returning the file status.
    /// </summary>
    protected static string FileWrite(IO.CobolFileManager fm, string fileName,
        byte[] buffer, CobolField recordField)
    {
        recordField.CopyToBytes(buffer);
        return fm.Write(fileName, buffer);
    }

    /// <summary>
    /// Rewrite the current record from the record field.
    /// </summary>
    protected static string FileRewrite(IO.CobolFileManager fm, string fileName,
        byte[] buffer, CobolField recordField)
    {
        recordField.CopyToBytes(buffer);
        return fm.Rewrite(fileName, buffer);
    }

    /// <summary>
    /// ACCEPT target FROM CONSOLE — reads one line from stdin and moves it to target.
    /// </summary>
    protected static void AcceptFromConsole(CobolField target)
    {
        string input = Console.ReadLine() ?? "";
        if (target.Type == FieldType.Numeric)
        {
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal num))
            {
                target.SetNumericValue(num);
            }
        }
        else
        {
            target.SetAlphanumericValue(input);
        }
    }

    /// <summary>
    /// ACCEPT target FROM DATE — sets target to current date as YYYYMMDD (8 digits).
    /// </summary>
    protected static void AcceptDate(CobolField target)
    {
        string value = DateTime.Today.ToString("yyyyMMdd");
        if (target.Type == FieldType.Numeric)
            target.SetNumericValue(decimal.Parse(value));
        else
            target.SetAlphanumericValue(value);
    }

    /// <summary>
    /// ACCEPT target FROM DAY — sets target to current day-of-year as YYYYDDD (7 digits).
    /// </summary>
    protected static void AcceptDay(CobolField target)
    {
        DateTime today = DateTime.Today;
        string value = today.Year.ToString("D4") + today.DayOfYear.ToString("D3");
        if (target.Type == FieldType.Numeric)
            target.SetNumericValue(decimal.Parse(value));
        else
            target.SetAlphanumericValue(value);
    }

    /// <summary>
    /// ACCEPT target FROM TIME — sets target to current time as HHMMSSss (8 digits,
    /// ss = hundredths of a second).
    /// </summary>
    protected static void AcceptTime(CobolField target)
    {
        DateTime now = DateTime.Now;
        int hundredths = now.Millisecond / 10;
        string value = now.ToString("HHmmss") + hundredths.ToString("D2");
        if (target.Type == FieldType.Numeric)
            target.SetNumericValue(decimal.Parse(value));
        else
            target.SetAlphanumericValue(value);
    }

    /// <summary>
    /// INITIALIZE target — numeric fields receive zeros, alphanumeric fields receive spaces.
    /// </summary>
    protected static void InitializeField(CobolField target)
    {
        if (target.Type == FieldType.Numeric)
            target.SetZeros();
        else
            target.SetSpaces();
    }

    /// <summary>
    /// CALL programName — attempts to find and run a CobolProgram subclass with the given name.
    /// Searches loaded assemblies for a type matching the program name that derives from CobolProgram.
    /// </summary>
    protected static void CallProgram(string programName, CobolField[] parameters)
    {
        // Search loaded assemblies for a CobolProgram subclass matching the name
        string upperName = programName.Trim().ToUpperInvariant();
        Type? programType = null;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(CobolProgram)) &&
                        string.Equals(type.Name, upperName, StringComparison.OrdinalIgnoreCase))
                    {
                        programType = type;
                        break;
                    }
                }
            }
            catch { /* skip assemblies that can't be reflected */ }
            if (programType != null) break;
        }

        if (programType == null)
        {
            Console.Error.WriteLine($"CALL: program '{programName}' not found in loaded assemblies");
            return;
        }

        // Instantiate and call Run()
        var instance = (CobolProgram)Activator.CreateInstance(programType)!;
        instance.Run();
    }

    /// <summary>
    /// STRING sources DELIMITED BY delimiters INTO target [WITH POINTER pointer].
    /// Each source string is appended to target up to (but not including) its delimiter.
    /// If a delimiter entry is null the whole source value is used.
    /// pointer, if supplied, tracks the next write position (1-based) in target and is
    /// updated on exit.
    /// </summary>
    protected static void StringConcat(CobolField[] sources, string?[] delimiters,
        CobolField target, CobolField? pointer)
    {
        // Determine starting position in target (1-based, default 1)
        int startPos = 1;
        if (pointer != null)
            startPos = (int)pointer.GetNumericValue();
        if (startPos < 1) startPos = 1;

        string targetText = target.GetDisplayValue().PadRight(target.Size);
        char[] buffer = targetText.ToCharArray();
        if (buffer.Length < target.Size)
            Array.Resize(ref buffer, target.Size);

        int pos = startPos - 1; // convert to 0-based index

        for (int i = 0; i < sources.Length && pos < buffer.Length; i++)
        {
            string srcText = sources[i].GetDisplayValue();
            string? delim = i < delimiters.Length ? delimiters[i] : null;

            // Trim at delimiter if one is specified
            string chunk;
            if (!string.IsNullOrEmpty(delim))
            {
                int delimIdx = srcText.IndexOf(delim, StringComparison.Ordinal);
                chunk = delimIdx >= 0 ? srcText[..delimIdx] : srcText;
            }
            else
            {
                chunk = srcText;
            }

            foreach (char c in chunk)
            {
                if (pos >= buffer.Length) break;
                buffer[pos++] = c;
            }
        }

        target.SetAlphanumericValue(new string(buffer));
        pointer?.SetNumericValue(pos + 1); // update pointer to next write position (1-based)
    }

    /// <summary>
    /// UNSTRING source DELIMITED BY delimiter INTO targets [TALLYING IN tallying].
    /// Splits source on delimiter and distributes segments into targets left-to-right.
    /// If delimiter is null, splits on a single space (COBOL default).
    /// tallying, if supplied, is incremented by the number of targets populated.
    /// </summary>
    protected static void UnstringField(CobolField source, string? delimiter,
        CobolField[] targets, CobolField? tallying)
    {
        string srcText = source.GetDisplayValue();
        string delim = string.IsNullOrEmpty(delimiter) ? " " : delimiter;

        string[] parts = srcText.Split(delim, StringSplitOptions.None);

        int count = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            string segment = i < parts.Length ? parts[i] : "";
            if (targets[i].Type == FieldType.Numeric)
            {
                if (decimal.TryParse(segment, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal num))
                {
                    targets[i].SetNumericValue(num);
                }
            }
            else
            {
                targets[i].SetAlphanumericValue(segment);
            }
            count++;
        }

        if (tallying != null)
            tallying.SetNumericValue(tallying.GetNumericValue() + count);
    }

    /// <summary>
    /// INSPECT target REPLACING — replaces occurrences of searchFor with replaceWith.
    /// If allOccurrences is true, replaces every occurrence; otherwise only the first.
    /// </summary>
    protected static void InspectReplacing(CobolField target, string searchFor,
        string replaceWith, bool allOccurrences)
    {
        if (string.IsNullOrEmpty(searchFor)) return;

        string text = target.GetDisplayValue().PadRight(target.Size);

        string result = allOccurrences
            ? text.Replace(searchFor, replaceWith, StringComparison.Ordinal)
            : ReplaceFirst(text, searchFor, replaceWith);

        target.SetAlphanumericValue(result);
    }

    private static string ReplaceFirst(string text, string searchFor, string replaceWith)
    {
        int idx = text.IndexOf(searchFor, StringComparison.Ordinal);
        if (idx < 0) return text;
        return text[..idx] + replaceWith + text[(idx + searchFor.Length)..];
    }

    /// <summary>
    /// INSPECT target TALLYING counter FOR — counts occurrences of searchFor in target
    /// and adds the count to counter.
    /// If allOccurrences is true, counts every non-overlapping occurrence;
    /// otherwise counts only the first.
    /// </summary>
    protected static void InspectTallying(CobolField target, CobolField counter,
        string searchFor, bool allOccurrences)
    {
        if (string.IsNullOrEmpty(searchFor)) return;

        string text = target.GetDisplayValue();
        int count = 0;

        if (allOccurrences)
        {
            int idx = 0;
            while (idx <= text.Length - searchFor.Length)
            {
                int found = text.IndexOf(searchFor, idx, StringComparison.Ordinal);
                if (found < 0) break;
                count++;
                idx = found + searchFor.Length; // non-overlapping
            }
        }
        else
        {
            if (text.Contains(searchFor, StringComparison.Ordinal))
                count = 1;
        }

        counter.SetNumericValue(counter.GetNumericValue() + count);
    }
}
