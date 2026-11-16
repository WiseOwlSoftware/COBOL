      *>--- conformance-case
      *> id: g2-value-sr3-unsigned-cd
      *> rules: [2002-SR-13.16.61.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR3 - "If a signed numeric literal is
      *> specified, the subject of the entry shall be a signed numeric
      *> data item."  CU is an UNSIGNED numeric item (PIC 99) of a
      *> record under a CD entry (§2002:A.1.1.2), and its VALUE is the
      *> signed literal +1.  +1 lies within the range PIC 99 indicates,
      *> so §2002:13.16.61.2 SR2 is met and only SR3 forbids the entry.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if the compiler accepts the signed literal on the
      *> unsigned communication-section item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VS302.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 CU PIC 99 VALUE +1.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
       END PROGRAM G2VS302.
