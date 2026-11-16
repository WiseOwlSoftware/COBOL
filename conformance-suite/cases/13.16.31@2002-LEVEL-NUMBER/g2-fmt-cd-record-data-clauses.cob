      *>--- conformance-case
      *> id: g2-fmt-cd-record-data-clauses
      *> rules: [2002-FMT-13.16.27.1, 2002-FMT-13.16.30.1,
      *>    2002-FMT-13.16.31.1, 2002-FMT-13.16.36.1,
      *>    2002-FMT-13.16.38.1, 2002-FMT-13.16.42.1,
      *>    2002-FMT-13.16.43.1, 2002-FMT-13.16.47.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The general formats of eight data description clauses written
      *> in a record description entry after an OUTPUT CD entry. At
      *> 2002 such entries are ordinary data description entries: "may
      *> be implicitly redefined by user-specified record description
      *> entries" (§2002:A.1.1.2). The program must compile and every
      *> line must show the clause took effect (the 66-level and the
      *> working-storage tables are the format's other placements).
      *> §2002:13.16.31 / §2002:13.16.31.2 SR2 - level-numbers 01, 05,
      *>   5 (SR3), 10, 66 and 88 head the CD record's entries.
      *> §2002:13.16.30 - JUSTIFIED RIGHT and JUST: "AB" into X(5)
      *>   gives "   AB" (§2002:13.16.30.3 GR2); "WXYZABCD" into X(5)
      *>   JUST gives "ZABCD" (GR1, leftmost truncated).
      *> §2002:13.16.36.1 - format 1 (ASCENDING KEY IS, INDEXED BY) and
      *>   format 2 (TO ... DEPENDING ON, DESCENDING KEY, INDEXED) in
      *>   the CD record and in working-storage; SEARCH ALL finds each
      *>   key; LENGTH of the ODO group follows WN (§2002:15.39.3
      *>   RV4b): 02 then 03.
      *> §2002:13.16.42.1 - 05 R2 REDEFINES R1 (level 1 is barred in
      *>   the communication section, §2002:13.16.42.2 SR4): "1234"
      *>   plus 1 through R2 gives 1235.
      *> §2002:13.16.27.1 - GROUP-USAGE IS BIT and GROUP-USAGE NATIONAL:
      *>   LENGTH 08 boolean positions (§2002:15.39.3 RV1) and 02
      *>   national character positions (RV2).
      *> §2002:13.16.47.1 - SAME AS WT: subordinates WT-A/WT-B exist
      *>   under SA (§2002:13.16.47.3 GR1): QQ07, LENGTH 04.
      *> §2002:13.16.38.1 - format 1 PIC / PICTURE IS; format 2 LOCALE
      *>   SIZE 2 and LOCALE IS FR SIZE IS 6: LENGTH is integer-1
      *>   (§2002:13.16.38.3 GR16), 02 and 06.
      *> §2002:13.16.43.1 - 66 RENAMES ... THRU and THROUGH (GR2, an
      *>   alphanumeric group over the range) and RENAMES alone (GR1).
      *> Locale-edited content is never displayed (locale-dependent).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FMT01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WT.
           05 WT-A PIC X(2).
           05 WT-B PIC 9(2).
       01  WN   PIC 9 VALUE 2.
       01  WLEN PIC 99.
       01  WIX  PIC 9.
       01  WTAB.
           05 WE OCCURS 3 TIMES ASCENDING KEY IS WK INDEXED BY WX.
              10 WK PIC 9.
       01  WODO.
           05 WD OCCURS 1 TO 4 TIMES DEPENDING ON WN
                 DESCENDING WDK INDEXED WY.
              10 WDK PIC 9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  REC1.
           05 J1 PIC X(5) JUSTIFIED RIGHT.
           05 J2 PIC X(5) JUST.
           05 T1 OCCURS 3 TIMES ASCENDING KEY IS T1K INDEXED BY X1.
              10 T1K PIC 9.
           05 R1 PIC X(4).
           05 R2 REDEFINES R1 PIC 9(4).
           05 GB GROUP-USAGE IS BIT.
              10 B1 PIC 1(4).
              10 B2 PIC 1(4).
           05 GN GROUP-USAGE NATIONAL.
              10 N1 PIC N(2).
           05 SA SAME AS WT.
           05 Q1 PIC $9 LOCALE SIZE 2.
           05 Q2 PICTURE IS ZZ9 LOCALE IS FR SIZE IS 6.
           05 P1 PICTURE IS 9(3).
           05 P2 PIC X(3).
           5  C1 PIC 9.
              88 C1-ONE VALUE 1.
           05 T2 OCCURS 1 TO 4 TIMES DEPENDING ON WN
                 DESCENDING KEY T2K INDEXED BY X2.
              10 T2K PIC 9.
       66  RN1 RENAMES J1 THRU J2.
       66  RN2 RENAMES R1.
       66  RN3 RENAMES P1 THROUGH P2.
       PROCEDURE DIVISION.
           MOVE "AB" TO J1
           MOVE "WXYZABCD" TO J2
           DISPLAY "J1=[" J1 "] J2=[" J2 "]"
           DISPLAY "RN1=[" RN1 "]"
           MOVE 1 TO T1K(1) MOVE 2 TO T1K(2) MOVE 3 TO T1K(3)
           SEARCH ALL T1 AT END DISPLAY "T1 NOT FOUND"
               WHEN T1K(X1) = 2
                   SET WIX TO X1
                   DISPLAY "T1 FOUND AT " WIX
           END-SEARCH
           MOVE "1234" TO R1
           ADD 1 TO R2
           DISPLAY "R2=" R2 " RN2=" RN2
           MOVE B"1010" TO B1 MOVE B"0110" TO B2
           IF GB = B"10100110" DISPLAY "GB=10100110"
           ELSE DISPLAY "GB=BAD" END-IF
           MOVE FUNCTION LENGTH(GB) TO WLEN
           DISPLAY "GBLEN=" WLEN
           MOVE N"XY" TO N1
           MOVE FUNCTION LENGTH(GN) TO WLEN
           DISPLAY "GNLEN=" WLEN
           MOVE "QQ" TO WT-A OF SA MOVE 7 TO WT-B OF SA
           DISPLAY "SA=" WT-A OF SA WT-B OF SA
           MOVE FUNCTION LENGTH(SA) TO WLEN
           DISPLAY "SALEN=" WLEN
           MOVE FUNCTION LENGTH(Q1) TO WLEN
           DISPLAY "Q1LEN=" WLEN
           MOVE FUNCTION LENGTH(Q2) TO WLEN
           DISPLAY "Q2LEN=" WLEN
           MOVE 125 TO P1 MOVE "XYZ" TO P2
           DISPLAY "RN3=[" RN3 "]"
           MOVE 1 TO C1
           IF C1-ONE DISPLAY "C1-ONE" ELSE DISPLAY "C1-NOT" END-IF
           MOVE 9 TO T2K(1) MOVE 8 TO T2K(2)
           SEARCH ALL T2 AT END DISPLAY "T2 NOT FOUND"
               WHEN T2K(X2) = 8
                   SET WIX TO X2
                   DISPLAY "T2 FOUND AT " WIX
           END-SEARCH
           MOVE 1 TO WK(1) MOVE 2 TO WK(2) MOVE 3 TO WK(3)
           SEARCH ALL WE AT END DISPLAY "WE NOT FOUND"
               WHEN WK(WX) = 3
                   SET WIX TO WX
                   DISPLAY "WE FOUND AT " WIX
           END-SEARCH
           MOVE 7 TO WDK(1) MOVE 4 TO WDK(2)
           SEARCH ALL WD AT END DISPLAY "WD NOT FOUND"
               WHEN WDK(WY) = 4
                   SET WIX TO WY
                   DISPLAY "WD FOUND AT " WIX
           END-SEARCH
           MOVE FUNCTION LENGTH(WODO) TO WLEN
           DISPLAY "WODOLEN=" WLEN
           MOVE 3 TO WN
           MOVE FUNCTION LENGTH(WODO) TO WLEN
           DISPLAY "WODOLEN=" WLEN
           STOP RUN.
