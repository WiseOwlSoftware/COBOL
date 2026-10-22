      *>--- conformance-case
      *> id: pb3510-depending-on-containing-record
      *> rules: [SR-13.18.38.3-17, SR-13.18.38.3-20]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3510 (fuzz program fz01530, reduced) - DEPENDING ON names
      *> WS-G, the record that CONTAINS the table WS-T. Two syntax rules of
      *> 13.18.38.3 refuse it: SR17, "Data-name-1 shall describe an integer"
      *> (WS-G is a group item; COBOLNET0852), and SR20, "The data item
      *> defined by data-name-1 shall not occupy a byte position within the
      *> range of the first byte position defined by the data description
      *> entry containing the OCCURS clause and the last byte position
      *> defined by the record description entry containing that OCCURS
      *> clause" (WS-G occupies every byte of that range; COBOLNET0857, the
      *> code this refusal expects). The binder overflowed its stack here
      *> before either diagnostic reached the user: resolving the group
      *> operand WS-G resolved its counter, WS-G, again without end.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3510N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
           05 WS-H.
               10 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-G PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO WS-T (1)
           DISPLAY WS-G
           STOP RUN.
