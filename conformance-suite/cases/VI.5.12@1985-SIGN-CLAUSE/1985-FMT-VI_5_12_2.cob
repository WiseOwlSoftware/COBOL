      *>--- conformance-case
      *> id: 1985-FMT-VI_5_12_2
      *> rules: [1985-FMT-VI.5.12.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.12.2 (THE SIGN CLAUSE), the general format
      *>   [ SIGN IS ] { LEADING | TRAILING } [ SEPARATE CHARACTER ]
      *> grants SIGN and IS as optional words, either position, and the
      *> SEPARATE phrase with or without CHARACTER. Each item below writes
      *> one of those forms; the program must translate and run at 85
      *> (kb/Work PB5740: a general format is witnessed by an accepted
      *> case, never by refusals alone). Expected values:
      *> - no SEPARATE (N1-N3): the item keeps its value through
      *>   arithmetic (§1985:VI.5.12.4 GR7, conversion is automatic); the
      *>   sign's representation is the implementor's (GR5 c), so only
      *>   the value is compared.
      *> - SEPARATE (N4-N7): §1985:VI.5.12.4 GR6 a and c, "The operational
      *>   signs for positive and negative are the standard data format
      *>   characters" '+' and '-', in the leading or trailing character
      *>   position: -12 is "-012" leading, "012-" trailing; +7 trailing
      *>   is "007+".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIGNFMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  N1 PIC S9(3) LEADING.
       01  G2.
           05  N2 PIC S9(3) SIGN TRAILING.
       01  G3.
           05  N3 PIC S9(3) SIGN IS LEADING.
       01  G4.
           05  N4 PIC S9(3) LEADING SEPARATE.
       01  G5.
           05  N5 PIC S9(3) SIGN TRAILING SEPARATE CHARACTER.
       01  G6.
           05  N6 PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER.
       01  G7.
           05  N7 PIC S9(3) TRAILING SEPARATE.
       01  OK PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           MOVE -12 TO N1 N2 N3.
           ADD 1 TO N1 N2 N3.
           IF N1 = -11 AND N2 = -11 AND N3 = -11
               DISPLAY "OK   EMBEDDED SIGN VALUES"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL EMBEDDED SIGN VALUES"
           END-IF.
           MOVE -12 TO N4 N5 N6.
           MOVE 7 TO N7.
           DISPLAY "N4 [" G4 "]".
           DISPLAY "N5 [" G5 "]".
           DISPLAY "N6 [" G6 "]".
           DISPLAY "N7 [" G7 "]".
           IF G4 = "-012" AND G5 = "012-" AND G6 = "-012"
              AND G7 = "007+"
               DISPLAY "OK   SEPARATE SIGN POSITIONS"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL SEPARATE SIGN POSITIONS"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
