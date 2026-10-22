      *>--- conformance-case
      *> id: pb3510-depending-on-group-with-picture
      *> rules: [SR-13.18.38.3-17, SR-13.18.38.3-20, SR-13.18.40.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3510 - the shape of fuzz program fz01530: WS-VARYING-LEN
      *> is written with a PICTURE, but a subordinate entry makes it a group,
      *> and the table under it depends on it. 13.18.40.3 SR1, "The PICTURE
      *> clause may be specified only at the elementary level"
      *> (COBOLNET2191); 13.18.38.3 SR17, "Data-name-1 shall describe an
      *> integer" (a group item is not one; COBOLNET0852, the code this
      *> refusal expects); and SR20 - the group occupies every byte position
      *> from the table's first to its record's last (COBOLNET0857). The
      *> binder overflowed its stack here before any of them was reported.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3510N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-VARYING-LEN PIC S9(4).
           05 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-VARYING-LEN
               PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO WS-T (1)
           DISPLAY WS-VARYING-LEN
           STOP RUN.
