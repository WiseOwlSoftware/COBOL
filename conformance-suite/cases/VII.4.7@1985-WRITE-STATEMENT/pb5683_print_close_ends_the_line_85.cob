      *>--- conformance-case
      *> id: pb5683_print_close_ends_the_line_85
      *> rules: [1985-GR-VII.4.7.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb5683p.dat, expected: pb5683_print_close_ends_the_line_85.exp}
      *>---
      *> kb/Work PB5683, the ruling on the row (kb/Work/PB5683/
      *> 2026-11-16-ruling-extend-after-close.md). §1985:VII.4.7.4 GR13:
      *> a WRITE to a file open in the extend mode adds records "as
      *> though the file were open in the output mode", so AA, BB and
      *> CC are in that order; where a line stands is the form of "a
      *> representation of a printed page" (GR16), the implementor's.
      *> The determination D-PRINT-CLOSE-ENDS-THE-LINE gives the bytes:
      *> each WRITE AFTER ADVANCING 1 LINE puts one line end before its
      *> line and leaves the line open, and the CLOSE ends that line
      *> (one line end), so the file is an empty first line, AA, BB, an
      *> empty line, CC: inside one OPEN one line end lies between two
      *> lines, across the CLOSE and the OPEN EXTEND two. The harness
      *> compares the file with LF line ends and the trailing line end
      *> trimmed, so the expected content holds on every host. A
      *> runtime whose CLOSE left the line open shows no empty line
      *> before CC; one whose OPEN EXTEND began a new page or line on
      *> its own shows two. Each OPEN, WRITE and CLOSE is status 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5683PCL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G ASSIGN TO "pb5683p.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       FD G LABEL RECORDS ARE STANDARD.
       01 G-R PIC X(2).
       WORKING-STORAGE SECTION.
       01 GS PIC XX.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT G.
           DISPLAY "OPEN OUTPUT " GS.
           MOVE "AA" TO G-R. WRITE G-R AFTER ADVANCING 1 LINE.
           MOVE "BB" TO G-R. WRITE G-R AFTER ADVANCING 1 LINE.
           DISPLAY "WRITE " GS.
           CLOSE G.
           DISPLAY "CLOSE " GS.
           OPEN EXTEND G.
           DISPLAY "OPEN EXTEND " GS.
           MOVE "CC" TO G-R. WRITE G-R AFTER ADVANCING 1 LINE.
           DISPLAY "WRITE " GS.
           CLOSE G.
           DISPLAY "CLOSE " GS.
           STOP RUN.
