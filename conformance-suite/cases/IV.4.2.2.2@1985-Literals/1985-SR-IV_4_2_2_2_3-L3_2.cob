      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_2_3-L3_2
      *> rules: [1985-SR-IV.4.2.2.2.3-L3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 SRL3.2 (Literals). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OBSALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NUM-ITEM PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE ALL "12" TO NUM-ITEM.
           DISPLAY NUM-ITEM.
           STOP RUN.
