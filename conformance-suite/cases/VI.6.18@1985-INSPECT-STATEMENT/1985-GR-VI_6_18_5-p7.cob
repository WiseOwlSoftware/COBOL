      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_5-p7
      *> rules: [1985-GR-VI.6.18.5-p7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.5 p7, Example 5: "The above INSPECT is equivalent
      *> to the following INSPECT": INSPECT ITEM CONVERTING "ABCD" TO
      *> "XYZX" AFTER QUOTE BEFORE "#" acts as the REPLACING of the four
      *> ALL phrases shown, and the printed row gives both, from
      *> AC"AEBDFBCD#AB"D, the value AC"XEYXFYZX#AB"D. The program runs
      *> both statements on equal copies and checks each result and that
      *> they agree (kb/Work PB6001, refuter 85e-01).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EX5P7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ITEM-C   PIC X(16).
       01  ITEM-R   PIC X(16).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AC""AEBDFBCD#AB""D" TO ITEM-C.
           MOVE ITEM-C TO ITEM-R.
           INSPECT ITEM-C CONVERTING
                "ABCD" TO "XYZX" AFTER QUOTE BEFORE "#".
           INSPECT ITEM-R REPLACING
                ALL "A" BY "X" AFTER QUOTE BEFORE "#"
                ALL "B" BY "Y" AFTER QUOTE BEFORE "#"
                ALL "C" BY "Z" AFTER QUOTE BEFORE "#"
                ALL "D" BY "X" AFTER QUOTE BEFORE "#".
           DISPLAY "CONVERTING " ITEM-C.
           DISPLAY "REPLACING  " ITEM-R.
           IF ITEM-C = "AC""XEYXFYZX#AB""D"
               DISPLAY "CONVERTING OK"
           ELSE
               DISPLAY "CONVERTING WRONG".
           IF ITEM-C = ITEM-R
               DISPLAY "EQUIVALENT OK"
           ELSE
               DISPLAY "EQUIVALENT WRONG".
           STOP RUN.
