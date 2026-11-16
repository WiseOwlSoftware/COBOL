      *>--- conformance-case
      *> id: g2-numval-c-alphanumeric-or-national
      *> rules: [2002-AR-15.55.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.55.2 AR1 - "Argument-1 shall be of category alphanumeric
      *> or national." Each leg writes a LEGAL argument-1 of a different
      *> shape and must be ACCEPTED and evaluated: an alphanumeric item, a
      *> national item, an alphanumeric group item (§2002:8.5.2 "an
      *> alphanumeric group item has class and category alphanumeric"), an
      *> alphanumeric literal, a national literal, and the alphanumeric
      *> record item of a 2002 input CD (obsolete, COBOL0903 warning only).
      *> Values by §2002:15.55.3 RV1 ("the numeric value represented by
      *> argument-1") and RV2 (negative for "CR, DB, or a minus sign"), the
      *> default currency sign $ (AR4) and period decimal separator (AR5c):
      *>   AN   "$1,234.50" -> +1234.50     NA   N"$3.25CR"  -> -0003.25
      *>   GRP  "-$7.00"    -> -0007.00     ALIT "$12.50"    -> +0012.50
      *>   NLIT N"$0.75 DB" -> -0000.75     CD   "$98.10"    -> +0098.10
      *> A leg fails if a legal category is refused (COBOL1627 misfiring)
      *> or if the national or group representation is misread.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NVC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AN      PIC X(10) VALUE "$1,234.50".
       01  NA      PIC N(8) VALUE N"$3.25CR".
       01  G.
           05  GA  PIC X(8) VALUE "-$7.00".
       01  R       PIC +9(4).99.
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT.
       01  IN-REC.
           05  IN-Q    PIC X(12).
           05  IN-R    PIC X(75).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION NUMVAL-C(AN)
           DISPLAY "AN=" R
           COMPUTE R = FUNCTION NUMVAL-C(NA)
           DISPLAY "NA=" R
           COMPUTE R = FUNCTION NUMVAL-C(G)
           DISPLAY "GRP=" R
           COMPUTE R = FUNCTION NUMVAL-C("$12.50")
           DISPLAY "ALIT=" R
           COMPUTE R = FUNCTION NUMVAL-C(N"$0.75 DB")
           DISPLAY "NLIT=" R
           MOVE "$98.10" TO IN-Q
           COMPUTE R = FUNCTION NUMVAL-C(IN-Q)
           DISPLAY "CD=" R
           STOP RUN.
