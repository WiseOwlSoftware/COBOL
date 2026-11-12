      *>--- conformance-case
      *> id: pb5187_p_scaled_image_compare
      *> rules: [GR-14.9.25.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5187. §2023:8.8.4.2.5: the integer operand of a comparison with an alphanumeric operand is moved to an
      *> item "of the same length in terms of character positions as the number of digits in the integer", and the move
      *> counts a P position as a zero (§2023:14.9.25.4 GR6 a), so PIC 99PP holding 1200 compares as "1200". PP and QP are
      *> subordinate to groups moved as wholes, so they are image-carried items; the image holds the two stored digits
      *> alone, and the P zeros must be appended as for any other numeric item. A compiler that compares "12" prints
      *> PPN and QPN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5187P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05 PP  PIC 99PP VALUE 1200.
       01  Q.
           05 QP PIC 99PP VALUE 1200.
       01  T2  PIC XX.
       PROCEDURE DIVISION.
       P1.
           MOVE G TO T2.
           MOVE Q TO T2.
           IF PP = "1200" DISPLAY "PPY" ELSE DISPLAY "PPN".
           IF QP = "1200" DISPLAY "QPY" ELSE DISPLAY "QPN".
           STOP RUN.
