      *>--- conformance-case
      *> id: pb4233_picture_string_then_separator_period
      *> rules: [FMT-13.18.40.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4233 - the accepted twin of
      *> pb4233-picture-clause-without-character-string. ISO 13.18.40.2
      *> Format 1: PICTURE IS character-string-1. A period inside a
      *> picture character-string is an insertion symbol (13.18.40.5),
      *> while a period followed by a space is the separator period
      *> (8.3.5 3)), so each entry below ends at its period and its
      *> picture is the text before it. WS-C's picture 9.99 ends its
      *> line with the separator period, so the next line is a new
      *> entry, not more picture. Expected, by 14.9.25 MOVE to a
      *> numeric-edited item (13.18.40.5 4) period insertion) and to an
      *> alphanumeric item: 1.25, ABC, 3.50 and OK. It fails if the
      *> separator period is read as part of the picture (WS-C would
      *> take the next line's text) or if the insertion period ends the
      *> picture early.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4233PICOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.99.
       01 WS-B PICTURE IS X(3).
       01 WS-C PIC 9.99.
       01 WS-D PIC X(2).
       PROCEDURE DIVISION.
           MOVE 1.25 TO WS-A.
           MOVE 'ABC' TO WS-B.
           MOVE 3.5 TO WS-C.
           MOVE 'OK' TO WS-D.
           DISPLAY WS-A.
           DISPLAY WS-B.
           DISPLAY WS-C.
           DISPLAY WS-D.
           STOP RUN.
