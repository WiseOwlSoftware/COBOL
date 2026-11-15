      *>--- conformance-case
      *> id: g2-aligned-cd-record-non-bit
      *> rules: [2002-SR-13.16.1.2-1]
      *> source-format: fixed
      *>---
      *> §2002:13.16.1.2 SR1 - "The ALIGNED clause may be specified
      *> only for a bit group item or an elementary bit data item."
      *> OUT-REC is an alphanumeric group in a record description entry
      *> following a CD entry (§2002:A.1.1.2: data description entries
      *> of 13.14), so ALIGNED on it violates SR1 and the program is
      *> refused at the ALIGNED line.  The accepted sibling is
      *> g2-aligned-cd-record-bit-items.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALG02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       01  OUT-REC ALIGNED.
           05 OUT-X PIC X(3).
       PROCEDURE DIVISION.
       P-PARA.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
