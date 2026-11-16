      *>--- conformance-case
      *> id: pb5777_unstring_odo_national_group_receiver_current_extent
      *> rules: [GR-14.9.48.4-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB5777 review R1-1: the current extent of a GROUP-USAGE
      *> NATIONAL group holding an OCCURS DEPENDING ON table is counted
      *> in national character positions, not bytes. NG is 2 national
      *> positions here, so NG takes AB and R1 takes CDE, pointer 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5777N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NO2 PIC 9 VALUE 2.
       01 NG GROUP-USAGE NATIONAL.
          05 NY PIC N OCCURS 1 TO 5 TIMES DEPENDING ON NO2.
       01 R1 PIC N(3).
       01 S PIC N(9) VALUE N"ABCDEFGHI".
       01 P PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO P.
           UNSTRING S INTO NG R1 WITH POINTER P.
           DISPLAY "NG=[" NG "] R1=[" R1 "] P=" P.
           STOP RUN.
