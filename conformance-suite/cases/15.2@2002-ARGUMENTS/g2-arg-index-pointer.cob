      *>--- conformance-case
      *> id: g2-arg-index-pointer
      *> rules: [2002-AR-15.2-4, 2002-AR-15.2-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.2 AR4 - "An index data item shall be specified": index
      *> data items given to MAX, MIN, ORD-MAX, ORD-MIN, LENGTH and
      *> BYTE-LENGTH (Table 22 Ind1).
      *> §2002:15.2 AR12 - "A data item of class pointer shall be
      *> specified": a USAGE POINTER item given to LENGTH and
      *> BYTE-LENGTH (Table 22 Ptr1).
      *> The implementor chooses the value an index data item holds for
      *> an occurrence number (§2002:13.16.58.3 GR9), and index data
      *> items compare by those actual values (§2002:8.8.4.1.1.10), so
      *> the expectations rest only on what the text fixes:
      *>   IX1 3 OK  IX1 (occurrence 3) and IX2 (occurrence 5) differ,
      *>             so ORD-MAX and ORD-MIN name the two positions and
      *>             sum to 3 (§2002:15.58.3 RV1, 15.59.3 RV1); MAX and
      *>             MIN return the content of the argument they name
      *>             (§2002:15.47.3 RV1), and SET TX TO that content
      *>             then SET W-N TO TX gives its occurrence number.
      *>   IX2 1 1 03  IX3 was SET from IX1 (content placed unchanged),
      *>             so the two are equal and ORD-MAX and ORD-MIN name
      *>             the leftmost (15.58.3 RV3); MAX(IX3) is
      *>             occurrence 3.
      *>   IX3 OK    LENGTH and BYTE-LENGTH of an index data item are
      *>             the size of a data item, so positive (15.39.3 RV3,
      *>             15.12.3 RV1); the size itself is the implementor's.
      *>   PT1 OK    the same for the pointer item P1.
      *> A refused argument, or a wrong ordinal, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARG03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 TE PIC X OCCURS 9 INDEXED BY TX.
       01 IX1 USAGE INDEX.
       01 IX2 USAGE INDEX.
       01 IX3 USAGE INDEX.
       01 P1  USAGE POINTER.
       01 W-MX PIC 99.
       01 W-MN PIC 99.
       01 N1A  PIC 9.
       01 N1B  PIC 9.
       01 N1C  PIC 9.
       PROCEDURE DIVISION.
       P0.
           SET TX TO 3
           SET IX1 TO TX
           SET TX TO 5
           SET IX2 TO TX
           SET IX3 TO IX1
           MOVE FUNCTION ORD-MAX(IX1 IX2) TO N1A
           MOVE FUNCTION ORD-MIN(IX1 IX2) TO N1B
           COMPUTE N1C = N1A + N1B
           SET TX TO FUNCTION MAX(IX1 IX2)
           SET W-MX TO TX
           SET TX TO FUNCTION MIN(IX1 IX2)
           SET W-MN TO TX
           IF ((N1A = 1 AND W-MX = 3) OR (N1A = 2 AND W-MX = 5))
              AND ((N1B = 1 AND W-MN = 3) OR (N1B = 2 AND W-MN = 5))
               DISPLAY "IX1 " N1C " OK"
           ELSE
               DISPLAY "IX1 " N1C " BAD " W-MX " " W-MN
           END-IF
           MOVE FUNCTION ORD-MAX(IX1 IX3) TO N1A
           MOVE FUNCTION ORD-MIN(IX1 IX3) TO N1B
           SET TX TO FUNCTION MAX(IX3)
           SET W-MX TO TX
           DISPLAY "IX2 " N1A " " N1B " " W-MX
           IF FUNCTION LENGTH(IX1) > 0
              AND FUNCTION BYTE-LENGTH(IX1) > 0
               DISPLAY "IX3 OK"
           ELSE
               DISPLAY "IX3 BAD"
           END-IF
           IF FUNCTION LENGTH(P1) > 0
              AND FUNCTION BYTE-LENGTH(P1) > 0
               DISPLAY "PT1 OK"
           ELSE
               DISPLAY "PT1 BAD"
           END-IF
           STOP RUN.
