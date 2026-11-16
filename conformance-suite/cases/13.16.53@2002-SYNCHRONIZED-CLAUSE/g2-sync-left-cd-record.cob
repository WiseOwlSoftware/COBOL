      *>--- conformance-case
      *> id: g2-sync-left-cd-record
      *> rules: [2002-GR-13.16.53.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.53.3 GR3 - "SYNCHRONIZED LEFT specifies that the
      *> elementary item is to be positioned such that it will begin at
      *> the left byte of the natural boundary in which the elementary
      *> item is placed."
      *> In 2002 SYNCHRONIZED is written in the data description entries
      *> of a record under a CD (§2002:A.1.1 "The communication
      *> description entry is followed by none, one, or more record
      *> description entries"); S2 and S3 of the output CD's record
      *> CDREC are elementary items with SYNC LEFT and SYNCHRONIZED LEFT.
      *> The natural boundaries are the implementor's (§2002:8.5.1.5.4
      *> "such as word boundaries, half-word boundaries, and byte
      *> boundaries"; §2002:13.16.53.3 GR8 "The effect of this clause is
      *> defined by the implementor"). WiseOwl COBOL's natural boundary
      *> is every byte (docs/conformance/7/DOC-A.1-197.md), so the left
      *> byte of the boundary in which S2 is placed is the byte after S1:
      *> each SYNC LEFT item begins right after its predecessor, no
      *> implicit FILLER is generated, and the item keeps its size
      *> (§2002:13.16.53.3 GR5). WREC is the same record without the
      *> clause.
      *> DERIVATION
      *>  elementary MOVEs P / QRS / 7 / T into each record: the bytes
      *>  are contiguous in both            -> "[PQRS07T] [PQRS07T]"
      *>  group MOVE "ABCD12E": S2 begins at byte 2, S3 at byte 5
      *>                                    -> "S2=BCD S3=12 S4=E"
      *>  1+3+2+1 = 7, S2 stays 3           -> "LEN=07 TWIN=07 S2LEN=3"
      *> A leg fails if SYNC LEFT is refused in a CD record, if an item
      *> is shifted off the left byte of its boundary (S2 or S3 would
      *> not begin where WREC's twin does), or if its size changes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SYN03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WREC.
           05  W1 PIC X.
           05  W2 PIC X(3).
           05  W3 PIC 9(2).
           05  W4 PIC X.
       01  LEN-C PIC 99.
       01  LEN-W PIC 99.
       01  LEN-S PIC 9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CDREC.
           05  S1 PIC X.
           05  S2 PIC X(3) SYNC LEFT.
           05  S3 PIC 9(2) SYNCHRONIZED LEFT.
           05  S4 PIC X.
       PROCEDURE DIVISION.
           MOVE "P" TO S1 W1.
           MOVE "QRS" TO S2 W2.
           MOVE 7 TO S3 W3.
           MOVE "T" TO S4 W4.
           DISPLAY "[" CDREC "] [" WREC "]".
           MOVE "ABCD12E" TO CDREC.
           DISPLAY "S2=" S2 " S3=" S3 " S4=" S4.
           MOVE FUNCTION LENGTH(CDREC) TO LEN-C.
           MOVE FUNCTION LENGTH(WREC) TO LEN-W.
           MOVE FUNCTION LENGTH(S2) TO LEN-S.
           DISPLAY "LEN=" LEN-C " TWIN=" LEN-W " S2LEN=" LEN-S.
           STOP RUN.
