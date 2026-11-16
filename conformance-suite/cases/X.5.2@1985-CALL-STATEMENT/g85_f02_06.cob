      *>--- conformance-case
      *> id: g85_f02_06
      *> rules: [1985-GR-X.5.2.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR8 - G85F0206 opens the EXTERNAL file and reads
      *> record 1; the called G85F02XR reads through the same external
      *> connector (record 2), its first entry and its exit leave the
      *> file open and positioned, so the caller reads record 3 and then
      *> meets end of file. A reset or lost position fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0206.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85F0206.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XR PIC X(4).
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT XF.
           MOVE "REC1" TO XR. WRITE XR.
           MOVE "REC2" TO XR. WRITE XR.
           MOVE "REC3" TO XR. WRITE XR.
           CLOSE XF.
           OPEN INPUT XF.
           READ XF AT END MOVE "EOF " TO XR.
           DISPLAY "MAIN " XR.
           CALL "G85F02XR".
           READ XF AT END MOVE "EOF " TO XR.
           DISPLAY "MAIN " XR.
           READ XF AT END MOVE "EOF " TO XR.
           DISPLAY "MAIN " XR.
           CLOSE XF.
           STOP RUN.
       END PROGRAM G85F0206.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02XR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85F0206.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XR PIC X(4).
       PROCEDURE DIVISION.
       P-XR.
           READ XF AT END MOVE "EOF " TO XR.
           DISPLAY "SUB " XR.
           EXIT PROGRAM.
       END PROGRAM G85F02XR.
