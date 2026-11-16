      *>--- conformance-case
      *> id: g2call01_overflow_activating_instance
      *> rules: [2002-GR-14.8.4.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.3 GR1 -- "The instance of the program, function,
      *> or method that executes the CALL statement is the activating
      *> runtime element." The rule sits under ALL FORMATS; this case
      *> writes every CALL in Format 1 (on-overflow, §2002:14.8.4.1:
      *> "ON OVERFLOW is an archaic feature"), the 2002 format no
      *> later edition has.
      *> DERIVATION. §2002:11.9.3 GR4: RECURSIVE makes G2C1R
      *> recursive. §2002:8.6.3: "Each activated instance of the
      *> runtime element has its own copy of the item" of
      *> LOCAL-STORAGE, so each activation reads its own LS-TAG.
      *>   D=1 sets TAG 11                        R-ENTER D=1 TAG=11
      *>   D=1 calls itself; D=2 sets TAG 22      R-ENTER D=2 TAG=22
      *>   D=2 skips recursion, resumes           R-RESUME D=2 TAG=22
      *>   control returns to the D=1 INSTANCE    R-RESUME D=1 TAG=11
      *>   (GR1: the activator is that instance)
      *> No OVF line: every program is present, so no EC-PROGRAM
      *> condition and §2002:14.8.4.3 GR3h ignores ON OVERFLOW.
      *> Fails if: control resumes in the wrong instance (TAG=22 on the
      *> D=1 resume line), the D=2 activation shares the D=1 copy of
      *> LS-TAG, or ON OVERFLOW runs on a successful call.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C1M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2C1R" USING WS-D ON OVERFLOW DISPLAY "OVF" END-CALL
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C1R RECURSIVE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-TAG PIC 9(2) VALUE 0.
       01 LS-NEXT PIC 9.
       LINKAGE SECTION.
       01 LK-D PIC 9.
       PROCEDURE DIVISION USING LK-D.
       P.
           COMPUTE LS-TAG = LK-D * 11
           DISPLAY "R-ENTER D=" LK-D " TAG=" LS-TAG
           IF LK-D < 2
               COMPUTE LS-NEXT = LK-D + 1
               CALL "G2C1R" USING LS-NEXT
                   ON OVERFLOW DISPLAY "OVF"
               END-CALL
           END-IF
           DISPLAY "R-RESUME D=" LK-D " TAG=" LS-TAG
           GOBACK.
       END PROGRAM G2C1R.
       END PROGRAM G2C1M.
