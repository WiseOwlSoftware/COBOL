       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB151OF.
       OPTIONS.
           INITIALIZE ALL TO "Q".
      *> kb/Work PB151 - ISO 14.9.3.4 GR8: with no INITIALIZED phrase the
      *> allocated content "depends on the INITIALIZE clause of the
      *> OPTIONS paragraph. If it is specified, the content is that of
      *> the specified-fill-character" - the clause previously had ZERO
      *> consumers and every allocation space-filled. The INITIALIZED
      *> phrase still wins with binary zeros (GR6), pinned by the
      *> low-value comparison leg.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 B3 PIC X(3) BASED.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE 3 CHARACTERS RETURNING P
           SET ADDRESS OF B3 TO P
           DISPLAY "FILL=" B3
           ALLOCATE 3 CHARACTERS INITIALIZED RETURNING P
           SET ADDRESS OF B3 TO P
           IF B3 = LOW-VALUES DISPLAY "INIT=ZEROS" END-IF
           STOP RUN.
