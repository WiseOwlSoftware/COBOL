      *>--- conformance-case
      *> id: pb5932-sr4-linkage-function-pointer-not-referenceable
      *> rules: [SR-13.7.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:13.7.3 SR4 (cite.py --check: OK): a linkage item "may be
      *> referenced within the procedure division of that source element
      *> if, and only if" it meets one of the rule's legs. L-FP is no
      *> USING operand and not BASED, and function-pointer-name-1 of a
      *> function-identifier names a data item (§2023:8.4.3.2.3 SR4,
      *> "Function-pointer-name-1 shall be defined as a function-pointer
      *> data item"), so FUNCTION L-FP (1) is refused with COBOL2746, as
      *> `SET L-FP TO NULL` is. It compiled clean until kb/Work PB5932's
      *> review of step 6b's remainder slice 1.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB5932F1.
       DATA DIVISION.
       LINKAGE SECTION.
       01  A PIC 9.
       01  R PIC 9.
       PROCEDURE DIVISION USING A RETURNING R.
           MOVE A TO R.
           GOBACK.
       END FUNCTION PB5932F1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5932FP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION PB5932F1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC 9.
       LINKAGE SECTION.
       01  L-FP USAGE FUNCTION-POINTER TO PB5932F1.
       01  L-OK PIC X.
       PROCEDURE DIVISION USING L-OK.
           MOVE FUNCTION L-FP (1) TO X.
           GOBACK.
       END PROGRAM PB5932FP.
