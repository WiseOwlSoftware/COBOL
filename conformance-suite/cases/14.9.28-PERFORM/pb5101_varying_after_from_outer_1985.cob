      *>--- conformance-case
      *> id: pb5101_varying_after_from_outer_1985
      *> rules: [GR-14.9.28.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5101 - PERFORM VARYING ... AFTER at COBOL-85: the ORDER of the two operations of an AFTER step.
      *>
      *> §1985:VI.6.21.4 GR10 d) 1 (checked with scripts/spec/cite.py --check): "the content of the data item
      *> referenced by identifier-2 is augmented by literal-2 or the content of the data item referenced by
      *> identifier-4" and THEN "the content of the data item referenced by identifier-5 is set to literal-3 or the
      *> current value of the data item referenced by identifier-6".  The OUTER variable augments FIRST, so an
      *> AFTER level whose FROM is the outer variable is reset from the POST-augment value.  (From COBOL-2002 the
      *> order is the other way round: see pb436_varying_after_from_outer, 8 bodies.)
      *>
      *> TEST BEFORE, A 1..3, AFTER B FROM A: B=1,2,3 run the body (3), then A:=2, B:=A=2; B=2,3 (2);
      *> then A:=3, B:=3; B=3 (1); then A:=4, B:=4; A>3 ends.   3+2+1 = 6 bodies (1,1)(1,2)(1,3)(2,2)(2,3)(3,3);
      *> A=4, B=4.  NIST NC201A PFM-TEST-F4-23 asserts the same 6.
      *>
      *> TEST AFTER (the arm that is in the same order from every edition): 4+3+2+1 = 10 bodies, I=4, J=4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5101VARY85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 9 VALUE 0.
       01 B   PIC 9 VALUE 0.
       01 WI  PIC 9 VALUE 0.
       01 WJ  PIC 9 VALUE 0.
       01 N   PIC 99 VALUE 0.
       01 T2  PIC X(12) VALUE SPACES.
       01 T5  PIC X(20) VALUE SPACES.
       01 P   PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO P
           MOVE 0 TO N
           PERFORM REC-AB VARYING A FROM 1 BY 1 UNTIL A > 3
                            AFTER B FROM A BY 1 UNTIL B > 3
           DISPLAY "BEFORE-N=" N " A=" A " B=" B
           DISPLAY "BEFORE-T=" T2
           MOVE 1 TO P
           MOVE 0 TO N
           PERFORM REC-IJ WITH TEST AFTER
                   VARYING WI FROM 1 BY 1 UNTIL WI > 3
                     AFTER WJ FROM WI BY 1 UNTIL WJ > 3
           DISPLAY "TSTAFT-N=" N " I=" WI " J=" WJ
           DISPLAY "TSTAFT-T=" T5
           STOP RUN.
       REC-AB.
           ADD 1 TO N
           STRING A B DELIMITED BY SIZE INTO T2 WITH POINTER P.
       REC-IJ.
           ADD 1 TO N
           STRING WI WJ DELIMITED BY SIZE INTO T5 WITH POINTER P.
