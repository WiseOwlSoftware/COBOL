      *>--- conformance-case
      *> id: pb6074_accept_odo_group_current_extent
      *> rules: [GR-14.9.1.4-4, GR-13.18.38.4-8]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *> stdin: pb6074_accept_odo_group_current_extent.in
      *>---
      *> kb/Work PB6074 item 1 (PB5777 R1-2, R2-1). §2023:13.18.38.4 GR8a: an occurs-depending group whose
      *>   DEPENDING ON item is outside it is used only to "that part of the table area that is specified by
      *>   the value of the data item referenced by data-name-1 at the start of the operation"; so its size, for
      *>   §2023:14.9.1.4 GR4b ("only the leftmost characters of the transferred data are stored in the receiving
      *>   data item"), is the CURRENT extent, not the maximum (GR8b's maximum is for a DEPENDING item INSIDE the
      *>   group). Each group below is filled with a marker at its full extent (the count is 5), then holds 2
      *>   occurrences of 2 positions (4 positions) for the ACCEPT, which takes the leftmost 4 characters of its
      *>   record and ignores the rest; the count goes back to 5 and the display shows the 4 stored positions and
      *>   the 6 the ACCEPT left alone. The alphanumeric, national and bit groups are three arms of the one size.
      *>   Records: ABCDEFGHIJ, ABCDEFGHIJ, 10110011. Expected: GA ABCD------, GN ABCD------, GB 1011000000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6074A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  NA PIC 9.
       77  NN PIC 9.
       77  NB PIC 9.
       01  GA.
           05  TA PIC XX OCCURS 1 TO 5 TIMES DEPENDING ON NA.
       01  GN GROUP-USAGE NATIONAL.
           05  TN PIC N(2) OCCURS 1 TO 5 TIMES DEPENDING ON NN.
       01  GB GROUP-USAGE BIT.
           05  TB PIC 1(2) OCCURS 1 TO 5 TIMES DEPENDING ON NB.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO NA NN NB.
           MOVE ALL "-" TO GA.
           MOVE ALL N"-" TO GN.
           MOVE B"0000000000" TO GB.
           MOVE 2 TO NA NN NB.
           ACCEPT GA.
           ACCEPT GN.
           ACCEPT GB.
           MOVE 5 TO NA NN NB.
           DISPLAY "GA[" GA "]".
           DISPLAY "GN[" GN "]".
           DISPLAY "GB[" GB "]".
           STOP RUN.
