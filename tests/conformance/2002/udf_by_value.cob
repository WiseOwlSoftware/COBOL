      *> ISO 14.2.2 using-phrase / 14.2.3 GR4+GR10 / 8.4.3.2.4 GR5c - BY VALUE formal parameters in
      *> the procedure division header, on BOTH activation paths (a user-defined function reference
      *> and a CALL target - one shared ABI): the activated element operates on a VALUE COPY (a
      *> detached cell conformed to the formal, 14.2.3 GR10), so a store into the formal NEVER
      *> reaches the caller's argument; a BY REFERENCE formal in the same header (GR4 transitivity)
      *> DOES write back. A literal argument to a BY VALUE formal is 8.4.3.2.3 SR10-legal (numeric).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UBYVAL-P10UV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SCALEV-P10UV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) VALUE 7.
       01 WS-B PIC 9(4) VALUE 100.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *>   an identifier argument to the BY VALUE formal: mutated inside, caller unchanged
           COMPUTE WS-R = FUNCTION SCALEV-P10UV(WS-A, WS-B).
           DISPLAY "R1=" WS-R.
           DISPLAY "A1=" WS-A.
           DISPLAY "B1=" WS-B.
      *>   a literal argument to the BY VALUE formal (SR10 - class numeric)
           COMPUTE WS-R = FUNCTION SCALEV-P10UV(3, WS-B).
           DISPLAY "R2=" WS-R.
           DISPLAY "B2=" WS-B.
      *>   the CALL leg: the same header shape on a called subprogram
           MOVE 7 TO WS-A.
           MOVE 100 TO WS-B.
           CALL "SUBV-P10UV" USING WS-B BY VALUE WS-A.
           DISPLAY "A3=" WS-A.
           DISPLAY "B3=" WS-B.
           STOP RUN.
       END PROGRAM UBYVAL-P10UV.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. SCALEV-P10UV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-V PIC 9(4).
       01 L-REF PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-V BY REFERENCE L-REF
           RETURNING L-R.
       P.
           COMPUTE L-R = L-V * 2.
      *>   a store into the VALUE copy - must NOT reach the caller (14.2.3 GR10)
           MOVE 9999 TO L-V.
      *>   a store through the REFERENCE formal - MUST reach the caller (14.2.3 GR8)
           ADD 1 TO L-REF.
           GOBACK.
       END FUNCTION SCALEV-P10UV.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBV-P10UV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 S-REF PIC 9(4).
       01 S-V PIC 9(4).
       PROCEDURE DIVISION USING S-REF BY VALUE S-V.
       P.
           MOVE 8888 TO S-V.
           ADD 5 TO S-REF.
           GOBACK.
       END PROGRAM SUBV-P10UV.
