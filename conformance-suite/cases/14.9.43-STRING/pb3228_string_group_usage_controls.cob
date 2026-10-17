      *>--- conformance-case
      *> id: pb3228_string_group_usage_controls
      *> rules: [SR-14.9.43.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3228 - THE POSITIVE CONTROL for the three bit-group refusals (pb3228-string-bit-group-*). A
      *> refusal's failure mode is reaching source the rule ADMITS, so each group STRING admits is written here.
      *>
      *> ISO 14.9.43.3 SR1: "all identifiers, except identifier-4, shall be described implicitly or explicitly as
      *> usage display or national."
      *>   AG  an alphanumeric group item: ISO 8.5.2.1 "An alphanumeric group item is treated as though it had a
      *>       usage of display" - admitted as identifier-1.
      *>   NG  a GROUP-USAGE NATIONAL group: 13.18.29.4 GR2 b) treats it as an elementary item of usage national,
      *>       so it is admitted with a national identifier-3 (SR1's second sentence: if any is national, all
      *>       are).
      *> ISO 14.9.43.4 GR3 a): the characters are transferred as alphanumeric-to-alphanumeric (national to
      *> national) moves do, with no space filling.
      *>   L1 STRING AG DELIMITED BY SIZE INTO AR: AR = "ABCD" (the rest keeps its VALUE spaces).
      *>   L2 STRING NG DELIMITED BY SIZE INTO NR: NR holds the national characters of NG, so NR = NG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3228P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AG.
          05 A1 PIC X(2) VALUE "AB".
          05 A2 PIC X(2) VALUE "CD".
       01 AR PIC X(8) VALUE SPACES.
       01 NG GROUP-USAGE NATIONAL.
          05 N1 PIC N(1) VALUE N"X".
          05 N2 PIC N(1) VALUE N"Y".
       01 NR PIC N(4) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           STRING AG DELIMITED BY SIZE INTO AR
           DISPLAY "L1 [" AR "]"
           STRING NG DELIMITED BY SIZE INTO NR
           IF NR(1:2) = N"XY"
               DISPLAY "L2 national group sent"
           ELSE
               DISPLAY "L2 WRONG"
           END-IF
           STOP RUN.
