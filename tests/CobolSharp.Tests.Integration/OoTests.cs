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

    // ── OO slice 3: INHERITS FROM + the not-yet-supported forms (loud, not silent) ──

    private const string AnimalDog = @"
       IDENTIFICATION DIVISION.
       CLASS-ID. ANIMAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(8) VALUE ""GENERIC"".
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY MSG.
       END METHOD SPEAK.
       END OBJECT.
       END CLASS ANIMAL.
       IDENTIFICATION DIVISION.
       CLASS-ID. DOG INHERITS FROM ANIMAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY ""WOOF"".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS DOG.
";

    [Fact]
    // Polymorphism: a DOG invoked through an ANIMAL-typed reference dispatches to DOG's override (virtual).
    public void Inherits_PolymorphicOverride_DispatchesToSubclass()
    {
        string driver = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOPOLY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ANIMAL.
           CLASS DOG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ANIMAL.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE ANIMAL ""NEW"" RETURNING A.
           INVOKE A ""SPEAK"".
           INVOKE DOG ""NEW"" RETURNING A.
           INVOKE A ""SPEAK"".
           STOP RUN.
       END PROGRAM OOPOLY.
" + AnimalDog;
        var (ok, stdout, stderr) = CompileAndRun(driver, DialectMode.Cobol2002);
        Assert.True(ok, stderr);
        Assert.Equal("GENERIC\nWOOF", stdout.Replace("\r\n", "\n").Trim());
    }

    [Fact]
    // INVOKE SUPER is a later OO slice — it must fail loudly (COBOL0112), not silently drop the statement.
    public void Invoke_SuperTarget_FailsLoudly()
    {
        string driver = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OODRV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ANIMAL.
           CLASS DOG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE OBJECT REFERENCE DOG.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE DOG ""NEW"" RETURNING D.
           INVOKE D ""SPEAK"".
           STOP RUN.
       END PROGRAM OODRV.
       IDENTIFICATION DIVISION.
       CLASS-ID. ANIMAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY ""GENERIC"".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS ANIMAL.
       IDENTIFICATION DIVISION.
       CLASS-ID. DOG INHERITS FROM ANIMAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SUPER ""SPEAK"".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS DOG.
";
        var (ok, _, stderr) = CompileAndRun(driver, DialectMode.Cobol2002);
        Assert.False(ok);
        Assert.Contains("COBOL0112", stderr);
    }

    [Fact]
    // A subclass declaring its OWN OBJECT data is a later OO slice — fail loudly (COBOL0113), not miscompile.
    public void Inherits_SubclassOwnData_FailsLoudly()
    {
        string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OODRV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS B.
           CLASS S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE S.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE S ""NEW"" RETURNING O.
           STOP RUN.
       END PROGRAM OODRV.
       IDENTIFICATION DIVISION.
       CLASS-ID. B.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
       P.
           DISPLAY X.
       END METHOD M.
       END OBJECT.
       END CLASS B.
       IDENTIFICATION DIVISION.
       CLASS-ID. S INHERITS FROM B.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       METHOD-ID. M2.
       PROCEDURE DIVISION.
       P.
           DISPLAY Y.
       END METHOD M2.
       END OBJECT.
       END CLASS S.
";
        var (ok, _, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.False(ok);
        Assert.Contains("COBOL0113", stderr);
    }

    [Fact]
    // COBOL is case-insensitive: a subclass METHOD-ID in different case from the base method must still OVERRIDE
    // it (the override is emitted under the base method's exact name so the .NET case-sensitive slot match holds).
    // Adversarial-review finding — without the fix this silently dispatched to the base (wrong output).
    public void Inherits_CaseMismatchedOverride_StillOverrides()
    {
        string driver = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOCASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ANIMAL.
           CLASS DOG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ANIMAL.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE DOG ""NEW"" RETURNING A.
           INVOKE A ""SPEAK"".
           STOP RUN.
       END PROGRAM OOCASE.
       IDENTIFICATION DIVISION.
       CLASS-ID. ANIMAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY ""GENERIC"".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS ANIMAL.
       IDENTIFICATION DIVISION.
       CLASS-ID. DOG INHERITS FROM ANIMAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. speak.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY ""WOOF"".
       END METHOD speak.
       END OBJECT.
       END CLASS DOG.
";
        var (ok, stdout, stderr) = CompileAndRun(driver, DialectMode.Cobol2002);
        Assert.True(ok, stderr);
        Assert.Equal("WOOF", stdout.Trim()); // the lowercase override must win, not the base GENERIC
    }

    [Fact]
    // A subclass method whose LINKAGE has an OCCURS … INDEXED BY (the index-name is synthetically placed in the
    // WS area) is NOT subclass OBJECT data — it must compile, not trip COBOL0113. Adversarial-review finding.
    public void Inherits_SubclassMethodLinkageIndex_Allowed()
    {
        string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOIDX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS B.
           CLASS S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE S.
       01 T PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE S ""NEW"" RETURNING O.
           INVOKE O ""USE-TBL"" USING T.
           STOP RUN.
       END PROGRAM OOIDX.
       IDENTIFICATION DIVISION.
       CLASS-ID. B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PLACEHOLDER.
       PROCEDURE DIVISION.
       P.
           CONTINUE.
       END METHOD PLACEHOLDER.
       END OBJECT.
       END CLASS B.
       IDENTIFICATION DIVISION.
       CLASS-ID. S INHERITS FROM B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. USE-TBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK.
          05 ELEM PIC 9 OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION USING LK.
       P.
           CONTINUE.
       END METHOD USE-TBL.
       END OBJECT.
       END CLASS S.
";
        var (ok, _, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.True(ok, stderr); // must NOT false-fire COBOL0113
    }

    [Fact]
    // INHERITS FROM a class not in the compilation group → fail loudly (COBOL0114), not silently degrade to a root.
    public void Inherits_UnknownBase_FailsLoudly()
    {
        string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OONOB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS DOG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE OBJECT REFERENCE DOG.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE DOG ""NEW"" RETURNING D.
           STOP RUN.
       END PROGRAM OONOB.
       IDENTIFICATION DIVISION.
       CLASS-ID. DOG INHERITS FROM NOSUCH.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY ""WOOF"".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS DOG.
";
        var (ok, _, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.False(ok);
        Assert.Contains("COBOL0114", stderr);
    }
}
