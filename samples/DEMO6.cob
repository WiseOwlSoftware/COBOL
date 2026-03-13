       IDENTIFICATION DIVISION.
       PROGRAM-ID. DEMO6.
       *> Phase 6 Demo: Production Quality

       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-RESULT       PIC 9(5) VALUE 0.
       01 WS-NAME         PIC X(20) VALUE "cobolsharp".

       PROCEDURE DIVISION.
           DISPLAY "=== Phase 6 Demo: Production Quality ===".
           DISPLAY " ".

           DISPLAY "1. Diagnostics with real locations:".
           DISPLAY "   Errors now show file(line,col)".
           DISPLAY "   with Did-you-mean suggestions".

           DISPLAY "2. Portable PDB debugging:".
           DISPLAY "   .pdb files emitted alongside .dll".
           DISPLAY "   Step through COBOL in VS/VS Code".

           DISPLAY "3. NuGet packaging:".
           DISPLAY "   dotnet tool install -g CobolSharp".

           DISPLAY "4. Intrinsic functions work end-to-end:".
           COMPUTE WS-RESULT = FUNCTION SQRT(256).
           DISPLAY "   SQRT(256) = " WS-RESULT.
           DISPLAY "   UPPER-CASE: "
               FUNCTION UPPER-CASE(WS-NAME).

           DISPLAY " ".
           DISPLAY "=== Phase 6 Demo Complete ===".
           DISPLAY "=== ALL PHASES COMPLETE ===".
           STOP RUN.
