      *>--- conformance-case
      *> id: g85_bf08_01
      *> rules: [1985-GR-VI.4.5.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR12 - "The clause DECIMAL-POINT IS COMMA means
      *> that the functions of comma and period are exchanged in the
      *> character-string of the PICTURE clause and in numeric literals."
      *> Both halves are pinned; every expected line is derived from the
      *> 1985 text, not measured.
      *> NUMERIC LITERALS: 1234,56 / 0,44 / 1234,99 / 2,5 / 7,5 carry a
      *> decimal comma. Each value of the 9(4)V99 item is shown through
      *> WS-F PIC 9(4),99, whose comma is the actual decimal point
      *> (§1985:VI.5.9.5 GR5 under the exchange), because the 1985
      *> DISPLAY leaves the conversion of an unedited item to the
      *> implementor (§1985:VI.6.10.4 GR1): N1=[1234,56] (MOVE 1234,56),
      *> N2=[1235,00] (ADD 0,44), GT (1235.00 > 1234,99), N3=[0007,50]
      *> (2,5 * 3). A compiler that read the comma as a separator or
      *> ignored it would hold 123456, 44 or 25 as integers and fail
      *> each leg.
      *> PICTURE: §1985:VI.5.9.4 GR8d "the rules for the period apply to
      *> the comma and the rules for the comma apply to the period", so
      *> in Z.ZZ9,99 the period is simple insertion (§1985:VI.5.9.5 GR4)
      *> and the comma is the decimal point (§1985:VI.5.9.5 GR5):
      *> E1=[1.235,00]; with 7,5 the period lies inside the suppression
      *> string (§1985:VI.5.9.5 GR8 "Any of the simple insertion
      *> characters embedded in the string of symbols ... are part of the
      *> string") so E2=[    7,50]. In 99.999 the period is NOT a
      *> decimal point, so the item has five integer digits:
      *> MOVE 12345 gives G=[12.345] (without the exchange it would be
      *> two integer digits and 45.000).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0801.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9(4)V99.
       01  WS-E PIC Z.ZZ9,99.
       01  WS-G PIC 99.999.
       01  WS-F PIC 9(4),99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1234,56 TO WS-N.
           MOVE WS-N TO WS-F.
           DISPLAY "N1=[" WS-F "]".
           ADD 0,44 TO WS-N.
           MOVE WS-N TO WS-F.
           DISPLAY "N2=[" WS-F "]".
           IF WS-N > 1234,99
               DISPLAY "GT"
           ELSE
               DISPLAY "NOT-GT".
           MOVE WS-N TO WS-E.
           DISPLAY "E1=[" WS-E "]".
           MOVE 7,5 TO WS-E.
           DISPLAY "E2=[" WS-E "]".
           MOVE 12345 TO WS-G.
           DISPLAY "G=[" WS-G "]".
           COMPUTE WS-N = 2,5 * 3.
           MOVE WS-N TO WS-F.
           DISPLAY "N3=[" WS-F "]".
           STOP RUN.
