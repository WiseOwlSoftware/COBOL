      *>--- conformance-case
      *> id: g85_bf10_05
      *> rules: [1985-SR-VI.5.11.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.11.3 SR5 - "Data-name-2 and data-name-3 may be qualified."
      *> FA and FB each occur twice in REC (under S1 and under S2), so every
      *> RENAMES operand below must be qualified to be unique.
      *>   RN1=CDEF     : FB OF S1 THRU FA OF S2.
      *>   RN2=EF       : FA OF S2 alone.
      *>   RN3=ABCD     : FA IN S1 IN REC THRU FB IN S1 (two-level qualifiers).
      *>   REC=ABWXYZGH : a MOVE to RN1 lands on FB OF S1 and FA OF S2 only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1005.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  S1.
               10  FA              PIC X(2) VALUE "AB".
               10  FB              PIC X(2) VALUE "CD".
           05  S2.
               10  FA              PIC X(2) VALUE "EF".
               10  FB              PIC X(2) VALUE "GH".
       66  RN1 RENAMES FB OF S1 THRU FA OF S2.
       66  RN2 RENAMES FA OF S2.
       66  RN3 RENAMES FA IN S1 IN REC THROUGH FB IN S1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RN1=" RN1.
           DISPLAY "RN2=" RN2.
           DISPLAY "RN3=" RN3.
           MOVE "WXYZ" TO RN1.
           DISPLAY "REC=" REC.
           STOP RUN.
