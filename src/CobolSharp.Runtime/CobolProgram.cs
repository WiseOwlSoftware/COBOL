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
}
