      *>--- conformance-case
      *> id: g2-renames-thru-cd-and-ws
      *> rules: [2002-SR-13.16.43.2-1, 2002-SR-13.16.43.2-2, 2002-SR-13.16.43.2-4, 2002-SR-13.16.43.2-10, 2002-GR-13.16.43.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR1 - "Any number of RENAMES entries may be
      *> written for a record": three 66 entries follow one working-
      *> storage record and three follow one record under a CD (a
      *> CD's records are record description entries, §2002:A.1.1;
      *> SR3 names CD among the qualifiers of data-name-1).
      *> §2002:13.16.43.3 GR2 - a THROUGH entry "defines an alphanumeric
      *> group item that includes all elementary items starting with
      *> data-name-2" (or the first elementary item of a group) "and
      *> concluding with" data-name-3 (or "the last elementary item in
      *> data-name-3"). The legal forms of SR2 (entries immediately
      *> after the record), SR4 (two different names in the same
      *> record) and SR10 (data-name-3 begins at or after and ends after
      *> data-name-2) are each written here and must be accepted.
      *> Every cite.py --check above is OK.
      *> DERIVATION. Each record is G1(A1 X2, A2 9(2)) B1 X3
      *> G3(C1 X2, C2 X2) D1 X5 = 16 characters, filled with "*"
      *> then set to AB 12 BBB CC DD.
      *>   AL  = G1 THRU G3 = A1..C2 = "AB12BBBCCDD", 11 characters.
      *>   R12 = A1 THRU B1         = "AB12BBB", 7 characters.
      *>   RB2 = B1 THRU C2         = "BBBCCDD", 7 characters.
      *> AL is an alphanumeric group, so MOVE "XY" TO AL stores XY and
      *> nine spaces in A1..C2 and leaves D1 "*****": the record reads
      *> "XY         *****". A leg fails if a range starts or ends at
      *> the wrong item, if the alias is not a group (no space fill,
      *> or D1 changed), or if an entry under the CD is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L2 PIC 99.
       01  WREC.
           05 WG1.
              10 WA1 PIC X(2).
              10 WA2 PIC 9(2).
           05 WB1 PIC X(3).
           05 WG3.
              10 WC1 PIC X(2).
              10 WC2 PIC X(2).
           05 WD1 PIC X(5).
       66  WAL RENAMES WG1 THRU WG3.
       66  WR12 RENAMES WA1 THROUGH WB1.
       66  WRB2 RENAMES WB1 THRU WC2.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1.
              10 A1 PIC X(2).
              10 A2 PIC 9(2).
           05 B1 PIC X(3).
           05 G3.
              10 C1 PIC X(2).
              10 C2 PIC X(2).
           05 D1 PIC X(5).
       66  AL RENAMES G1 THRU G3.
       66  R12 RENAMES A1 THROUGH B1.
       66  RB2 RENAMES B1 THRU C2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE ALL "*" TO WREC
           MOVE "AB" TO WA1 MOVE 12 TO WA2 MOVE "BBB" TO WB1
           MOVE "CC" TO WC1 MOVE "DD" TO WC2
           MOVE FUNCTION LENGTH(WAL) TO L2
           DISPLAY "WAL=[" WAL "] L=" L2
           MOVE FUNCTION LENGTH(WR12) TO L2
           DISPLAY "WR12=[" WR12 "] L=" L2
           MOVE FUNCTION LENGTH(WRB2) TO L2
           DISPLAY "WRB2=[" WRB2 "] L=" L2
           MOVE "XY" TO WAL
           DISPLAY "WREC=[" WREC "]"
           MOVE ALL "*" TO CDREC
           MOVE "AB" TO A1 MOVE 12 TO A2 MOVE "BBB" TO B1
           MOVE "CC" TO C1 MOVE "DD" TO C2
           MOVE FUNCTION LENGTH(AL) TO L2
           DISPLAY "AL=[" AL IN CDOUT "] L=" L2
           MOVE FUNCTION LENGTH(R12) TO L2
           DISPLAY "R12=[" R12 "] L=" L2
           MOVE FUNCTION LENGTH(RB2) TO L2
           DISPLAY "RB2=[" RB2 "] L=" L2
           MOVE "XY" TO AL
           DISPLAY "CDREC=[" CDREC "]"
           STOP RUN.
