      *>--- conformance-case
      *> id: pb4901-call-debug-register-at-85
      *> rules: [SR-14.9.4.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4901. §1985:X.5.2.3 SR3 - "Each of the operands in the
      *> USING phrase must have been defined as a data item in the File
      *> Section, Working-Storage Section, Communication Section, or
      *> Linkage Section" (cite.py --check OK). The '85 DEBUG-ITEM
      *> special register (§1985:XV.3.2.4 GR10, an implicit description)
      *> is defined in no section, so a subordinate of it is no CALL
      *> operand, in either CALL format and in either passing mode: the
      *> refusal names the 1985 rule, not a 2023 rule about a register
      *> that edition lacks. Both CALLs below fail, each for that rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4901A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF A.
       DB1-B.
           CALL "X" USING DEBUG-NAME.
           CALL "X" USING BY CONTENT DEBUG-LINE
               ON EXCEPTION CONTINUE.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY A.
           STOP RUN.
