      *>--- conformance-case
      *> id: pb6140_return_from_nested_frames
      *> rules: [GR-14.9.18.4-2, GR-14.9.14.4-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB6140 - ISO 14.9.18.4 GR2: "The execution of an GOBACK
      *>   statement causes the executing program to terminate, and
      *>   control to return to the calling statement. If a RETURNING
      *>   phrase is specified in the procedure division header of the
      *>   program containing the GOBACK statement, the value in the
      *>   data item referenced by that RETURNING phrase becomes the
      *>   result of the program activation." The rule names no frame:
      *>   the program terminates from whatever depth of PERFORM
      *>   statements is active, and no active PERFORM returns.
      *> cite.py --check "§2023:14.9.18.4 GR2" "causes the executing
      *>   program to terminate, and control to return to the calling
      *>   statement" -> OK
      *> cite.py --check "§2023:14.9.14.4 GR3" "execution proceeds as
      *>   specified in 14.9.18, GOBACK statement" -> OK (EXIT PROGRAM
      *>   in a called program returns as GOBACK does)
      *> Each leg fails a different way, because WiseOwl returns
      *> DIRECTLY up a chain of dispatch frames that all test for the
      *> return, and throws the return signal below any frame that
      *> does not (kb/Work PB6140):
      *> - PB6140A: GOBACK two PERFORMs deep, inside an inline PERFORM
      *>   VARYING loop: a frame that dropped the return resumes its
      *>   PERFORM and DISPLAYs "NOT REACHED"; the RETURNING item
      *>   delivers 10 * CNT, so a lost result shows as the previous
      *>   value. CALLed twice: the instance is reused after a return.
      *> - PB6140B: EXIT PROGRAM inside an IF, in the THRU range of a
      *>   PERFORM.
      *> - PB6140D: GOBACK in a paragraph a USE declarative PERFORMs.
      *>   The declarative runs below the activation's frames, so the
      *>   return takes the thrown form there. It is entered by OPEN
      *>   INPUT of a file that is not present: cite.py --check
      *>   "§2023:9.1.13.6 GR5" "on a file that is not described as
      *>   optional and the physical file is not present" -> OK (I-O
      *>   status 35, a permanent error). CALLed twice: the second
      *>   OPEN runs the declarative again, which it would not if the
      *>   first return had left it one that "had previously been
      *>   activated and had not yet returned control" (cite.py
      *>   --check "§2023:14.9.49.4 GR2" -> OK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6140M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(4) VALUE 0.
       01 RES PIC 9(4) VALUE 0.
       01 K   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 2
               CALL "PB6140A" USING CNT RETURNING RES
               DISPLAY "A " K " CNT=" CNT " RES=" RES
           END-PERFORM
           CALL "PB6140B" USING CNT
           DISPLAY "B CNT=" CNT
           CALL "PB6140D" USING CNT
           DISPLAY "D CNT=" CNT
           CALL "PB6140D" USING CNT
           DISPLAY "D CNT=" CNT
           STOP RUN.
       END PROGRAM PB6140M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6140A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 J PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 CNT PIC 9(4).
       01 R   PIC 9(4).
       PROCEDURE DIVISION USING CNT RETURNING R.
       A0.
           MOVE 0 TO R
           PERFORM A1
           DISPLAY "A NOT REACHED A0".
       A1.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 5
               PERFORM A2
               DISPLAY "A NOT REACHED A1"
           END-PERFORM.
       A2.
           ADD 1 TO CNT
           COMPUTE R = CNT * 10
           GOBACK.
       END PROGRAM PB6140A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6140B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 CNT PIC 9(4).
       PROCEDURE DIVISION USING CNT.
       S1 SECTION.
       B0.
           PERFORM B1 THRU B2
           DISPLAY "B NOT REACHED B0".
       B1.
           ADD 1 TO CNT.
       B2.
           IF CNT > 0
               EXIT PROGRAM
           END-IF
           DISPLAY "B NOT REACHED B2".
       END PROGRAM PB6140B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6140D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb6140-no-such-file.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       LINKAGE SECTION.
       01 CNT PIC 9(4).
       PROCEDURE DIVISION USING CNT.
       DECLARATIVES.
       D-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       D1.
           PERFORM D2
           DISPLAY "D NOT REACHED D1".
       D2.
           ADD 100 TO CNT
           GOBACK.
       END DECLARATIVES.
       M SECTION.
       M0.
           OPEN INPUT F
           DISPLAY "D NOT REACHED M0".
       END PROGRAM PB6140D.
