      *>--- conformance-case
      *> id: g85_f14_04
      *> rules: [1985-SR-VI.6.27.3-6, 1985-GR-VI.6.27.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.3 SR6 - DELIMITER IN and COUNT IN 'may be
      *> specified only if the DELIMITED BY phrase is specified': both
      *> statements below take that permitted form and must translate
      *> and run (a refusal of either fails the case).
      *> §1985:VI.6.27.4 GR5 - the COUNT item holds the number of
      *> characters of identifier-1 isolated for the move to
      *> identifier-4, and 'does not include a count of the delimiter
      *> character(s)' (§1985:VI.6.27.4 GR13e: the number examined,
      *> excluding the delimiter). U1 on AB,CDE--FGHI: R1 receives A
      *> (MOVE truncation) but counts 02, the characters isolated, not
      *> moved; R2 CDE counts 03 although its delimiter -- is two
      *> characters; R3 FGHI ends at the end of SRC, counts 04, and its
      *> DELIMITER IN item is space filled (§1985:VI.6.27.4 GR13d). U2
      *> on XY,,Z: the empty field between two delimiters counts 00
      *> and R2 is space filled (§1985:VI.6.27.4 GR9). A count that
      *> includes the delimiter (03, 05, ...) or counts the moved
      *> characters (01 for R1) fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1404.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC   PIC X(12) VALUE "AB,CDE--FGHI".
       01  SRC2  PIC X(5)  VALUE "XY,,Z".
       01  R1    PIC X.
       01  R2    PIC X(5).
       01  R3    PIC X(6).
       01  D1    PIC XX.
       01  D2    PIC XX.
       01  D3    PIC XX.
       01  C1    PIC 99.
       01  C2    PIC 99.
       01  C3    PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "*" TO D3.
           UNSTRING SRC DELIMITED BY "," OR "--"
               INTO R1 DELIMITER IN D1 COUNT IN C1
                    R2 DELIMITER IN D2 COUNT IN C2
                    R3 DELIMITER IN D3 COUNT IN C3
           END-UNSTRING.
           DISPLAY "U1 [" R1 "] " C1 " [" D1 "]".
           DISPLAY "U1 [" R2 "] " C2 " [" D2 "]".
           DISPLAY "U1 [" R3 "] " C3 " [" D3 "]".
           MOVE ALL "*" TO R2.
           UNSTRING SRC2 DELIMITED BY ","
               INTO R1 COUNT IN C1
                    R2 COUNT IN C2
                    R3 COUNT IN C3
           END-UNSTRING.
           DISPLAY "U2 [" R1 "] " C1.
           DISPLAY "U2 [" R2 "] " C2.
           DISPLAY "U2 [" R3 "] " C3.
           STOP RUN.
