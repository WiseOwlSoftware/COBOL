      *> kb/Work PB263 — THE THIRD ARM of the same literal-rendering dispatch: INVOKE's numeric-literal
      *> argument path (OoEmitter), which renders through the same EmitText.UnscaledLit helper the two CALL
      *> arms use. PB264 recorded it as "not yet measured"; it was measured and it was broken the same way.
      *>
      *> ISO 8.3.3.3.3 rule 5 makes 1.2345678901234567890123E+3 exactly 1234.5678901234567890123, the same
      *> value as the fixed-point spelling below it, and ISO 14.8.2.3.3 rule 2 a) transfers a BY CONTENT
      *> argument to a numeric formal "according to the rules of the COMPUTE statement" — the formal
      *> PIC S9(5)V9(20) holds 4 integer and 19 fraction digits exactly, so nothing here rounds.
      *>
      *> WHAT USED TO HAPPEN: the E-form rows emitted a C# double into an Int128-typed store and the generated
      *> code did not compile — "error CS1503: cannot convert from 'double' to 'System.Int128'", a raw Roslyn
      *> failure on conforming source with no COBOL diagnostic. The fixed-point rows were already correct,
      *> which is why the two are pinned side by side here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB263IV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB263CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB263CL.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB263CL "NEW" RETURNING OBJ.
           INVOKE OBJ "TAKE" USING BY CONTENT 1234.5678901234567890123.
           INVOKE OBJ "TAKE" USING BY CONTENT 1.2345678901234567890123E+3.
           INVOKE OBJ "TAKE" USING BY CONTENT 1.5E+3.
           STOP RUN.
       END PROGRAM PB263IV.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB263CL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC S9(5)V9(20).
       PROCEDURE DIVISION USING LK.
       T1.
           DISPLAY "I=" LK.
       END METHOD TAKE.
       END OBJECT.
       END CLASS PB263CL.
