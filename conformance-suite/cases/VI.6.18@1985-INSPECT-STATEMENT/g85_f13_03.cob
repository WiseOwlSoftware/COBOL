      *>--- conformance-case
      *> id: g85_f13_03
      *> rules: [1985-GR-VI.6.18.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR9 - in Format 1 (TALLYING) identifier-1 is a
      *> sending data item. Two consequences are observable:
      *> (a) TALLYING never changes identifier-1 (T1, T4 with the
      *>     unchanged-content lines);
      *> (b) identifier-1 may be a function-identifier: §1985:IV.4.3.8.3.3
      *>     SR3 admits an alphanumeric function-identifier anywhere an
      *>     identifier is permitted except as a receiving operand, and
      *>     GR9 makes identifier-1 of Format 1 a sending one (T2, T3;
      *>     §1985:VI.6.18.4 GR5 names the function-identifier case).
      *> Why each leg can fail: a refusal of T2 or T3 at 85 rejects
      *> legal source; a count other than the derived one, or a changed
      *> W1, W2 or S1, contradicts the rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1303.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1  PIC X(8) VALUE "ABRACADA".
       01  W2  PIC X(6) VALUE "banana".
       01  S1  PIC S9(4) VALUE -1201.
       01  CNT PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           MOVE 0 TO CNT.
           INSPECT W1 TALLYING CNT FOR ALL "A".
           DISPLAY "T1=" CNT " W1=" W1.
           MOVE 0 TO CNT.
           INSPECT FUNCTION UPPER-CASE (W2) TALLYING CNT FOR ALL "A".
           DISPLAY "T2=" CNT " W2=" W2.
           MOVE 0 TO CNT.
           INSPECT FUNCTION REVERSE (W1) TALLYING CNT FOR LEADING "A".
           DISPLAY "T3=" CNT " W1=" W1.
           MOVE 0 TO CNT.
           INSPECT S1 TALLYING CNT FOR ALL "1".
           DISPLAY "T4=" CNT.
           IF S1 = -1201
               DISPLAY "S1=UNCHANGED"
           ELSE
               DISPLAY "S1=CHANGED".
           STOP RUN.
