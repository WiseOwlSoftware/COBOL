*> reject-at: 2002 2014 2023
      *> The NATIONAL arm of the same rule (ISO 12.3.7.3 SR14 a, which is stated once for both classes):
      *> N"B" appears in the ALSO group and again as the next operand. The national arm carried the identical
      *> `// SR14a duplicate - first wins` deferral, so BOTH arms accepted this (kb/Work PB770). One builder
      *> now serves both, and this golden is what proves the shared rule reaches the national side too.
     
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETNATIONA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET NDUP FOR NATIONAL IS N"A" ALSO N"B", N"B".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
