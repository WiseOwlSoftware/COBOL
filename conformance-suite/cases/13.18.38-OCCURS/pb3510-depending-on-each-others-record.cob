      *>--- conformance-case
      *> id: pb3510-depending-on-each-others-record
      *> rules: [SR-13.18.38.3-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3510 - two records whose tables each depend on the OTHER
      *> record. Neither counter lies in its own table's range (SR20 holds:
      *> they are separate records), but neither is an integer: 13.18.38.3
      *> SR17, "Data-name-1 shall describe an integer" (COBOL0852, once
      *> per table). The binder overflowed its stack here too: resolving the
      *> group operand WS-A resolved its counter WS-B, a group whose own
      *> counter is WS-A, and so on without end. Only an integer counter -
      *> an elementary item - is recorded as a table's DEPENDING item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3510N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A.
           05 WS-TA OCCURS 1 TO 5 TIMES DEPENDING ON WS-B PIC X.
       01 WS-B.
           05 WS-TB OCCURS 1 TO 5 TIMES DEPENDING ON WS-A PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO WS-TA (1)
           DISPLAY WS-A
           DISPLAY WS-B
           STOP RUN.
