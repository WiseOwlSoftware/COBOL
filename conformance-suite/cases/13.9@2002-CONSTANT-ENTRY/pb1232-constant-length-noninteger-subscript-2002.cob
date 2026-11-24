*>--- conformance-case
*> id: pb1232-constant-length-noninteger-subscript-2002
*> rules: [2002-SR-13.9.2-3]
*> source-format: free
*> portable: unverified
*>---
      *> The 2002 twin of 13.10-Constant/pb1232-constant-length-noninteger-subscript (kb/Work PB1232, PB5709). The
      *> 2002 constant entry has no literal-subscript rule (13.9.2 SR3 allows only qualification), so the subscript is an
      *> ordinary one (8.4.1.2.2) and is read by the one subscript reading; the non-integer constant KD (1.5) is no
      *> integer subscript there either, and the entry is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1232NEG2002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KD CONSTANT AS 1.5.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 K CONSTANT AS LENGTH OF E (KD).
       PROCEDURE DIVISION.
           STOP RUN.
