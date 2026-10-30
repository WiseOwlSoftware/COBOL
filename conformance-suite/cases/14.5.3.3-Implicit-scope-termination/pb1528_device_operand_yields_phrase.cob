      *>--- conformance-case
      *> id: pb1528_device_operand_yields_phrase
      *> rules: [GR-14.5.3.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1528. ISO 14.5.3.3 (its closing paragraph, after list item 4):
      *> "Any phrase encountered is the next phrase of the most-recently
      *> preceding unterminated statement with which that phrase may be
      *> syntactically associated." (cite.py --check 14.5.3.3 -> OK.)
      *> Only the screen formats of ACCEPT (14.9.1.2 Format 3) and DISPLAY
      *> (14.9.11.2 Format 2) print [NOT] ON EXCEPTION, and their operand is a
      *> screen-name. WS-MSG and WS-X are data items, so neither inner device
      *> statement may take the phrase that follows it: the phrase is the
      *> enclosing CALL's, and END-CALL closes the CALL. Why it can fail: a
      *> compiler that gives the phrase to the inner statement refuses the
      *> program (a screen format whose operand is no screen-name) or runs
      *> the CALL without its arm.
      *> Expected values: a CALL of a program that does not exist sets an
      *> EC-PROGRAM exception condition, and 14.9.4.4 3) h) 1.: "an ON
      *> EXCEPTION phrase is specified in the CALL statement, control is
      *> transferred to imperative-statement-1" (cite.py --check 14.9.4.4 ->
      *> OK), so leg 1 displays WS-MSG and leg 2 never runs its ACCEPT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1528DY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(5) VALUE "1 EXC".
       01 WS-X   PIC X VALUE "-".
       PROCEDURE DIVISION.
       MAIN-P.
      * 1. NOT ON EXCEPTION after a device DISPLAY of a data item.
           CALL "PB1528XX"
               ON EXCEPTION DISPLAY WS-MSG
               NOT ON EXCEPTION DISPLAY "1 NOT REACHED"
           END-CALL
      * 2. ON EXCEPTION after a device ACCEPT of a data item.
           CALL "PB1528XX"
               NOT ON EXCEPTION ACCEPT WS-X
               ON EXCEPTION DISPLAY "2 EXC X=" WS-X
           END-CALL
           STOP RUN.
