using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// OO COBOL (COBOL-2002) semantic/emit tests beyond the positive conformance corpus
/// (tests/conformance/2002/oo_*). Focused on diagnostics for the not-yet-supported INVOKE
/// argument forms — they must FAIL LOUDLY (COBOL0111), never silently drop an argument and
/// miscompile (an adversarial-review finding, DEVLOG 448).
/// </summary>
public sealed class OoTests : EndToEndTestBase
{
    private const string ClassAcc = @"
       IDENTIFICATION DIVISION.
       CLASS-ID. ACC.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. ADDTO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-AMT PIC 9(4).
       01 LK-RES PIC 9(4).
       PROCEDURE DIVISION USING LK-AMT RETURNING LK-RES.
       MAIN.
           ADD LK-AMT TO BAL.
           MOVE BAL TO LK-RES.
       END METHOD ADDTO.
       END OBJECT.
       END CLASS ACC.
";

    private static string Driver(string invokeUsing) => $@"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OODRV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ACC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACC.
       01 AMT PIC 9(4) VALUE 7.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE ACC ""NEW"" RETURNING A.
           {invokeUsing}
           DISPLAY ""R="" R.
           STOP RUN.
       END PROGRAM OODRV.
" + ClassAcc;

    [Theory]
    // Each grammar-legal-but-unsupported USING form must be rejected with COBOL0111, not silently dropped
    // (a silent drop shifts the trailing RETURNING slot → wrong binding / runtime crash).
    [InlineData("INVOKE A \"ADDTO\" USING 10 RETURNING R.")]                  // bare literal
    [InlineData("INVOKE A \"ADDTO\" USING BY CONTENT AMT RETURNING R.")]      // BY CONTENT
    [InlineData("INVOKE A \"ADDTO\" USING BY VALUE AMT RETURNING R.")]        // BY VALUE
    public void Invoke_UnsupportedArgForm_FailsLoudly(string invoke)
    {
        var (ok, _, stderr) = CompileAndRun(Driver(invoke), DialectMode.Cobol2002);
        Assert.False(ok); // a clean compile error, never a silent miscompile / crash
        Assert.Contains("COBOL0111", stderr);
    }

    [Fact]
    // The supported BY REFERENCE data-reference form compiles + runs, mutating per-instance OBJECT data.
    public void Invoke_ByReferenceArg_Works()
    {
        var (ok, stdout, stderr) = CompileAndRun(Driver("INVOKE A \"ADDTO\" USING AMT RETURNING R."),
            DialectMode.Cobol2002);
        Assert.True(ok, stderr);
        Assert.Equal("R=0007", stdout.Trim());
    }
}
