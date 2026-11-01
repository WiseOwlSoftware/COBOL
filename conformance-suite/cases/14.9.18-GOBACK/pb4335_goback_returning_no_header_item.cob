*>--- conformance-case
*> id: pb4335_goback_returning_no_header_item
*> rules: [GR-14.9.18.4-2, GR-14.9.18.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4335. GOBACK RETURNING is an IMPLEMENTOR EXTENSION (ISO 14.9.18.2 has no RETURNING phrase) and
      *> 14.9.18.4 GR2's activation result exists only "If a RETURNING phrase is specified in the procedure
      *> division header of the program containing the GOBACK statement". Here NEITHER program has a header
      *> RETURNING item, so there is no result to hand over: a called program's GOBACK RETURNING is inert (the
      *> activator just continues, GR2) and a main program's operates as STOP (GR3) whose exit status is the
      *> operand (the extension's GnuCOBOL / Micro Focus meaning). This case used to compile clean and die at
      *> run time with NotImplementedCobolFeatureException in BOTH programs. The golden harness checks only
      *> ExitCode == 0, so the operand is 0 here; the nonzero exit codes are asserted by StopGobackExitCodeTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4335MAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RC PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "MAIN BEFORE".
           CALL "PB4335SUB".
           DISPLAY "MAIN AFTER".
           GOBACK RETURNING RC.
       END PROGRAM PB4335MAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4335SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SUB-CODE PIC 9 VALUE 4.
       PROCEDURE DIVISION.
       P.
           DISPLAY "SUB".
           GOBACK RETURNING SUB-CODE.
       END PROGRAM PB4335SUB.
