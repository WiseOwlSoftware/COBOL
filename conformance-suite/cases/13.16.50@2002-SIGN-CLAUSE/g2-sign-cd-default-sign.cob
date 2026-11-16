      *>--- conformance-case
      *> id: g2-sign-cd-default-sign
      *> rules: [2002-GR-13.16.50.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.50.3 GR4 - "A numeric item whose picture
      *> character-string contains the symbol 'S', and to which no SIGN
      *> clause applies, has an operational sign. ... The implementor
      *> shall specify the position and representation of the
      *> operational sign." N1 is such an item in a record under a CD
      *> (§2002:A.1.1.2 "A record description consists of a set of data
      *> description entries"); no SIGN clause is written on N1 or on
      *> CDREC. The implementor's specification is
      *> docs/conformance/7/DOC-A.1-177.md: the sign is FUSED onto the
      *> TRAILING digit position and, under the default
      *> --sign-encoding ibm, positive 0-9 are {ABCDEFGHI and negative
      *> 0-9 are }JKLMNOPQR.
      *> DERIVATION.
      *>  LEN 3: the fused sign adds no character position to S9(3).
      *>  -12 => digits 012, trailing 2 negative => "01K"; +12 => "01B";
      *>  -123 => "12L".
      *>  TRL SAME: N1 holds the same bytes as T1 SIGN IS TRAILING (the
      *>  documented position); LDG DIFFER: unlike L1 SIGN IS LEADING.
      *>  "has an operational sign": N1 = -12 moved to E1 PIC -999 shows
      *>  "-012" (the editing sign '-' prints '-' for a negative value,
      *>  §2002:13.16.38.4 GR5 and Table 9; alignment and zero fill
      *>  §2002:14.8.24.3 GR4b), and
      *>  ADD 1 to -12 gives -11 => "01J".
      *> Each leg fails if the sign is dropped, made separate, placed
      *> leading, or encoded with another table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SGN06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1 PIC -999.
       01  K1 PIC 9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 N1 PIC S9(3).
           05 T1 PIC S9(3) SIGN IS TRAILING.
           05 L1 PIC S9(3) SIGN IS LEADING.
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(N1) TO K1
           DISPLAY "LEN " K1
           MOVE -12 TO N1 T1 L1
           DISPLAY "NEG [" CDREC(1:3) "]"
           IF CDREC(1:3) = CDREC(4:3)
              DISPLAY "TRL SAME"
           ELSE
              DISPLAY "TRL DIFFER"
           END-IF
           IF CDREC(1:3) = CDREC(7:3)
              DISPLAY "LDG SAME"
           ELSE
              DISPLAY "LDG DIFFER"
           END-IF
           MOVE N1 TO E1
           DISPLAY "VAL [" E1 "]"
           ADD 1 TO N1
           DISPLAY "ADD [" CDREC(1:3) "]"
           MOVE 12 TO N1
           DISPLAY "POS [" CDREC(1:3) "]"
           MOVE -123 TO N1
           DISPLAY "NEG3 [" CDREC(1:3) "]"
           STOP RUN.
