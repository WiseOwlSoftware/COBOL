      *>--- conformance-case
      *> id: g2goto01_cd_depending_integer
      *> rules: [2002-SR-14.8.16.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.16.2 SR1 - "Identifier-1 shall reference a numeric
      *> elementary data item that is an integer." Permitted operands
      *> from the COMMUNICATION SECTION: the output CD's DESTINATION
      *> COUNT DC, "a data item whose implicit description is that of an
      *> integer" (§2002:A.1.2.3 GR16a), and RN PIC 9 in the input CD's
      *> record. §2002:14.8.16.3 GR2: "control is transferred to
      *> procedure-name-1, etc., depending on the value of identifier-1
      *> being 1, 2" ...; for any other value "then no transfer occurs
      *> and control passes to the next statement in the normal
      *> sequence for execution".
      *>  G1 DC = 2 -> the second name, P2           -> G1 P2
      *>  G2 RN = 3 -> the third name, Q3            -> G2 Q3
      *>  G3 DC = 0 -> no transfer, the next statement -> G3 FELL
      *> The refusals beside it (g2goto02..g2goto04) name an
      *> alphanumeric, a non-integer and a group CD operand. Each line
      *> fails if a CD integer is refused or read with the wrong value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOT01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       CD CDIN FOR INPUT.
       01 INREC.
          05 RN     PIC 9.
          05 RX     PIC 9V9.
          05 FILLER PIC X(84).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 2 TO DC.
           GO TO P1 P2 P3 DEPENDING ON DC.
           DISPLAY "G1 FELL". GO TO T2.
       P1. DISPLAY "G1 P1". GO TO T2.
       P2. DISPLAY "G1 P2". GO TO T2.
       P3. DISPLAY "G1 P3". GO TO T2.
       T2.
           MOVE 3 TO RN.
           GO TO Q1 Q2 Q3 DEPENDING ON RN.
           DISPLAY "G2 FELL". GO TO T3.
       Q1. DISPLAY "G2 Q1". GO TO T3.
       Q2. DISPLAY "G2 Q2". GO TO T3.
       Q3. DISPLAY "G2 Q3". GO TO T3.
       T3.
           MOVE 0 TO DC.
           GO TO R1 R2 DEPENDING ON DC.
           DISPLAY "G3 FELL".
           STOP RUN.
       R1. DISPLAY "G3 R1". STOP RUN.
       R2. DISPLAY "G3 R2". STOP RUN.
       END PROGRAM G2GOT01.
