*>--- conformance-case
*> id: pb4764_length_of_variable_length_group_extreme
*> rules: [AR-15.50.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4764 review R1-1 - a class test over FUNCTION LENGTH (VG), a variable-length group's run-time LENGTH. ISO §2023:8.8.4.4.3 SR6
      *> needs a numeric identifier-1 and §2023:8.4.3.2.4 1) makes the function-identifier a temporary data item, so FARTHEST-FROM-ZERO and
      *> NEAREST-TO-ZERO (§2023:8.8.4.4.4 3) g and its sibling) are legal; LENGTH is an integer function, so its value 3 is neither the
      *> farthest value nor the nearest nonzero one (1) of the returned-value item. Each test was a run-time abort for want of a range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4764EXTREME.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K     PIC 9 VALUE 3.
       01 VG.
          05 T  PIC X OCCURS 1 TO 9 DEPENDING ON K.
       PROCEDURE DIVISION.
           IF FUNCTION LENGTH(VG) IS FARTHEST-FROM-ZERO
              DISPLAY "L-FAR"
           ELSE
              DISPLAY "L-NOT-FAR"
           END-IF
           IF FUNCTION BYTE-LENGTH(VG) IS NEAREST-TO-ZERO
              DISPLAY "B-NEAR"
           ELSE
              DISPLAY "B-NOT-NEAR"
           END-IF
           MOVE 1 TO K
           IF FUNCTION LENGTH(VG) IS NEAREST-TO-ZERO
              DISPLAY "L1-NEAR"
           ELSE
              DISPLAY "L1-NOT-NEAR"
           END-IF
           STOP RUN.
