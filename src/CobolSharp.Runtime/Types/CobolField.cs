using System.Text;

namespace CobolSharp.Runtime.Types;

/// <summary>
/// Represents a COBOL data item at runtime. Stores data as byte[] with metadata
/// for PICTURE, USAGE, and size. Supports the dual-layer approach: byte[] storage
/// with decimal computation.
/// </summary>
public class CobolField
{
    private readonly byte[] _data;

    public string Name { get; }
    public int Size { get; }
    public FieldType Type { get; }
    public int IntegerDigits { get; }
    public int DecimalDigits { get; }
    public bool IsSigned { get; }

    public CobolField(string name, int size, FieldType type,
        int integerDigits = 0, int decimalDigits = 0, bool isSigned = false)
    {
        Name = name;
        Size = size;
        Type = type;
        IntegerDigits = integerDigits;
        DecimalDigits = decimalDigits;
        IsSigned = isSigned;
        _data = new byte[size];

        // Initialize: numeric fields get ASCII '0', alphanumeric get spaces
        byte fill = type == FieldType.Numeric ? (byte)'0' : (byte)' ';
        Array.Fill(_data, fill);
    }

    /// <summary>
    /// Get the raw byte storage.
    /// </summary>
    public ReadOnlySpan<byte> RawBytes => _data;

    /// <summary>
    /// Get the display string value (ASCII interpretation).
    /// </summary>
    public string GetDisplayValue()
    {
        return Encoding.ASCII.GetString(_data).TrimEnd();
    }

    /// <summary>
    /// Get the numeric decimal value (for arithmetic).
    /// </summary>
    public decimal GetNumericValue()
    {
        if (Type != FieldType.Numeric)
            return 0m;

        string text = Encoding.ASCII.GetString(_data).Trim();

        // Handle sign
        bool negative = false;
        if (text.Length > 0 && text[0] == '-')
        {
            negative = true;
            text = text[1..];
        }
        else if (text.Length > 0 && text[0] == '+')
        {
            text = text[1..];
        }

        if (!decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal result))
        {
            return 0m;
        }

        // Apply implied decimal point from PICTURE V
        if (DecimalDigits > 0)
        {
            // The stored value has no decimal point — we need to insert one
            // e.g., PIC 9(3)V99 storing "12345" means 123.45
            result /= (decimal)Math.Pow(10, DecimalDigits);
        }

        return negative ? -result : result;
    }

    /// <summary>
    /// Set the field from a numeric value.
    /// </summary>
    public void SetNumericValue(decimal value)
    {
        if (Type != FieldType.Numeric)
            return;

        // Scale by implied decimal places
        decimal scaled = value * (decimal)Math.Pow(10, DecimalDigits);
        scaled = Math.Truncate(scaled); // truncate to integer

        bool negative = scaled < 0;
        if (negative) scaled = -scaled;

        string digits = ((long)scaled).ToString();

        // Pad to field size (minus sign position if signed)
        int digitSize = IsSigned ? Size - 1 : Size;
        if (digits.Length > digitSize)
            digits = digits[^digitSize..]; // truncate from left
        else
            digits = digits.PadLeft(digitSize, '0');

        string text = IsSigned
            ? (negative ? "-" : "+") + digits
            : digits;

        byte[] bytes = Encoding.ASCII.GetBytes(text);
        Array.Copy(bytes, 0, _data, 0, Math.Min(bytes.Length, _data.Length));
    }

    /// <summary>
    /// Set the field from a string value (alphanumeric MOVE).
    /// Left-justified, space-padded on right.
    /// </summary>
    public void SetAlphanumericValue(string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        Array.Fill(_data, (byte)' ');
        Array.Copy(bytes, 0, _data, 0, Math.Min(bytes.Length, _data.Length));
    }

    /// <summary>
    /// Set the field to spaces.
    /// </summary>
    public void SetSpaces()
    {
        Array.Fill(_data, (byte)' ');
    }

    /// <summary>
    /// Set the field to zeros.
    /// </summary>
    public void SetZeros()
    {
        Array.Fill(_data, (byte)'0');
    }

    /// <summary>
    /// Set the field to HIGH-VALUE (0xFF).
    /// </summary>
    public void SetHighValues()
    {
        Array.Fill(_data, (byte)0xFF);
    }

    /// <summary>
    /// Set the field to LOW-VALUE (0x00).
    /// </summary>
    public void SetLowValues()
    {
        Array.Fill(_data, (byte)0x00);
    }

    /// <summary>
    /// Set the field to QUOTE (double-quote character).
    /// </summary>
    public void SetQuotes()
    {
        Array.Fill(_data, (byte)'"');
    }

    /// <summary>
    /// Set the field to ALL literal (repeat the literal to fill the field).
    /// </summary>
    public void SetAll(string literal)
    {
        if (string.IsNullOrEmpty(literal)) return;
        byte[] litBytes = Encoding.ASCII.GetBytes(literal);
        for (int i = 0; i < _data.Length; i++)
        {
            _data[i] = litBytes[i % litBytes.Length];
        }
    }
}

public enum FieldType
{
    Numeric,
    Alphabetic,
    Alphanumeric,
}
