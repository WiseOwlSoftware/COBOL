*>--- conformance-case
*> id: pb3511_implicit_goback_function_method
*> rules: [GR-14.6.3-3, GR-14.9.18.4-4, GR-14.9.18.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - the implicit GOBACK past the last paragraph of a FUNCTION or
      *>   a METHOD, reached while a PERFORM in it is active, ends that
      *>   activation and returns to the activating statement (14.9.18.4 GR5,
      *>   GR4); it never returns to the PERFORM.
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> cite.py --check 14.9.18.4 "The execution of a GOBACK statement within
      *>   a function causes the executing function to terminate, and control
      *>   to return to the activating statement" -> OK  14.9.18.4 5)
      *> cite.py --check 14.9.18.4 "The execution of a GOBACK statement within
      *>   a method causes the executing method to terminate, and control to
      *>   return to the invoking statement" -> OK  14.9.18.4 4)
      *> Derivation: PB3511FN sets R to 1, PERFORMs FP1 (R = 2), whose GO TO
      *>   FP3 leaves the range; FP3 adds 10 (R = 12) and is the last
      *>   paragraph, so the implicit GOBACK returns R = 0012 and MOVE 9 TO R
      *>   never runs. Method M1 displays M1-P1, leaves its PERFORM by GO TO
      *>   MP3 (M1-P3), and returns to the INVOKE: M1-AFTER-PERFORM is never
      *>   shown, and the method that follows M1 in the class (M2) runs only
      *>   when it is INVOKEd. How it can fail: returning to the PERFORM yields
      *>   0009 and displays M1-AFTER-PERFORM.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB3511FN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION RETURNING R.
       FA.
           MOVE 1 TO R
           PERFORM FP1
           MOVE 9 TO R.
       FP1.
           MOVE 2 TO R
           GO TO FP3.
       FP2.
           MOVE 5 TO R.
       FP3.
           ADD 10 TO R.
       END FUNCTION PB3511FN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511FM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB3511FN
           CLASS PB3511CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE OBJECT REFERENCE PB3511CL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY FUNCTION PB3511FN
           INVOKE PB3511CL "NEW" RETURNING G
           INVOKE G "M1"
           DISPLAY "BACK"
           INVOKE G "M2"
           STOP RUN.
       END PROGRAM PB3511FM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3511CL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
       MA.
           PERFORM MP1
           DISPLAY "M1-AFTER-PERFORM".
       MP1.
           DISPLAY "M1-P1"
           GO TO MP3.
       MP2.
           DISPLAY "M1-P2".
       MP3.
           DISPLAY "M1-P3".
       END METHOD M1.
       METHOD-ID. M2.
       PROCEDURE DIVISION.
       MB.
           DISPLAY "M2".
       END METHOD M2.
       END OBJECT.
       END CLASS PB3511CL.
