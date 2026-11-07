*>--- conformance-case
*> id: file-record-pointer-refused-85
*> rules: [GR-14.9.30.4-13, GR-13.18.60.4-23]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4838 - file-record-pointer-keyed-round-trip is introduced by COBOL-2002: a file record holding a data-pointer
      *> is a strongly-typed group (13.18.60.3 SR14), and the TYPEDEF clause (13.18.58) does not exist in X3.23-1985, so the
      *> record is refused below 2002 on the type declaration's own introduction gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILERECPTRREFUSED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO "filerecptrrefused.dat"
              ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD TF.
       01 FREC TYPE T-REC.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 TKEY PIC 9(2).
          05 TPTR USAGE POINTER.
       PROCEDURE DIVISION.
           OPEN OUTPUT TF
           MOVE 1 TO TKEY OF FREC
           WRITE FREC
           CLOSE TF
           STOP RUN.
