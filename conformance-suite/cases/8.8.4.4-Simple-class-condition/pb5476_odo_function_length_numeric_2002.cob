      *>--- conformance-case
      *> id: pb5476_odo_function_length_numeric_2002
      *> rules: [SR-8.8.4.4.3-8]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5476 review R1-1 - the positive half of pb5476-odo-function-length-numeric-at-85.
      *> §2023:8.8.4.4.3 SR8: "If the NUMERIC phrase is specified, identifier-1 shall reference
      *> a data item whose usage is display or national or whose category is numeric." LENGTH of the
      *> variable-length group is an integer function (category numeric), value 3: T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5476Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  VG.
           05  E PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       P1.
           IF FUNCTION LENGTH (VG) IS NUMERIC
               DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
