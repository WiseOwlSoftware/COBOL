*>--- conformance-case
*> id: pb3237_delete_file_handled_warning_by_when_phrase_skips_not_on_exception
*> rules: [GR-14.9.10.4-14, GR-14.6.13.1.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3237 - ISO 14.9.10.4 GR14: DELETE FILE of an absent file is SUCCESSFUL and sets '05'
      *>   (EC-I-O-WARNING, 9.1.13.1). The exception-checking PERFORM's WHEN EC-I-O-WARNING handles
      *>   it (14.6.13.1.4 2)): the WHEN runs, the PERFORM body continues (IN-BODY), and "If a
      *>   conditional phrase with a NOT phrase is specified in the interrupted statement, the
      *>   imperative-statement in that phrase is also not executed."
      *>   Each leg can fail: the NOT ON EXCEPTION line appearing is the old gate-less rendering.
      *>   cite.py: OK 14.6.13.1.4 2); OK 14.9.10.4 14)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3237B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F1 ASSIGN TO "pb3237b-absent.dat"
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               DELETE FILE F1
                   ON EXCEPTION DISPLAY "ON EXCEPTION"
                   NOT ON EXCEPTION DISPLAY "NOT ON EXCEPTION RAN FS=" FS1
               END-DELETE
               DISPLAY "IN-BODY"
           WHEN EC-I-O-WARNING
               DISPLAY "WHEN " FS1
           END-PERFORM
           DISPLAY "AFTER DELETE FS=" FS1
           STOP RUN.
