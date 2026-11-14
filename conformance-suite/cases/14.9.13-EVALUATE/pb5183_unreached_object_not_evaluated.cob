      *>--- conformance-case
      *> id: pb5183_unreached_object_not_evaluated
      *> rules: [GR-14.9.13.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5183. §2023:14.9.13.4 GR4: the WHEN phrases are
      *> analyzed in turn, and "If the result of the analysis is false
      *> for any pair in a WHEN phrase, no more pairs in that WHEN phrase
      *> are evaluated"; a phrase after the selected one is not analyzed
      *> at all. RANDOM (§2023:15.75.3 AR5): "subsequent references
      *> without specifying argument-1 return the next number in the
      *> current sequence". Each case seeds 7, then the EVALUATE holds an
      *> object RANDOM (5) the analysis never reaches, then one RANDOM
      *> follows: the seed-7 sequence continues, so it is not R1 (the
      *> second number of the seed-5 sequence): SKIPPED, five times
      *> (a later phrase, the pair after a false one, a THROUGH bound, a
      *> condition object, a reference modifier). The 1985 rule evaluates
      *> every object (cases/VI.6.13@1985-EVALUATE-STATEMENT/
      *> pb5183_every_object_evaluated_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5183B.
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
