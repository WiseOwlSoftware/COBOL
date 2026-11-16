      *>--- conformance-case
      *> id: pb5776_string_odo_group_receiver_maximum_length
      *> rules: [1985-GR-VI.5.8.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5776 - §1985:VI.5.8.4 GR3b: when a group holding an OCCURS DEPENDING ON table whose object (data-name-1)
      *> is inside the group is a receiving item, "the maximum length of the group will be used" (§2023:13.18.38.4 GR8b
      *> the same). §1985:VI.6.25.4 GR1 makes identifier-3 the receiving item, GR4a moves characters with no space filling
      *> and GR8 changes only the positions referenced. H holds "5*****" (6 positions at the maximum), HN is then 1, and
      *> STRING "ab" at pointer 2 stores into positions 2 and 3 of the 6, so there is no overflow, H is "5ab***" and
      *> the pointer is 4. A table read at its current extent (2 positions) overflowed and blanked the rest.
      *> The second STRING sends 6 characters from pointer 2 into the 5 positions left: that overflows by GR8a, stores
      *> the first 5 characters ("5abcde") and leaves the pointer at 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5776A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H.
          05 HN PIC 9.
          05 HE PIC X OCCURS 1 TO 5 TIMES DEPENDING ON HN.
       01 PTR PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO HN.
           MOVE "5*****" TO H.
           MOVE 1 TO HN.
           MOVE 2 TO PTR.
           STRING "ab" DELIMITED BY SIZE INTO H WITH POINTER PTR
               ON OVERFLOW DISPLAY "A OVERFLOW"
               NOT ON OVERFLOW DISPLAY "A NO OVERFLOW"
           END-STRING.
           MOVE 5 TO HN.
           DISPLAY "A H=[" H "] PTR=" PTR.
           MOVE "5*****" TO H.
           MOVE 1 TO HN.
           MOVE 2 TO PTR.
           STRING "abcdef" DELIMITED BY SIZE INTO H WITH POINTER PTR
               ON OVERFLOW DISPLAY "B OVERFLOW"
               NOT ON OVERFLOW DISPLAY "B NO OVERFLOW"
           END-STRING.
           MOVE 5 TO HN.
           DISPLAY "B H=[" H "] PTR=" PTR.
           STOP RUN.
