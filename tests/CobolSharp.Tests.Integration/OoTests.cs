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

    [Fact]
    // Typed-native OO (ADR §7): a class's OBJECT data is a PER-INSTANCE .NET field, so two objects hold
    // INDEPENDENT state. BUMP displays V (PIC X(3) → an instance `string`) then mutates it; b1/b2/b1 must print
    // ---/---/XYZ. A static (shared) field — the #1 silent-corruption risk — would instead print ---/XYZ/XYZ.
    // A single-object test cannot catch the share bug; this two-object sequence is the adversarial gate.
    public void TypedObjectData_IsPerInstance()
    {
        const string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOINST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 USAGE OBJECT REFERENCE BOX.
       01 B2 USAGE OBJECT REFERENCE BOX.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE BOX ""NEW"" RETURNING B1.
           INVOKE BOX ""NEW"" RETURNING B2.
           INVOKE B1 ""BUMP"".
           INVOKE B2 ""BUMP"".
           INVOKE B1 ""BUMP"".
           STOP RUN.
       END PROGRAM OOINST.
       IDENTIFICATION DIVISION.
       CLASS-ID. BOX.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC X(3) VALUE ""---"".
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY V.
           MOVE ""XYZ"" TO V.
       END METHOD BUMP.
       END OBJECT.
       END CLASS BOX.
";
        var (ok, stdout, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.True(ok, stderr);
        Assert.Equal("---\n---\nXYZ", stdout.Replace("\r\n", "\n").Trim());
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
    // INVOKE SUPER (slice 3b): an override calls the base class's method (non-virtual, no recursion).
    public void Invoke_Super_CallsBaseMethod()
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
           DISPLAY ""ANIMAL"".
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
           DISPLAY ""DOG"".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS DOG.
";
        var (ok, stdout, stderr) = CompileAndRun(driver, DialectMode.Cobol2002);
        Assert.True(ok, stderr);
        Assert.Equal("ANIMAL\nDOG", stdout.Replace("\r\n", "\n").Trim());
    }

    [Fact]
    // Multi-method classes + INVOKE SELF (the keystone): COUNTER has TWO methods sharing per-instance N; DRIVE
    // calls its sibling BUMP via INVOKE SELF twice, then DISPLAYs N. Proves (a) two METHOD-ID bodies on one class
    // (each its own .NET method + dispatch range — both have a MAIN paragraph), (b) shared per-instance typed N,
    // (c) INVOKE SELF → callvirt this (COBOL0112 lifted). N=2.
    public void Invoke_Self_MultiMethodClass_SharesStateAndDispatches()
    {
        const string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOSELF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS COUNTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE COUNTER ""NEW"" RETURNING C.
           INVOKE C ""DRIVE"".
           STOP RUN.
       END PROGRAM OOSELF.
       IDENTIFICATION DIVISION.
       CLASS-ID. COUNTER.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO N.
       END METHOD BUMP.
       METHOD-ID. DRIVE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SELF ""BUMP"".
           INVOKE SELF ""BUMP"".
           DISPLAY ""N="" N.
       END METHOD DRIVE.
       END OBJECT.
       END CLASS COUNTER.
";
        var (ok, stdout, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.True(ok, stderr);
        Assert.Equal("N=2", stdout.Replace("\r\n", "\n").Trim());
    }

    [Fact]
    // Multi-method dispatch isolation: method A (INC, no STOP/GOBACK) defined BEFORE method B (DEC) must NOT fall
    // through into B's paragraphs — invoking only INC prints just "INC=1", never "DEC=...". The exit bound is the
    // method's own last paragraph (not run-to-end). (Adversarial gate for the method-A-falls-into-B risk.)
    public void Invoke_MultiMethod_FirstMethodDoesNotFallIntoSecond()
    {
        const string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOFALL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS COUNTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE COUNTER ""NEW"" RETURNING C.
           INVOKE C ""INC"".
           STOP RUN.
       END PROGRAM OOFALL.
       IDENTIFICATION DIVISION.
       CLASS-ID. COUNTER.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. INC.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO N.
           DISPLAY ""INC="" N.
       END METHOD INC.
       METHOD-ID. DEC.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY ""DEC-RAN"".
       END METHOD DEC.
       END OBJECT.
       END CLASS COUNTER.
";
        var (ok, stdout, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.True(ok, stderr);
        Assert.Equal("INC=1", stdout.Replace("\r\n", "\n").Trim());   // NOT "INC=1\nDEC-RAN"
    }

    [Fact]
    // INVOKE SUPER in a class with no INHERITS FROM base is invalid — a clean COBOL0115 (not a COBOL0600
    // internal-compiler-error at emit time). Adversarial-review finding.
    public void Invoke_SuperInRootClass_FailsLoudly()
    {
        string driver = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OODRV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ANIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ANIMAL.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE ANIMAL ""NEW"" RETURNING A.
           INVOKE A ""SPEAK"".
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
           INVOKE SUPER ""SPEAK"".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS ANIMAL.
";
        var (ok, _, stderr) = CompileAndRun(driver, DialectMode.Cobol2002);
        Assert.False(ok);
        Assert.Contains("COBOL0115", stderr);
        Assert.DoesNotContain("COBOL0600", stderr); // not a misleading internal-compiler-error
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

    [Fact]
    // A SECTION inside a METHOD-ID is not yet method-scoped — its paragraphs would be excluded from the method's
    // dispatch range and SILENTLY SKIPPED. Reject loudly (COBOL0116) rather than emit wrong output. (DEVLOG 455.)
    public void Method_WithSection_FailsLoudly()
    {
        const string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOSEC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE K.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE K ""NEW"" RETURNING O.
           INVOKE O ""DRIVE"".
           STOP RUN.
       END PROGRAM OOSEC.
       IDENTIFICATION DIVISION.
       CLASS-ID. K.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DRIVE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY ""DRIVE-MAIN"".
       DRIVE-SEC SECTION.
       DRIVE-P1.
           DISPLAY ""DRIVE-P1"".
       END METHOD DRIVE.
       END OBJECT.
       END CLASS K.
";
        var (ok, _, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.False(ok);
        Assert.Contains("COBOL0116", stderr);
    }

    [Fact]
    // Multi-method classes work, and a single-method class may have parameters (Invoke_ByReferenceArg_Works), but a
    // class with MULTIPLE methods where any has USING/RETURNING is not yet supported (per-method LINKAGE is a later
    // slice). Reject loudly (COBOL0117) rather than crash at run time with cross-wired param buffers. (DEVLOG 455.)
    public void MultiMethod_WithParams_FailsLoudly()
    {
        const string src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOMP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CALC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE CALC.
       01 X PIC 9(4) VALUE 5.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CALC ""NEW"" RETURNING C.
           INVOKE C ""ADDONE"" USING X RETURNING R.
           STOP RUN.
       END PROGRAM OOMP.
       IDENTIFICATION DIVISION.
       CLASS-ID. CALC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ADDONE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-IN PIC 9(4).
       01 LK-OUT PIC 9(4).
       PROCEDURE DIVISION USING LK-IN RETURNING LK-OUT.
       MAIN.
           COMPUTE LK-OUT = LK-IN + 1.
       END METHOD ADDONE.
       METHOD-ID. TRIPLEV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-T PIC 9(4).
       01 LK-TR PIC 9(4).
       PROCEDURE DIVISION USING LK-T RETURNING LK-TR.
       MAIN.
           COMPUTE LK-TR = LK-T * 3.
       END METHOD TRIPLEV.
       END OBJECT.
       END CLASS CALC.
";
        var (ok, _, stderr) = CompileAndRun(src, DialectMode.Cobol2002);
        Assert.False(ok);
        Assert.Contains("COBOL0117", stderr);
    }
}
