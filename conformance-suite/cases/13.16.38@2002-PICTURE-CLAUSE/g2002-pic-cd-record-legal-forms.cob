      *>--- conformance-case
      *> id: g2002-pic-cd-record-legal-forms
      *> rules: [2002-SR-13.16.38.2-16, 2002-SR-13.16.38.2-17, 2002-SR-13.16.38.2-18, 2002-SR-13.16.38.2-26, 2002-SR-13.16.38.2-27, 2002-SR-13.16.38.2-28, 2002-SR-13.16.38.2-29, 2002-SR-13.16.38.2-30, 2002-SR-13.16.38.2-31]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The PICTURE syntax rules of §2002:13.16.38.2 reach a record
      *> description entry written after an OUTPUT CD entry, an
      *> ordinary data description entry there (§2002:A.1.1.2). Each
      *> item below writes a form the rules leave legal; the program
      *> must compile and each item must edit as 13.16.38.4 defines.
      *> F1/F2 §2002:13.16.38.2 SR16 - 'V' alone, '.' alone. F1 is
      *>       shown through WSHOW: DISPLAY's own conversion of a
      *>       numeric item is the implementor's (§2002:14.8.10.3 GR1).
      *> F3/F4 §2002:13.16.38.2 SR17 - 'Z' alone, '*' alone (GR7).
      *> F5    §2002:13.16.38.2 SR18 - ZZ9 BLANK WHEN ZERO, no S or *;
      *>       a zero store gives all spaces (§2002:13.16.8.3 GR1).
      *> F6    §2002:13.16.38.2 SR26 - $$9.99, the float left of the
      *>       point; 1.5 edits to " $1.50" (§2002:13.16.38.4 GR6).
      *> F7    §2002:13.16.38.2 SR27 - N(3) USAGE NATIONAL, 3 national
      *>       character positions (§2002:15.39.3 RV2).
      *> L1    §2002:13.16.38.2 SR28 and SR31 - ZZ9, a 'Z' and a '9',
      *>       LOCALE IS FR declared in SPECIAL-NAMES.
      *> L2    §2002:13.16.38.2 SR29 and SR30 - +$Z9.99, '+', '$' and
      *>       '.' once each, '+' and '$' left of the point.
      *> L3    §2002:13.16.38.4 GR10 - BLANK WHEN ZERO over locale
      *>       editing: a zero store gives 8 spaces.
      *> Locale-edited text depends on the runtime locale (GR9, GR12),
      *> so L1/L2 are pinned by their size (§2002:13.16.38.3 GR16) and
      *> by de-editing them back to a number (§2002:14.8.24.3 GR3,
      *> same locale both ways, §2002:13.16.38.4 GR11).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2P3OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WBACK  PIC S99V99.
       01  WSHOW  PIC -99.99.
       01  WLEN   PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 F1 PIC 9V99.
           05 F2 PIC 9.99.
           05 F3 PIC ZZ9.
           05 F4 PIC **9.
           05 F5 PIC ZZ9 BLANK WHEN ZERO.
           05 F6 PIC $$9.99.
           05 F7 PIC N(3) USAGE NATIONAL.
           05 L1 PIC ZZ9 LOCALE IS FR SIZE IS 6.
           05 L2 PIC +$Z9.99 LOCALE IS FR SIZE IS 12.
           05 L3 PIC Z9.99 LOCALE IS FR SIZE IS 8 BLANK WHEN ZERO.
       PROCEDURE DIVISION.
           MOVE 1.5 TO F1
           MOVE F1 TO WSHOW
           DISPLAY "F1=" WSHOW
           MOVE 1.5 TO F2
           DISPLAY "F2=" F2
           MOVE 5 TO F3
           DISPLAY "F3=[" F3 "]"
           MOVE 5 TO F4
           DISPLAY "F4=[" F4 "]"
           MOVE 0 TO F5
           DISPLAY "F5=[" F5 "]"
           MOVE 1.5 TO F6
           DISPLAY "F6=[" F6 "]"
           MOVE "ABC" TO F7
           MOVE FUNCTION LENGTH(F7) TO WLEN
           DISPLAY "F7LEN=" WLEN
           IF F7 = N"ABC" DISPLAY "F7=ABC" ELSE DISPLAY "F7=BAD"
           END-IF
           MOVE 7 TO L1
           MOVE L1 TO WBACK
           MOVE WBACK TO WSHOW
           DISPLAY "L1BACK=" WSHOW
           MOVE FUNCTION LENGTH(L1) TO WLEN
           DISPLAY "L1LEN=" WLEN
           MOVE -12.5 TO L2
           MOVE L2 TO WBACK
           MOVE WBACK TO WSHOW
           DISPLAY "L2BACK=" WSHOW
           MOVE FUNCTION LENGTH(L2) TO WLEN
           DISPLAY "L2LEN=" WLEN
           MOVE 0 TO L3
           DISPLAY "L3=[" L3 "]"
           STOP RUN.
