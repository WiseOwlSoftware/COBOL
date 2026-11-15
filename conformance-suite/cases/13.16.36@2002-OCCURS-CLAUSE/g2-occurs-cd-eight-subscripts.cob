      *>--- conformance-case
      *> id: g2-occurs-cd-eight-subscripts
      *> rules: [2002-SR-13.16.36.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR9 - nested OCCURS without DEPENDING ON are
      *> allowed while the subscripts required do not exceed seven. A8
      *> needs eight, in a record under an OUTPUT CD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 A1 OCCURS 2.
           06 A2 OCCURS 2.
            07 A3 OCCURS 2.
             08 A4 OCCURS 2.
              09 A5 OCCURS 2.
               10 A6 OCCURS 2.
                11 A7 OCCURS 2.
                 12 A8 PIC X OCCURS 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
