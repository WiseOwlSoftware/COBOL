      *>--- conformance-case
      *> id: pb6074_same_record_area_redefines_in_second_record
      *> rules: [GR-12.4.6.4.4-2, GR-13.18.44.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6074 item 3 (PB5729): a REDEFINES written INSIDE the record of the second file of a SAME RECORD AREA group.
      *>   §2023:12.4.6.4.4 GR2 (cite.py: OK on its quoted sentence "A logical record in the shared memory area is a logical record
      *>   of each file open in the output mode") makes B-REC and A-REC one memory area, so B-N, which redefines B-X, is a window over
      *>   the same bytes as B-X, and both are windows over A-REC (§2023:13.18.44.4 GR1: the redefinition "starts at the first bit of
      *>   the data item referenced by data-name-2"). The compiler classes the redefinition B-N / B-X as one area and the implicit
      *>   redefinition B-REC / A-REC as another that holds the first; it must make ONE overlay of them and assign each name its
      *>   window. Values (each derived from the layout: B-X is A-REC positions 1-4, B-Y positions 5-6):
      *>   A-REC = 123456 gives B-X 1234, B-N 1234, B-Y 56; then B-N = 9876 gives A-REC 987656.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6074R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "pb6074r-a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FB ASSIGN TO "pb6074r-b.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR FA FB.
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 A-REC PIC X(6).
       FD FB.
       01 B-REC.
          05 B-X PIC X(4).
          05 B-N REDEFINES B-X PIC 9(4).
          05 B-Y PIC X(2).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT FA.
           MOVE "123456" TO A-REC.
           DISPLAY "X[" B-X "] N[" B-N "] Y[" B-Y "]".
           MOVE 9876 TO B-N.
           DISPLAY "A[" A-REC "]".
           CLOSE FA.
           STOP RUN.
