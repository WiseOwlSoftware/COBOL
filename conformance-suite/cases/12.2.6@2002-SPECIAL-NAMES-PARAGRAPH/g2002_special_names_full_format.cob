      *>--- conformance-case
      *> id: g2002_special_names_full_format
      *> rules: [2002-FMT-12.2.6.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.1 - the 2002 general format of the SPECIAL-NAMES
      *> paragraph, written with its clauses in the order the 2002 format
      *> prints them: switch-name entries (ON and OFF STATUS), a
      *> feature-name entry, a device-name entry, alphabet-name-clauses
      *> (STANDARD-1, a literal-phrase with THRU and ALSO, FOR NATIONAL
      *> UTF-16, NATIVE, LOCALE), symbolic-characters-clauses (FOR
      *> ALPHANUMERIC ... IN, plural ARE), LOCALE, ORDER TABLE, CLASS
      *> (FOR ALPHANUMERIC, FOR NATIONAL, integer literals IN
      *> alphabet-name-4), CURRENCY SIGN ... WITH PICTURE SYMBOL,
      *> DECIMAL-POINT IS COMMA, and one closing period. CURSOR and CRT
      *> STATUS are left out: they depend on the processor's screen
      *> functionality (§2002:B.3 item 1).
      *> Each line can fail: the program must compile with every clause
      *> in that order, and each DISPLAY shows a clause took effect.
      *> AMT: currency string EUR (literal-7) under picture symbol U
      *> (literal-8) as a fixed insertion character, decimal point
      *> comma: 12,5 edits to EUR012,50 (§2002:12.2.6.3 GR13 and GR14).
      *> STAR: position 43 of STANDARD-1 (ISO/IEC 646) is the asterisk.
      *> DIGX: positions 49 through 58 of STANDARD-1 are 0 through 9, so
      *> "5" is in the class and "A" is not. HEXD, NATD: literal ranges.
      *> SW1: exactly one of its ON and OFF condition-names is true,
      *> whichever status the implementor gives the switch.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN08.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF
           SWITCH-2 OFF STATUS IS SW2-OFF
           C01 IS TOP-PAGE
           CONSOLE IS CONS
           ALPHABET STD1 FOR ALPHANUMERIC IS STANDARD-1
           ALPHABET MYSEQ IS "Z" THRU "A" "0" ALSO "1"
           ALPHABET NAT16 FOR NATIONAL IS UTF-16
           ALPHABET NATIV IS NATIVE
           ALPHABET LOCAL IS LOCALE LOC1
           SYMBOLIC CHARACTERS FOR ALPHANUMERIC BEL IS 8 IN STD1
           SYMBOLIC CHARACTERS STAR ARE 43 IN STD1
           LOCALE LOC1 IS "en_US"
           ORDER TABLE ORD1 IS "ISO14651_2001_TABLE1"
           CLASS HEXD FOR ALPHANUMERIC IS "0" THRU "9" "A" THRU "F"
           CLASS NATD FOR NATIONAL IS N"0" THRU N"9"
           CLASS DIGX IS 49 THRU 58 IN STD1
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "U"
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AMT  PIC U999,99.
       01  H1   PIC X VALUE "C".
       01  N1   PIC N VALUE N"7".
       01  D1   PIC X VALUE "5".
       01  D2   PIC X VALUE "A".
       01  S1   PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE 12,5 TO AMT.
           DISPLAY "AMT [" AMT "]".
           IF H1 IS HEXD DISPLAY "HEXD YES" ELSE DISPLAY "HEXD NO".
           IF N1 IS NATD DISPLAY "NATD YES" ELSE DISPLAY "NATD NO".
           IF D1 IS DIGX DISPLAY "DIGX 5 YES" ELSE DISPLAY "DIGX 5 NO".
           IF D2 IS DIGX DISPLAY "DIGX A YES" ELSE DISPLAY "DIGX A NO".
           MOVE STAR TO S1.
           DISPLAY "STAR [" S1 "]".
           IF (SW1-ON AND NOT SW1-OFF) OR (SW1-OFF AND NOT SW1-ON)
               DISPLAY "SW1 ONE-OF"
           ELSE
               DISPLAY "SW1 BOTH-OR-NEITHER".
           DISPLAY "DONE" UPON CONS.
           STOP RUN.
