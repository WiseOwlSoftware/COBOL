      *>--- conformance-case
      *> id: g2-redefines-cd-record-storage
      *> rules: [2002-SR-13.16.42.2-9, 2002-SR-13.16.42.2-10, 2002-SR-13.16.42.2-15, 2002-GR-13.16.42.3-1, 2002-GR-13.16.42.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.3 GR1, GR2 and the accepted forms of
      *> §2002:13.16.42.2 SR9, SR10 and SR15, in a record description under an
      *> output CD (legal at 2002: §2002:13.16.42.2 SR4 bars REDEFINES only at
      *> level 1 of the communication section). Each expected line from the
      *> 2002 text. GR1 "Storage association for the subject of the entry
      *> starts at the first bit of the data item referenced by data-name-2 and
      *> continues over an area sufficient to contain the number of bits
      *> required by the data item referenced by the subject of the entry":
      *> ADD 1 TO C1R (9(4) over C1) leaves C1=1235; C1S (X(2) over C1) reads
      *> C1S=12; MOVE "XY" TO C1S changes only the first two positions,
      *> C1=XY35; B2R (4 bits over B2) reads 0110 written through B2, and the
      *> write of 1001 through B2R leaves B1=0000. GR1 "The size used for
      *> references to the data item referenced by data-name-2 is not changed":
      *> LEN-C1=04 although C1S is smaller; LEN-C1S=02. GR2 "the data-name
      *> associated with any of those data description entries may be used to
      *> reference that storage area": each line above reads one name after a
      *> write through another; CDREC2, which implicitly redefines the CD
      *> record area (§2002:A.1.2.2 SR9), reads CDREC2=1235CD, and DC, the
      *> DESTINATION COUNT field at positions 1 to 4 of that area
      *> (§2002:A.1.2.3 GR16), reads DC=1235. SR9: C1R (equal) and C1S
      *> (smaller) are accepted. SR10: the 88 C3-CD under the redefining C3R is
      *> accepted and true, C3-CD=TRUE, since C3 holds "CD". SR15: B2R has B2's
      *> alignment (both bit items at bit 5 of byte 7) and is accepted. A leg
      *> fails if a redefinition gets storage of its own, an offset other than
      *> data-name-2's first bit, or a length other than its own. The bit legs
      *> are compared, not displayed (§2002:14.8.10.3 GR1 leaves the display
      *> conversion to the implementor).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1  PIC X(4).
           05 C1R REDEFINES C1 PIC 9(4).
           05 C1S REDEFINES C1 PIC X(2).
           05 C3  PIC X(2).
           05 C3R REDEFINES C3 PIC X(2).
              88 C3-CD VALUE "CD".
           05 B1  PIC 1(4) USAGE BIT.
           05 B2  PIC 1(4) USAGE BIT.
           05 B2R REDEFINES B2 PIC 1(4) USAGE BIT.
       01  CDREC2 PIC X(6).
       PROCEDURE DIVISION.
           MOVE "1234" TO C1
           MOVE "CD" TO C3
           ADD 1 TO C1R
           DISPLAY "C1=" C1
           DISPLAY "C1S=" C1S
           MOVE FUNCTION LENGTH(C1) TO N
           DISPLAY "LEN-C1=" N
           MOVE FUNCTION LENGTH(C1S) TO N
           DISPLAY "LEN-C1S=" N
           DISPLAY "CDREC2=" CDREC2
           DISPLAY "DC=" DC
           MOVE "XY" TO C1S
           DISPLAY "C1=" C1
           IF C3-CD
               DISPLAY "C3-CD=TRUE"
           ELSE
               DISPLAY "C3-CD=FALSE"
           END-IF
           MOVE B"0000" TO B1
           MOVE B"0110" TO B2
           IF B2R = B"0110"
               DISPLAY "B2R=0110"
           ELSE
               DISPLAY "B2R=OTHER"
           END-IF
           MOVE B"1001" TO B2R
           IF B2 = B"1001"
               DISPLAY "B2=1001"
           ELSE
               DISPLAY "B2=OTHER"
           END-IF
           IF B1 = B"0000"
               DISPLAY "B1=0000"
           ELSE
               DISPLAY "B1=OTHER"
           END-IF
           STOP RUN.
