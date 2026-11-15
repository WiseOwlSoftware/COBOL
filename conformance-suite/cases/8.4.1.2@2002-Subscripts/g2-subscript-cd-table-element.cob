      *>--- conformance-case
      *> id: g2-subscript-cd-table-element
      *> rules: [2002-SR-8.4.1.2.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.2 SR2 - "If a subscript is specified, the data
      *> description entry describing qualified-data-name-1 or the
      *> conditional variable associated with qualified-condition-name-1
      *> shall contain an OCCURS clause or shall be subordinate to a
      *> data description entry that contains an OCCURS clause." At
      *> 2002 qualified-data-name-1 reaches the communication section
      *> (§2002:8.4.1.1.1 file-cd-report-qualifier). The accepted half:
      *> - T1 under CDOUT contains an OCCURS clause: T1 OF CDREC IN
      *>   CDOUT (I + 1) is T1 (3) (S1 [ABY]); its condition-name T1-Y
      *>   takes the subscript of its conditional variable (S2, S3);
      *> - GA and GB are subordinate to G, which contains the OCCURS
      *>   clause (S4 RQ from "PQRS");
      *> - SD2 and EK2 are data-names the CD entry itself defines under
      *>   DESTINATION TABLE OCCURS 2 TIMES (§2002:A.1.2.3 GR16, "02
      *>   data-name OCCURS integer-1 TIMES" over ERROR KEY PIC X and
      *>   SYMBOLIC DESTINATION PIC X(12)), which "may be referenced
      *>   only by subscripting" (§2002:A.1.2.2 SR12) (S5).
      *> The refused half is g2-subscript-cd-*-non-table beside it.
      *> Each leg can fail: a refused subscript fails the compile; a
      *> wrong occurrence prints another letter or destination.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUB02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  I PIC 9 VALUE 2.
       COMMUNICATION SECTION.
       CD  CDT FOR OUTPUT
           DESTINATION COUNT IS DC2
           TEXT LENGTH IS TL2
           STATUS KEY IS SK2
           DESTINATION TABLE OCCURS 2 TIMES
           ERROR KEY IS EK2
           SYMBOLIC DESTINATION IS SD2.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 T1 PIC X OCCURS 3 TIMES.
              88 T1-Y VALUE "Y".
           05 G OCCURS 2 TIMES.
              10 GA PIC X.
              10 GB PIC X.
           05 S1 PIC 9.
       PROCEDURE DIVISION.
           MOVE "ABCPQRS0" TO CDREC.
           MOVE "Y" TO T1 OF CDREC IN CDOUT (I + 1).
           DISPLAY "S1 [" T1 (1) T1 (2) T1 (3) "]".
           IF T1-Y (3)
               DISPLAY "S2 TRUE"
           ELSE
               DISPLAY "S2 FALSE"
           END-IF.
           IF T1-Y OF T1 IN CDOUT (1)
               DISPLAY "S3 TRUE"
           ELSE
               DISPLAY "S3 FALSE"
           END-IF.
           DISPLAY "S4 " GA (2) GB OF G (1).
           MOVE "DEST1" TO SD2 (1).
           MOVE "DEST2" TO SD2 (2).
           MOVE "E" TO EK2 (2).
           DISPLAY "S5 [" SD2 (2) "][" SD2 (1) "]" EK2 (2).
           STOP RUN.
