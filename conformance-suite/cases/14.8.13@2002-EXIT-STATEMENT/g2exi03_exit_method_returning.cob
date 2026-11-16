      *>--- conformance-case
      *> id: g2exi03_exit_method_returning
      *> rules: [2002-GR-14.8.13.3-7, 2002-FMT-14.8.13.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR7 - "The execution of an EXIT METHOD statement
      *> causes the executing method to terminate, and control to return
      *> to the invoking statement. If a RETURNING phrase is present in
      *> the containing method definition, the value in the data item
      *> referenced by the RETURNING phrase becomes the result of the
      *> method invocation." (cite.py --check OK, GR7). Format 3 plain
      *> EXIT METHOD (§2002:14.8.13.1).
      *>   M=04   GETV moves 4 to RES then EXIT METHOD; the later MOVE 97
      *>          and M-NEVER are never executed, so the result is 04.
      *>   W=08   GETW executes EXIT METHOD inside a PERFORMed paragraph:
      *>          the METHOD terminates (not just the PERFORM), so W-NEVER
      *>          and the MOVE 99 after the PERFORM never run.
      *>   NR-IN, NR-BACK  NORET has no RETURNING phrase: EXIT METHOD
      *>          returns control to the INVOKE, which goes on to NR-BACK.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXI03C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 9(2).
       PROCEDURE DIVISION RETURNING RES.
       P1.
           MOVE 4 TO RES
           EXIT METHOD.
           MOVE 97 TO RES
           DISPLAY "M-NEVER".
       END METHOD GETV.
       METHOD-ID. GETW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 9(2).
       PROCEDURE DIVISION RETURNING RES.
       P1.
           PERFORM P2
           MOVE 99 TO RES
           DISPLAY "W-NEVER".
       P2.
           MOVE 8 TO RES
           EXIT METHOD.
       END METHOD GETW.
       METHOD-ID. NORET.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "NR-IN"
           EXIT METHOD.
           DISPLAY "NR-NEVER".
       END METHOD NORET.
       END OBJECT.
       END CLASS G2EXI03C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2EXI03C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE G2EXI03C.
       01 W PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2EXI03C "NEW" RETURNING C
           INVOKE C "GETV" RETURNING W
           DISPLAY "M=" W
           INVOKE C "GETW" RETURNING W
           DISPLAY "W=" W
           INVOKE C "NORET"
           DISPLAY "NR-BACK"
           STOP RUN.
       END PROGRAM G2EXI03.
