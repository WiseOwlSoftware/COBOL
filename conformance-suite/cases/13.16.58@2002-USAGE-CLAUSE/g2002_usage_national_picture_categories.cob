      *>--- conformance-case
      *> id: g2002_usage_national_picture_categories
      *> rules: [2002-SR-13.16.58.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR11 - an elementary item whose description
      *> contains, or that is subordinate to a group whose description
      *> contains, USAGE NATIONAL "shall be declared with a picture
      *> character-string that describes a boolean, national,
      *> national-edited, numeric, or numeric-edited data item". This
      *> program writes all five categories with USAGE NATIONAL written
      *> on the item and, under groups WG and CG, inherited from the
      *> group - in working storage and in the record of an output CD,
      *> whose record description entries are ordinary data description
      *> entries (§2002:A.1.2.2 SR9: they implicitly redefine the CD's
      *> record area; CR is its only, so first, record and may carry
      *> VALUE clauses). The refused categories (alphabetic,
      *> alphanumeric, alphanumeric-edited) are the g2002-usage-national-
      *> and g2002-cd-usage-national- refusal cases beside this one.
      *> Expected values:
      *>  B: PIC 1(4) VALUE B"1010" shows 1010; its LENGTH is 4 boolean
      *>     positions (§2002:15.39.3 RV1).
      *>  N: PIC N(3) VALUE N"ABC" shows ABC, LENGTH 3 national
      *>     character positions (§2002:15.39.3 RV2).
      *>  E: PIC N(2)BN receives N"PQR": the B is an insertion space, so
      *>     PQ R, LENGTH 4.
      *>  9: PIC 9(3) VALUE 123 shows 123, LENGTH 3 (RV2).
      *>  Z: PIC ZZ9 receives 7: two suppressed zeros, so "  7".
      *>  G: inherited - GA PIC 9(2) VALUE 45, GN PIC N(2) VALUE N"XY",
      *>     GB PIC 1(2) VALUE B"01", GZ PIC Z9 receives 6 (" 6").
      *> The program fails if any of the five categories, written or
      *> inherited, is refused or loses its value in either section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L   PIC 99.
       01  WB  PIC 1(4) USAGE NATIONAL VALUE B"1010".
       01  WN  PIC N(3) USAGE NATIONAL VALUE N"ABC".
       01  WE  PIC N(2)BN USAGE NATIONAL.
       01  W9  PIC 9(3) USAGE NATIONAL VALUE 123.
       01  WZ  PIC ZZ9 USAGE NATIONAL.
       01  WG  USAGE NATIONAL.
           05 WGA PIC 9(2) VALUE 45.
           05 WGN PIC N(2) VALUE N"XY".
           05 WGB PIC 1(2) VALUE B"01".
           05 WGZ PIC Z9.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CB  PIC 1(4) USAGE NATIONAL VALUE B"1010".
           05 CN  PIC N(3) USAGE NATIONAL VALUE N"ABC".
           05 CE  PIC N(2)BN USAGE NATIONAL.
           05 C9  PIC 9(3) USAGE NATIONAL VALUE 123.
           05 CZ  PIC ZZ9 USAGE NATIONAL.
           05 CG  USAGE NATIONAL.
              10 CGA PIC 9(2) VALUE 45.
              10 CGN PIC N(2) VALUE N"XY".
              10 CGB PIC 1(2) VALUE B"01".
              10 CGZ PIC Z9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"PQR" TO WE
           MOVE 7 TO WZ
           MOVE 6 TO WGZ
           MOVE FUNCTION LENGTH(WB) TO L
           DISPLAY "WS B " WB " " L
           MOVE FUNCTION LENGTH(WN) TO L
           DISPLAY "WS N " WN " " L
           MOVE FUNCTION LENGTH(WE) TO L
           DISPLAY "WS E " WE " " L
           MOVE FUNCTION LENGTH(W9) TO L
           DISPLAY "WS 9 " W9 " " L
           DISPLAY "WS Z " WZ
           DISPLAY "WS G " WGA " " WGN " " WGB " " WGZ
           MOVE N"PQR" TO CE
           MOVE 7 TO CZ
           MOVE 6 TO CGZ
           MOVE FUNCTION LENGTH(CB) TO L
           DISPLAY "CD B " CB " " L
           MOVE FUNCTION LENGTH(CN) TO L
           DISPLAY "CD N " CN " " L
           MOVE FUNCTION LENGTH(CE) TO L
           DISPLAY "CD E " CE " " L
           MOVE FUNCTION LENGTH(C9) TO L
           DISPLAY "CD 9 " C9 " " L
           DISPLAY "CD Z " CZ
           DISPLAY "CD G " CGA " " CGN " " CGB " " CGZ
           STOP RUN.
