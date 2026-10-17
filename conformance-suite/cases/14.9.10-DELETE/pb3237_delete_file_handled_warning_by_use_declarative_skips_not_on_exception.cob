*>--- conformance-case
*> id: pb3237_delete_file_handled_warning_by_use_declarative_skips_not_on_exception
*> rules: [GR-14.9.10.4-14, GR-14.6.13.1.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3237 - ISO 14.9.10.4 GR14: DELETE FILE of an absent file is SUCCESSFUL and sets '05'
      *>   (EC-I-O-WARNING, 9.1.13.1). With that checking on, 14.6.13.1.4 3): the USE declarative
      *>   runs and "If a conditional phrase with a NOT phrase is specified in the interrupted
      *>   statement, the imperative-statement in that phrase is not executed." So the output is the
      *>   declarative line and the line after the statement - never the NOT ON EXCEPTION line.
      *>   Each leg can fail: the NOT ON EXCEPTION line appearing is the old gate-less rendering;
      *>   the ON EXCEPTION line appearing means '05' was taken for unsuccessful.
      *>   cite.py: OK 14.6.13.1.4 3); OK 14.9.10.4 14)
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3237A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F1 ASSIGN TO "pb3237a-absent.dat"
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-WARNING.
       H-P.
           DISPLAY "DECLARATIVE RAN FS=" FS1.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DELETE FILE F1
               ON EXCEPTION DISPLAY "ON EXCEPTION"
               NOT ON EXCEPTION DISPLAY "NOT ON EXCEPTION RAN FS=" FS1
           END-DELETE
           DISPLAY "AFTER DELETE FS=" FS1
           STOP RUN.
