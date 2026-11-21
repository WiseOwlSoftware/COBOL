      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_2_3-p1
      *> rules: [1985-SR-IV.4.2.2.2.3-p1, 1985-SR-IV.4.2.2.2.3-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 p1; §1985:IV.4.2.2.2.3 p6 (Literals). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.2.2.2.3-p1 and -p6: figurative constants are not
      * bounded by quotes; singular and plural are equivalent; ALL
      * and the word after it are two separate character-strings.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P16FIG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4).
       01 B PIC X(4).
       01 N PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ZERO" TO A.
           DISPLAY "1[" A "]".
           MOVE ZERO TO A. MOVE ZEROS TO B.
           IF A = B DISPLAY "2=T" ELSE DISPLAY "2=F".
           MOVE ZEROES TO B.
           IF A = B DISPLAY "3=T" ELSE DISPLAY "3=F".
           MOVE SPACE TO A. MOVE SPACES TO B.
           IF A = B DISPLAY "4=T" ELSE DISPLAY "4=F".
           MOVE HIGH-VALUE TO A. MOVE HIGH-VALUES TO B.
           IF A = B DISPLAY "5=T" ELSE DISPLAY "5=F".
           MOVE LOW-VALUE TO A. MOVE LOW-VALUES TO B.
           IF A = B DISPLAY "6=T" ELSE DISPLAY "6=F".
           MOVE QUOTE TO A. MOVE QUOTES TO B.
           IF A = B DISPLAY "7=T" ELSE DISPLAY "7=F".
           DISPLAY "8[" B "]".
           MOVE ALL
                    SPACES TO A.
           MOVE ALL      "XY" TO B.
           DISPLAY "9[" A "][" B "]".
           IF N > ZEROS DISPLAY "10=T" ELSE DISPLAY "10=F".
           STOP RUN.
