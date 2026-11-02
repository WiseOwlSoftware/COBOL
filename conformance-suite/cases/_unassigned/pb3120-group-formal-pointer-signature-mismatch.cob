      *>--- conformance-case
      *> id: pb3120-group-formal-pointer-signature-mismatch
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3120 - the converse of 2002/pb3120_group_formal_
      *> pointer_type: a strongly-typed group formal's pointer member is
      *> compared by the prototype its TO phrase names (kb/Work PB4107;
      *> docs/CONFORMANCE.md D-RP1; 8.5.3.1, cite.py --check 8.5.3.1 ->
      *> OK 8.5.3.1). QA and QC are two names (and two signatures: a
      *> PIC 9(4) and a PIC X(4)), so PX's prototype (group member
      *> restricted TO QA) and its definition (TO QC) do not have "the
      *> same" signature, and 10.6.2 SR2 (OK 10.6.2 2)) refuses the
      *> pair (COBOL1513). Two names of ONE signature are refused too:
      *> pb4107-group-formal-pointer-two-names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM QA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QC IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC X(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM QC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE T1.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3120N.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB3120N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO QC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE T2.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           GOBACK.
       END PROGRAM PX.
