*> reject-at: 2023
*> VCR 30 (ISO §7.3.23.2): >>REF-MOD-ZERO-LENGTH takes exactly ON or OFF — any other
*> operand is COBOLNET1576 (renumbered from a bare-literal 1573 that collided with the
*> external-file-status-consistency descriptor; P13 plan-vs-spec review finding C1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RMZLMAL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
       MAIN.
      *> the malformed operand: neither ON nor OFF
       >>REF-MOD-ZERO-LENGTH MAYBE
           DISPLAY W(2:3)
           STOP RUN.
