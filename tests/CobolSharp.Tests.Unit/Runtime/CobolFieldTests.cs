using CobolSharp.Runtime.Types;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime;

public class CobolFieldTests
{
    // ── Numeric field basics ──

    [Fact]
    public void NumericField_InitializedToZeros()
    {
        var field = new CobolField("TEST", 5, FieldType.Numeric, integerDigits: 5);
        Assert.Equal("00000", field.GetDisplayValue());
        Assert.Equal(0m, field.GetNumericValue());
    }

    [Fact]
    public void NumericField_SetAndGet()
    {
        var field = new CobolField("TEST", 5, FieldType.Numeric, integerDigits: 5);
        field.SetNumericValue(42);
        Assert.Equal(42m, field.GetNumericValue());
        Assert.Equal("00042", field.GetDisplayValue());
    }

    [Fact]
    public void NumericField_WithDecimalPlaces()
    {
        var field = new CobolField("TEST", 5, FieldType.Numeric, integerDigits: 3, decimalDigits: 2);
        field.SetNumericValue(12.34m);
        Assert.Equal(12.34m, field.GetNumericValue());
        Assert.Equal("01234", field.GetDisplayValue()); // stored without decimal point
    }

    [Fact]
    public void NumericField_Truncation_Left()
    {
        var field = new CobolField("TEST", 3, FieldType.Numeric, integerDigits: 3);
        field.SetNumericValue(12345);
        // COBOL truncates from the left for integer overflow
        Assert.Equal("345", field.GetDisplayValue());
    }

    [Fact]
    public void NumericField_Signed()
    {
        var field = new CobolField("TEST", 6, FieldType.Numeric, integerDigits: 5, isSigned: true);
        field.SetNumericValue(-42);
        Assert.Equal(-42m, field.GetNumericValue());
    }

    [Fact]
    public void NumericField_Signed_Positive()
    {
        var field = new CobolField("TEST", 6, FieldType.Numeric, integerDigits: 5, isSigned: true);
        field.SetNumericValue(42);
        Assert.Equal(42m, field.GetNumericValue());
    }

    // ── Alphanumeric field basics ──

    [Fact]
    public void AlphanumericField_InitializedToSpaces()
    {
        var field = new CobolField("TEST", 10, FieldType.Alphanumeric);
        Assert.Equal("", field.GetDisplayValue()); // TrimEnd removes trailing spaces
    }

    [Fact]
    public void AlphanumericField_SetAndGet()
    {
        var field = new CobolField("TEST", 10, FieldType.Alphanumeric);
        field.SetAlphanumericValue("Hello");
        Assert.Equal("Hello", field.GetDisplayValue());
    }

    [Fact]
    public void AlphanumericField_SpacePadded()
    {
        var field = new CobolField("TEST", 10, FieldType.Alphanumeric);
        field.SetAlphanumericValue("Hi");
        // Raw bytes should be "Hi" + 8 spaces
        var raw = field.RawBytes.ToArray();
        Assert.Equal((byte)'H', raw[0]);
        Assert.Equal((byte)'i', raw[1]);
        Assert.Equal((byte)' ', raw[2]);
    }

    [Fact]
    public void AlphanumericField_Truncated()
    {
        var field = new CobolField("TEST", 5, FieldType.Alphanumeric);
        field.SetAlphanumericValue("Hello, World!");
        Assert.Equal("Hello", field.GetDisplayValue());
    }

    // ── Figurative constants ──

    [Fact]
    public void SetSpaces_FillsWithSpaces()
    {
        var field = new CobolField("TEST", 5, FieldType.Numeric, integerDigits: 5);
        field.SetNumericValue(42);
        field.SetSpaces();
        Assert.Equal("", field.GetDisplayValue()); // all spaces, trimmed = empty
    }

    [Fact]
    public void SetZeros_FillsWithZeros()
    {
        var field = new CobolField("TEST", 5, FieldType.Alphanumeric);
        field.SetAlphanumericValue("Hello");
        field.SetZeros();
        Assert.Equal("00000", field.GetDisplayValue());
    }

    // ── Arithmetic helpers ──

    [Fact]
    public void Arithmetic_AddTo()
    {
        var field = new CobolField("TEST", 5, FieldType.Numeric, integerDigits: 5);
        field.SetNumericValue(100);
        // Simulate ADD 25 TO TEST
        field.SetNumericValue(field.GetNumericValue() + 25);
        Assert.Equal(125m, field.GetNumericValue());
    }

    [Fact]
    public void Arithmetic_SubtractFrom()
    {
        var field = new CobolField("TEST", 5, FieldType.Numeric, integerDigits: 5);
        field.SetNumericValue(100);
        field.SetNumericValue(field.GetNumericValue() - 30);
        Assert.Equal(70m, field.GetNumericValue());
    }

    // ── Cross-type MOVE ──

    [Fact]
    public void Move_NumericToAlphanumeric()
    {
        var source = new CobolField("SRC", 5, FieldType.Numeric, integerDigits: 5);
        source.SetNumericValue(42);
        var target = new CobolField("TGT", 10, FieldType.Alphanumeric);
        target.SetAlphanumericValue(source.GetDisplayValue());
        Assert.Equal("00042", target.GetDisplayValue());
    }

    [Fact]
    public void Move_AlphanumericToNumeric()
    {
        var source = new CobolField("SRC", 5, FieldType.Alphanumeric);
        source.SetAlphanumericValue("00042");
        var target = new CobolField("TGT", 5, FieldType.Numeric, integerDigits: 5);
        // Parse numeric value from alphanumeric source
        if (decimal.TryParse(source.GetDisplayValue().Trim(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal val))
        {
            target.SetNumericValue(val);
        }
        Assert.Equal(42m, target.GetNumericValue());
    }
}
