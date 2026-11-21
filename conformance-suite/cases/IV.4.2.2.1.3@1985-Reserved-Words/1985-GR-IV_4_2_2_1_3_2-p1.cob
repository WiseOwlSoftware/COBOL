      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_3_2-p1
      *> rules: [1985-GR-IV.4.2.2.1.3.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.3.2 p1 (Reserved Words). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.2.2.1.3.2-p1: optional words may be written or
      * omitted with no effect on the semantics.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P08OPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PICTURE IS 9(3) VALUE IS 12.
       01 B PICTURE 9(3) VALUE 7.
       01 C PIC 9(3) USAGE IS DISPLAY.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A IS GREATER THAN B DISPLAY "1=T" ELSE DISPLAY "1=F".
           IF A GREATER B DISPLAY "2=T" ELSE DISPLAY "2=F".
           IF A IS NOT LESS THAN B DISPLAY "3=T" ELSE DISPLAY "3=F".
           IF A NOT LESS B DISPLAY "4=T" ELSE DISPLAY "4=F".
           IF A IS NUMERIC DISPLAY "5=T" ELSE DISPLAY "5=F".
           IF A NUMERIC DISPLAY "6=T" ELSE DISPLAY "6=F".
           ADD A B GIVING C.
           DISPLAY "C=" C.
           GO TO P-END.
       P-END.
           STOP RUN.
