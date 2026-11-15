      *>--- conformance-case
      *> id: g2-value-linkage-initialize
      *> rules: [2002-GR-13.16.61.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.3 GR3 - "In the linkage section, VALUE clauses
      *> take effect only
      *> during the execution of an explicit or implicit INITIALIZE
      *> statement." Format 1
      *> (data-item) VALUEs L-A, L-B and a format 2 (table) VALUE L-T
      *> (the rule is
      *> banded FORMATS 1 AND 2) on a linkage record. ON ENTRY shows
      *> the caller's
      *> argument [...999...], not the VALUEs (the "only" half);
      *> INITIALIZE ... ALL
      *> TO VALUE (§2002:14.8.19.3 GR5 c) 1. b. and GR6 a) 3.) gives
      *> ABC, 123 and the
      *> table X Y X ("X" "Y" reused to TO (3), §2002:13.16.61.3 GR12,
      *> GR13):
      *> [ABC123XYX] (the operative half); the caller sees [ABC123XYX]
      *> because the
      *> argument is passed by reference. Every occurrence is keyed, so
      *> no
      *> unspecified occurrence is asserted.
      *> Fails if the VALUEs are applied on entry or not applied by the
      *> INITIALIZE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAL03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-REC.
           05 W-A PIC X(3) VALUE "...".
           05 W-B PIC 9(3) VALUE 999.
           05 W-T PIC X(3) VALUE "...".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "G2VAL03S" USING W-REC
           DISPLAY "CALLER AFTER=[" W-REC "]"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAL03S.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-REC.
           05 L-A PIC X(3) VALUE "ABC".
           05 L-B PIC 9(3) VALUE 123.
           05 L-T PIC X OCCURS 3
              VALUES ARE "X" "Y" FROM (1) TO (3).
       PROCEDURE DIVISION USING L-REC.
       MAIN-S.
           DISPLAY "ON ENTRY=[" L-REC "]"
           INITIALIZE L-REC ALL TO VALUE
           DISPLAY "AFTER INITIALIZE=[" L-REC "]"
           GOBACK.
       END PROGRAM G2VAL03S.
       END PROGRAM G2VAL03.
