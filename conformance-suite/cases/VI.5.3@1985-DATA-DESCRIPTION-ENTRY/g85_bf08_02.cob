      *>--- conformance-case
      *> id: g85_bf08_02
      *> rules: [1985-SR-VI.5.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.3.3 SR1 - "Level-number in format 1 may be any number
      *> from 01 through 49 or 77." The program uses the bounds and
      *> interior values of that range: 01, 02, 05, 10, 49 and 77, so a
      *> compiler that refused any of them (49 is the upper bound) rejects
      *> legal source. The hierarchy those numbers build is then
      *> observed: R1=[ABCD] is the 01 group of its four one-character
      *> elementaries, H=[BC] the 10 group of its two 49 items, S=[77]
      *> the 77 item, T=[XY] the 01/02 record; after MOVE "YZ" TO WS-H
      *> the record reads R2=[AYZD] - a compiler that did not
      *> subordinate the 49 items to the 10 group would not change
      *> positions 2 and 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0802.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  WS-77 PIC X(2) VALUE "77".
       01  WS-REC.
           05  WS-A PIC X VALUE "A".
           05  WS-G.
               10  WS-H.
                   49  WS-B PIC X VALUE "B".
                   49  WS-C PIC X VALUE "C".
           05  WS-D PIC X VALUE "D".
       01  WS-TWO.
           02  WS-T1 PIC X VALUE "X".
           02  WS-T2 PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "R1=[" WS-REC "]".
           DISPLAY "H=[" WS-H "]".
           DISPLAY "S=[" WS-77 "]".
           DISPLAY "T=[" WS-TWO "]".
           MOVE "YZ" TO WS-H.
           DISPLAY "R2=[" WS-REC "]".
           STOP RUN.
