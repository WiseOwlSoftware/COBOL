      *>--- conformance-case
      *> id: pb6074_initialize_refmod_national_boolean
      *> rules: [GR-14.9.20.4-4, GR-14.9.20.4-5, GR-14.9.20.4-6, GR-8.4.3.3.4-6]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6074 item 2 (PB5813 R1-3): the National and Boolean arms of the category a reference-modified
      *>   INITIALIZE receiver takes. §2023:8.4.3.3.4 GR6 (cite.py: OK): the unique data item "has the same class,
      *>   category, and usage as that defined for identifier-1" except that alphanumeric-edited, national-edited,
      *>   numeric and numeric-edited are rewritten, so a slice of a national item stays national and a slice of a
      *>   boolean item stays boolean. §2023:14.9.20.4 GR5 initializes an item of the category a REPLACING phrase names,
      *>   and GR6c (cite.py: OK) gives the default sender by category: national SPACES for national, ZEROES for boolean.
      *>   Legs (each display is its expected value):
      *>   M1 WN is N"ABCD"; its national slice (2:1) REPLACING NATIONAL by N"X" gives AXCD.
      *>   M2 the national slice (3:1) REPLACING ALPHANUMERIC changes nothing, as the slice is not alphanumeric: AXCD.
      *>   M3 the national slice (1:1) with no REPLACING phrase takes national space: " XCD".
      *>   M4 the boolean slice (2:2) of B"1111" with no REPLACING phrase takes zeroes: 1001.
      *>   M5 the boolean slice (3:1) of B"0000" REPLACING BOOLEAN by B"1" gives 0010.
      *>   M6 the same slice REPLACING NUMERIC by 1 changes nothing, the slice being boolean: 0000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6074I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WN PIC N(4) VALUE N"ABCD".
       01  WB PIC 1(4) VALUE B"1111".
       01  WC PIC 1(4) VALUE B"0000".
       01  WD PIC 1(4) VALUE B"0000".
       PROCEDURE DIVISION.
       P0.
           INITIALIZE WN (2:1) REPLACING NATIONAL DATA BY N"X".
           DISPLAY "M1[" WN "]".
           INITIALIZE WN (3:1) REPLACING ALPHANUMERIC DATA BY "Z".
           DISPLAY "M2[" WN "]".
           INITIALIZE WN (1:1).
           DISPLAY "M3[" WN "]".
           INITIALIZE WB (2:2).
           DISPLAY "M4[" WB "]".
           INITIALIZE WC (3:1) REPLACING BOOLEAN DATA BY B"1".
           DISPLAY "M5[" WC "]".
           INITIALIZE WD (3:1) REPLACING NUMERIC DATA BY 1.
           DISPLAY "M6[" WD "]".
           STOP RUN.
