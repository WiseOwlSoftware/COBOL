      *>--- conformance-case
      *> id: g2-function-receive-into-function
      *> rules: [2002-SR-8.4.2.2.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.2.2 SR1 - "A function-identifier shall not be
      *> specified as a receiving operand." RECEIVE ... INTO
      *> identifier-1
      *> (2002 Annex A, the communication facility) transfers the
      *> message "to the receiving character positions of the area
      *> referenced by identifier-1" (§2002:A.2.5.3 GR3): identifier-1
      *> is a receiving operand, so FUNCTION UPPER-CASE (W) there is
      *> refused. RECEIVE CDIN MESSAGE INTO W, the same statement with
      *> a data item, compiles.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FNR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(10).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
           RECEIVE CDIN MESSAGE INTO FUNCTION UPPER-CASE (W)
               NO DATA DISPLAY "NO DATA"
           END-RECEIVE.
           STOP RUN.
