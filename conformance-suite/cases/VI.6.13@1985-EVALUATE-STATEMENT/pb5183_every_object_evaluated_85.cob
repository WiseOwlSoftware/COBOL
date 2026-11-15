      *>--- conformance-case
      *> id: pb5183_every_object_evaluated_85
      *> rules: [1985-GR-VI.6.13.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5183. §1985:VI.6.13.4 GR1: "The execution of the
      *> EVALUATE statement operates as if each selection subject and
      *> selection object were evaluated and assigned a numeric or
      *> nonnumeric value, a range of numeric or nonnumeric values, or a
      *> truth value"; GR2 then compares. So a selection object no WHEN
      *> phrase reaches is evaluated too. §1989:A.2.35.3 AR2: "If a
      *> subsequent reference specifies argument-1, a new sequence of
      *> pseudo-random numbers is started"; a reference without it
      *> returns "the next number in the current sequence".
      *> Each case seeds 7, then the EVALUATE holds an object RANDOM (5)
      *> that the comparison never needs, then one RANDOM follows. If
      *> the object was evaluated that RANDOM is the second number of
      *> the seed-5 sequence (R1, computed first): EVALUATED.
      *>   1 a later WHEN phrase (the first one is selected)
      *>   2 the second pair of a phrase whose first pair is false
      *>   3 a THROUGH bound of a later phrase
      *>   4 a condition object paired with TRUE
      *>   5 a reference modifier of an object identifier
      *> The 2002 rule evaluates none of them (cases/14.9.13-EVALUATE/
      *> pb5183_unreached_object_not_evaluated).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5183A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 1.
       01  R1  PIC 9V9(6).
       01  R2  PIC 9V9(6).
       01  C   PIC X VALUE "A".
       01  AX  PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R1 = FUNCTION RANDOM (5).
           COMPUTE R1 = FUNCTION RANDOM.
           COMPUTE R2 = FUNCTION RANDOM (7).
           EVALUATE N
               WHEN 1 CONTINUE
               WHEN FUNCTION RANDOM (5) + 5 DISPLAY "WRONG"
           END-EVALUATE.
           PERFORM CHECK-PARA.
           COMPUTE R2 = FUNCTION RANDOM (7).
           EVALUATE N ALSO N
               WHEN 2 ALSO FUNCTION RANDOM (5) DISPLAY "WRONG"
               WHEN OTHER CONTINUE
           END-EVALUATE.
           PERFORM CHECK-PARA.
           COMPUTE R2 = FUNCTION RANDOM (7).
           EVALUATE N
               WHEN 1 CONTINUE
               WHEN 0 THRU FUNCTION RANDOM (5) DISPLAY "WRONG"
           END-EVALUATE.
           PERFORM CHECK-PARA.
           COMPUTE R2 = FUNCTION RANDOM (7).
           EVALUATE TRUE
               WHEN N = 1 CONTINUE
               WHEN FUNCTION RANDOM (5) > 2 DISPLAY "WRONG"
           END-EVALUATE.
           PERFORM CHECK-PARA.
           COMPUTE R2 = FUNCTION RANDOM (7).
           EVALUATE C
               WHEN "A" CONTINUE
               WHEN AX (FUNCTION INTEGER (FUNCTION RANDOM (5)) + 1:1)
                   DISPLAY "WRONG"
           END-EVALUATE.
           PERFORM CHECK-PARA.
           STOP RUN.
       CHECK-PARA.
           COMPUTE R2 = FUNCTION RANDOM.
           IF R1 = R2 DISPLAY "EVALUATED"
           ELSE DISPLAY "SKIPPED".
