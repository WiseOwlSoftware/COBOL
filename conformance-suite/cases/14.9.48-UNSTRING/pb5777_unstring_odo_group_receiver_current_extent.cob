      *>--- conformance-case
      *> id: pb5777_unstring_odo_group_receiver_current_extent
      *> rules: [GR-14.9.48.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.4 GR13b: with no DELIMITED BY the number of
      *> characters examined is the size of the current receiving
      *> area; §1985:VI.5.8.4 GR3a: a group holding an OCCURS DEPENDING
      *> ON table whose object lies outside it uses only the part given
      *> by that object's value. OH is 2 characters long here, so OH
      *> takes AB and R1 takes CDE, and the pointer ends at 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5777A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NO2 PIC 9 VALUE 2.
       01 OH.
          05 OY PIC X OCCURS 1 TO 5 TIMES DEPENDING ON NO2.
       01 R1 PIC X(3).
       01 S PIC X(9) VALUE "ABCDEFGHI".
       01 P PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO P.
           UNSTRING S INTO OH R1 WITH POINTER P.
           DISPLAY "OH=[" OH "] R1=[" R1 "] P=" P.
           STOP RUN.
