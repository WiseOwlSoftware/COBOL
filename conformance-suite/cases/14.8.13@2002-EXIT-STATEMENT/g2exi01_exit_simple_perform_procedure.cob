      *>--- conformance-case
      *> id: g2exi01_exit_simple_perform_procedure
      *> rules: [2002-FMT-14.8.13.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.1 - the formats of the EXIT statement that need no
      *> activation: Format 1 "EXIT", Format 5 "EXIT PERFORM [ CYCLE ]",
      *> Format 6 "EXIT { PARAGRAPH | SECTION }" (rendered page 468:
      *> CYCLE is optional, PARAGRAPH/SECTION a choice). Formats 2-4 are
      *> g2exi02..g2exi04. Each accepted form also does what its rule
      *> says (cite.py --check OK on each):
      *>  §2002:14.8.13.3 GR10 CYCLE "causes control to be passed to an
      *>   implicit CONTINUE statement immediately preceding the
      *>   END-PERFORM phrase": I=2 is skipped, the loop goes on.
      *>  §2002:14.8.13.3 GR9 EXIT PERFORM "causes control to be passed to
      *>   an implicit CONTINUE statement immediately following the
      *>   END-PERFORM phrase": I=4 leaves with I still 4.
      *>  §2002:14.8.13.3 GR11 EXIT PARAGRAPH: "PARA NOT REACHED" never.
      *>  §2002:14.8.13.3 GR1 "Such an EXIT statement has no other effect
      *>   on the compilation or execution": P-EXIT ends the PERFORM THRU
      *>   range, and falling into P-EXIT2 continues into P-NEXT.
      *>  §2002:14.8.13.3 GR12 EXIT SECTION: control goes past X-2.
      *> Expected: LOOP 1, LOOP 3, AFTER-LOOP 4, PARA IN, P-A, THRU BACK,
      *> FELL INTO P-NEXT, SECTION IN, SECTION BACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9.
       PROCEDURE DIVISION.
       M-SEC SECTION.
       M-P.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               IF I = 2
                   EXIT PERFORM CYCLE
               END-IF
               IF I = 4
                   EXIT PERFORM
               END-IF
               DISPLAY "LOOP " I
           END-PERFORM
           DISPLAY "AFTER-LOOP " I
           PERFORM P-PARA
           PERFORM P-A THRU P-EXIT
           DISPLAY "THRU BACK"
           GO TO P-EXIT2.
       P-PARA.
           DISPLAY "PARA IN"
           EXIT PARAGRAPH
           DISPLAY "PARA NOT REACHED".
       P-A.
           DISPLAY "P-A".
       P-EXIT.
           EXIT.
       P-EXIT2.
           EXIT.
       P-NEXT.
           DISPLAY "FELL INTO P-NEXT"
           PERFORM X-SEC
           DISPLAY "SECTION BACK"
           STOP RUN.
       X-SEC SECTION.
       X-1.
           DISPLAY "SECTION IN"
           EXIT SECTION
           DISPLAY "SECTION NOT REACHED".
       X-2.
           DISPLAY "X-2 NOT REACHED".
