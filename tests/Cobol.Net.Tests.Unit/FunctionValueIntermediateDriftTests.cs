// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE INTERMEDIATE THAT HANDS A NUMERIC FUNCTION'S VALUE TO SEVERAL USES IS DESCRIBED FOR THAT VALUE, NEVER A FIXED
/// S9(21)V9(9) (kb/Work PB3268): the §15.4.1 float family is held on a binary64 under native arithmetic and on a
/// fixed-point item under a decimal-engine mode, over EVERY <c>Float: true</c> catalog row, and
/// <c>SendingValueTemp.FunctionValuePic</c> is read only by the questions asked of the returned-value item itself.
/// </summary>
/// <remarks>
/// <para><b>The rule.</b> ISO §14.9.25.4 GR1 writes a function-identifier's single evaluation as "MOVE a TO temp /
/// MOVE temp TO b / MOVE temp to c … where 'temp' is an intermediate result item provided by the implementor", and
/// §15.4.1 requires "the returned value is the same for all instances of a given function within a single execution
/// of the runtime element". So <c>MOVE FUNCTION PI TO Q1 Q2</c> must store what <c>MOVE FUNCTION PI TO Q3</c> stores;
/// a scale-9 intermediate stored 3.141592653000000 in the first two.</para>
/// <para><b>What this holds.</b> The float family's native returned value is a binary64
/// (<c>ReceiverContext.FloatLanding</c>), so the intermediate is a <c>double</c> field and each receiver's own float
/// landing quantizes once at its own scale; under a decimal-engine mode the same value is an SDIDI
/// (§15.4.1), which a <c>double</c> intermediate would round away, so the intermediate is a scaled-integer field.
/// The population is read out of the catalog (<see cref="StandardModeReturnedValueContainerDriftTests"/> keeps the
/// fixture table honest), so a float-family function added tomorrow inherits the property or fails here.</para>
/// <para>The second test holds the other half: the fixed S9(21)V9(9) description is the RETURNED-VALUE ITEM's (what
/// <c>FUNCTION LENGTH</c> measures and IN-ARITHMETIC-RANGE tests, DOC-A.1-92), so only those two readers may ask for
/// it; a statement-level intermediate that reads it again is this defect again.</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class FunctionValueIntermediateDriftTests : CobolNetTestBase
{
    /// <summary>The generated C# field an intermediate result item is declared as — the type is the carrier.</summary>
    private static readonly Regex IntermediateField = new(
        @"private (?<type>\w+) __sendval\d+_MOVE\b", RegexOptions.Compiled);

    [Fact]
    public void EveryFloatFamilyFunction_MovedToTwoReceivers_UnderNative_IsHeldOnABinary64()
    {
        var offenders = new List<string>();
        int i = 0;
        foreach (string name in StandardModeReturnedValueContainerDriftTests.CatalogFloatRows().OrderBy(n => n, StringComparer.Ordinal))
        {
            string reference = StandardModeReturnedValueContainerDriftTests.Fixtures[name];
            string types = IntermediateTypes(Compile("FVINAT" + (++i).ToString("00"), reference, standardDecimal: false));
            if (types != "double")
                offenders.Add($"{name}: the MOVE intermediate is declared as [{types}], not a double — the single-receiver MOVE lands the binary64");
        }
        Assert.True(offenders.Count == 0,
            "ISO §14.9.25.4 GR1 / §15.4.1 — a float-family function moved to more than one receiver must hand each the "
            + "value the single-receiver MOVE stores; an intermediate narrower than the binary64 loses it (kb/Work PB3268):\n  "
            + string.Join("\n  ", offenders));
    }

    [Fact]
    public void EveryFloatFamilyFunction_MovedToTwoReceivers_UnderStandardDecimal_IsNotRoundedThroughABinary64()
    {
        var offenders = new List<string>();
        int i = 0;
        foreach (string name in StandardModeReturnedValueContainerDriftTests.CatalogFloatRows().OrderBy(n => n, StringComparer.Ordinal))
        {
            string reference = StandardModeReturnedValueContainerDriftTests.Fixtures[name];
            string types = IntermediateTypes(Compile("FVISD" + (++i).ToString("00"), reference, standardDecimal: true));
            if (types.Contains("double", StringComparison.Ordinal))
                offenders.Add($"{name}: the MOVE intermediate is declared as [{types}] — a binary64 rounds the SDIDI §15.4.1 requires");
        }
        Assert.True(offenders.Count == 0,
            "ISO §15.4.1 — under a standard arithmetic mode the returned value is contained in an SDIDI; the intermediate "
            + "that carries it to several receivers must not round it through a binary64 (kb/Work PB3268):\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>⛔ THE BINARY64 INTERMEDIATE OF A FLOAT-FAMILY VALUE IS READ AS THE APPROXIMATION IT HOLDS (kb/Work PB1606).
    /// A floating-point data item is read by its exact value and an approximation by its shortest round-trip decimal
    /// (docs/CONFORMANCE.md), and the binary64 cell cannot say which it holds; the intermediate is the returned value
    /// (§14.9.25.4 GR1), so each floating-point numeric-edited receiver edits it through <c>CobolDec.FromApproximation</c>,
    /// as the single-receiver MOVE edits the function. Without the flag <c>MOVE FUNCTION PI TO W1 W2</c> stored
    /// +3.14159265358979311599E+00 where <c>MOVE FUNCTION PI TO W3</c> stores +3.14159265358979300000E+00
    /// (the Linux landing gate's red on <c>2002/pb370_move_function_value_digits</c>).</summary>
    [Fact]
    public void EveryFloatFamilyFunction_MovedToTwoFloatEditedReceivers_ReadsTheIntermediateAsAnApproximation()
    {
        var offenders = new List<string>();
        int i = 0;
        foreach (string name in StandardModeReturnedValueContainerDriftTests.CatalogFloatRows().OrderBy(n => n, StringComparer.Ordinal))
        {
            string reference = StandardModeReturnedValueContainerDriftTests.Fixtures[name];
            string generated = Compile("FVIAP" + (++i).ToString("00"), reference, standardDecimal: false,
                receivers: "       01 Q1 PIC +9.9(20)E+99.\n       01 Q2 PIC +9.9(20)E+99.\n");
            int approximate = ApproximationRead.Matches(generated).Count;
            if (approximate != 2)
                offenders.Add($"{name}: {approximate} of the 2 float-edited stores read the intermediate through FromApproximation");
        }
        Assert.True(offenders.Count == 0,
            "ISO §14.9.25.4 GR1 — a float-family function moved to two floating-point numeric-edited receivers must edit "
            + "in each what the single-receiver MOVE edits; the binary64 intermediate holds an approximation, never a float "
            + "data item (kb/Work PB1606):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>A floating-point edited store that reads a MOVE intermediate as an approximation.</summary>
    private static readonly Regex ApproximationRead = new(
        @"FormatFloatStore\(CobolDec\.FromApproximation\(\(double\)\(__sendval\d+_MOVE\)\)", RegexOptions.Compiled);

    /// <summary>⛔ AN EXACT VALUE WHOSE EVERY RECEIVER IS FLOATING-POINT IS HELD IN THE RECEIVERS' OWN BINARY FORMAT, AND A
    /// BINARY32 SET NEVER ON A BINARY64 (kb/Work PB3268, review R2-1). ISO §14.6.8.3 rule 2 converts the value to a
    /// binary32 receiver "in a manner consistent with the specifications of ISO/IEC 60559" — one rounding; a binary64
    /// intermediate narrowed into a binary32 receiver is two (kb/Work PB1110). A binary32 receiver beside a binary64 one,
    /// and a floating-point receiver beside a fixed-point one, have no float format that converts once for both and no
    /// fixed-point window that holds the float receiver's range, so those sets are held on the 36-digit decimal
    /// floating-point item (a <c>string</c> field; kb/Work PB3268 review round 3, R1-1 and R1-2: a 12-integer-digit
    /// window cut a FLOAT-LONG beside a PIC S9(3)V99, and an S9(21)V9(17) item stored 1.234E-21 as 0). A set of
    /// fixed-point receivers keeps its fixed-point item. A float-family function's value IS a binary64 and is held on
    /// one whatever its receivers.</summary>
    [Theory]
    [InlineData("FVIB01", "FUNCTION NUMVAL(\"1.5\")", "USAGE FLOAT-BINARY-32", "USAGE FLOAT-SHORT", "float")]
    [InlineData("FVIB02", "FUNCTION INTEGER(7.5)", "USAGE FLOAT-BINARY-32", "USAGE FLOAT-SHORT", "float")]
    [InlineData("FVIB03", "FUNCTION NUMVAL(\"1.5\")", "USAGE FLOAT-LONG", "USAGE FLOAT-BINARY-64", "double")]
    [InlineData("FVIB04", "FUNCTION NUMVAL(\"1.5\")", "USAGE FLOAT-BINARY-32", "USAGE FLOAT-LONG", "string")]
    [InlineData("FVIB05", "FUNCTION INTEGER(7.5)", "USAGE FLOAT-SHORT", "USAGE FLOAT-LONG", "string")]
    [InlineData("FVIB06", "FUNCTION SQRT(2)", "USAGE FLOAT-BINARY-32", "USAGE FLOAT-SHORT", "double")]
    [InlineData("FVIB07", "FUNCTION NUMVAL(\"1.5\")", "PIC S9(3)V99", "USAGE FLOAT-LONG", "string")]
    [InlineData("FVIB08", "FUNCTION INTEGER(7.5)", "PIC S9(3)", "USAGE FLOAT-SHORT", "string")]
    [InlineData("FVIB09", "FUNCTION NUMVAL(\"1.5\")", "PIC S9(3)V99", "PIC S9(5)V9", "long")]
    public void AFloatReceiverSet_HoldsTheValueInOneFormatThatConvertsOnceForEveryReceiver(
        string programId, string reference, string clause1, string clause2, string expected)
    {
        string types = IntermediateTypes(Compile(programId, reference, standardDecimal: false,
            receivers: $"       01 Q1 {clause1}.\n       01 Q2 {clause2}.\n"));
        Assert.True(types == expected,
            $"ISO §14.6.8.3 rule 2 / §14.9.25.4 GR1 — MOVE {reference} TO Q1 ({clause1}) Q2 ({clause2}): the intermediate is "
            + $"[{types}], expected [{expected}] (kb/Work PB3268 review R2-1, R1-1, R1-2: each receiver is owed the one "
            + "conversion its single-receiver MOVE makes)");
    }

    [Fact]
    public void TheReturnedValueItemDescription_IsReadOnlyByTheQuestionsAskedOfTheItem()
    {
        // FunctionValuePic / NumericReturnedValuePic are the §15.4 returned-value item (LENGTH, BYTE-LENGTH,
        // IN-ARITHMETIC-RANGE). A statement's intermediate is SendingValueTemp.IntermediatePic's.
        var allowed = new[]
        {
            Path.Combine("Binding", "Procedure", "SendingValueTemp.cs"),
            Path.Combine("Binding", "AlgebraicRange.cs"),
            Path.Combine("Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"),
        };
        var readers = new List<string>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
            string rel = Path.GetRelativePath(root, file);
            if (allowed.Contains(rel)) continue;
            foreach (string line in File.ReadLines(file))
            {
                string code = line.TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                if (code.Contains("SendingValueTemp.FunctionValuePic", StringComparison.Ordinal)
                    || code.Contains("SendingValueTemp.NumericReturnedValuePic", StringComparison.Ordinal))
                    readers.Add(rel);
            }
        }
        Assert.True(readers.Count == 0,
            "the S9(21)V9(9) returned-value item description is asked for outside the three places that ask a question of "
            + "the ITEM (kb/Work PB3268) — a statement's intermediate is described by SendingValueTemp.IntermediatePic:\n  "
            + string.Join("\n  ", readers.Distinct()));
    }

    /// <summary>The declared C# types of every MOVE intermediate in the generated program, comma-joined.</summary>
    private static string IntermediateTypes(string generated)
    {
        var types = IntermediateField.Matches(generated).Select(m => m.Groups["type"].Value).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList();
        return types.Count == 0 ? "(none)" : string.Join(",", types);
    }

    /// <summary>The two fixed-point receivers the float-family sweeps move a value to.</summary>
    private const string FixedReceivers = "       01 Q1 PIC S9(9)V9(15).\n       01 Q2 PIC S9(9)V9(15).\n";

    /// <summary>Compile one program that moves <paramref name="reference"/> to the receivers Q1 and Q2 that
    /// <paramref name="receivers"/> declares (two fixed-point items by default) and return its generated C#.</summary>
    private string Compile(string programId, string reference, bool standardDecimal, string receivers = FixedReceivers)
    {
        string options = standardDecimal
            ? "       OPTIONS.\n           ARITHMETIC IS STANDARD-DECIMAL.\n"
            : "";
        string source =
            "       IDENTIFICATION DIVISION.\n"
            + $"       PROGRAM-ID. {programId}.\n"
            + options
            + "       DATA DIVISION.\n"
            + "       WORKING-STORAGE SECTION.\n"
            + receivers
            + "       PROCEDURE DIVISION.\n"
            + "       MAIN.\n"
            + $"           MOVE {reference} TO Q1 Q2\n"
            + "           STOP RUN.\n";
        string srcPath = Path.Combine(TempDir, programId + ".cob");
        File.WriteAllText(srcPath, source);
        var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(result.Success, $"[{programId}] {reference} must compile: {string.Join("\n", result.Errors)}");
        Assert.NotNull(result.GeneratedCsPath);
        return File.ReadAllText(result.GeneratedCsPath!);
    }
}
