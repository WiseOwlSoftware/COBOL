      *>--- conformance-case
      *> id: g2-constant-cd-as-if-written
      *> rules: [2002-GR-13.9.3-1, 2002-SR-13.9.2-9, 2002-SR-13.9.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Constant entries written in the COMMUNICATION SECTION after a
      *> CD entry, where the 2002 data division format places them
      *> (§2002:13.1.1 general format: COMMUNICATION SECTION.
      *> {communication-description-entry
      *>  [constant-entry | record-description-entry] ...} ...).
      *> §2002:13.9.3 GR1 - "the effect of specifying constant-name-1 in
      *> other than this entry is as if literal-1 or the text
      *> represented by compilation-variable-name-1 were written where
      *> constant-name-1 is written". K1 (AS 7) is written as a MOVE
      *> sending operand (W-N PIC 99 receives 07), as a relation
      *> object (W-7 = K1, where
      *> W-7 PIC 99 VALUE 7, is true) and as a PICTURE repetition count
      *> (CR-A PIC X(K1) is X(7), §2002:13.9.2 SR2, so
      *> FUNCTION LENGTH(CR-A) is 7); K6 (AS "HELLO") moves HELLO; K4
      *> (FROM CV, >>DEFINE CV AS "ABC") moves ABC, and W-ABC (VALUE
      *> "ABC") = K4 is true. Each leg fails if the constant under the
      *> CD is not replaced by its literal (wrong value, wrong length,
      *> or an undefined-name refusal).
      *> §2002:13.9.2 SR9 - "If constant-name-1 duplicates another
      *> constant-name, the specification ... shall be the same": KD is
      *> declared AS 4 in working-storage and again AS 4 under the CD;
      *> the same specification is accepted and KD moves 04 (the leg
      *> fails if a same-specification duplicate is refused).
      *> §2002:13.9.2 SR11 - "Data-name-1 and data-name-2, if defined in
      *> the report section, shall reference elementary report items":
      *> KE (LENGTH OF EL) under the CD names the elementary report item
      *> EL PIC X(5), so it is accepted and KE is 5 (§2002:13.9.3 GR6);
      *> the leg fails if the elementary report item is refused or KE is
      *> any other value.
      *> The communication facility is obsolete at 2002; its obsolete
      *> warning (COBOL0903) does not fail an accepted case.
       >>DEFINE CV AS "ABC"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNP01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "G2CNP01.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  W-N PIC 99 VALUE 0.
       01  W-X PIC X(5) VALUE SPACES.
       01  W-C PIC X(3) VALUE SPACES.
       01  W-7 PIC 99 VALUE 7.
       01  W-ABC PIC X(3) VALUE "ABC".
       01  KD CONSTANT AS 4.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  K1 CONSTANT AS 7.
       01  K6 CONSTANT AS "HELLO".
       01  K4 CONSTANT FROM CV.
       01  KD CONSTANT AS 4.
       01  KE CONSTANT AS LENGTH OF EL.
       01  CDREC.
           05 CR-A PIC X(K1).
           05 CR-B PIC X(4).
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 10 LINES.
       01  DL TYPE DE LINE PLUS 1.
           03  EL COLUMN 1 PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
           MOVE K1 TO W-N
           DISPLAY "K1=" W-N
           IF W-7 = K1
               DISPLAY "K1-EQ-7"
           ELSE
               DISPLAY "K1-NE-7"
           END-IF
           MOVE FUNCTION LENGTH(CR-A) TO W-N
           DISPLAY "LEN-CR-A=" W-N
           MOVE K6 TO W-X
           DISPLAY "K6=" W-X
           MOVE K4 TO W-C
           DISPLAY "K4=" W-C
           IF W-ABC = K4
               DISPLAY "K4-EQ-ABC"
           ELSE
               DISPLAY "K4-NE-ABC"
           END-IF
           MOVE KD TO W-N
           DISPLAY "KD=" W-N
           MOVE KE TO W-N
           DISPLAY "KE=" W-N
           STOP RUN.
