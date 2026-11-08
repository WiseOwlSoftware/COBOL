      *>--- conformance-case
      *> id: pb4919_rerun_optional_words_85
      *> rules: [FMT-12.4.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4919 - the X3.23-1985 RERUN clause's optional words.
      *> The printed general format (§1985:VII.2.12.2, page VII-17):
      *>   RERUN [ON {file-name-1 | implementor-name-1}]
      *>     EVERY {{[END OF] {REEL | UNIT} | integer-1 RECORDS}
      *>              OF file-name-2
      *>           | integer-2 CLOCK-UNITS | condition-name-1}
      *> underlines RERUN, ON, END, REEL, UNIT, RECORDS and CLOCK-UNITS
      *> only, so EVERY and both OFs are optional words. The grammar
      *> demanded EVERY, the OF after REEL/UNIT and the OF of END OF:
      *> each leg below omits one or more of them and was COBOL5506.
      *> The Relative and Indexed I-O modules print a subset and point
      *> here (§1985:VIII.2.6.3 GR1). The legs respect the clause's
      *> syntax rules: RECORDS names an implementor-name in ON (SR3),
      *> no file-name-2 repeats within RECORDS or END OF REEL/UNIT
      *> (SR4), and file-name-1 is a sequential file (SR1). A null
      *> rerun facility is conforming (GR2), so the clause has no
      *> program-visible effect.
      *> Expected: the program compiles at 85 and displays its name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4919RR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "P4919R1".
           SELECT F2 ASSIGN TO "P4919R2".
       I-O-CONTROL.
           RERUN ON SYSRR 100 RECORDS F1
           RERUN ON SYSRR EVERY 200 RECORDS OF F2
           RERUN ON F1 EVERY END REEL F2
           RERUN END OF UNIT OF F1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       FD F2.
       01 R2 PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "P4919RR".
           STOP RUN.
