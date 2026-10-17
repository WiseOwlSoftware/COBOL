*>--- conformance-case
*> id: pb3237_delete_file_unhandled_warning_runs_not_on_exception
*> rules: [GR-14.9.10.4-14, GR-14.6.13.1.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3237 control - ISO 14.9.10.4 GR14: DELETE FILE of an absent file is SUCCESSFUL ('05').
      *>   With EC-I-O-WARNING checking on but NO handler (no declarative, no WHEN), 14.6.13.1.4 4):
      *>   "Execution of the statement continues as specified in the rules for that statement" -
      *>   a successful completion, so NOT ON EXCEPTION runs and ON EXCEPTION does not. The gate
      *>   asks "was it handled", not "was it raised".
      *>   cite.py: OK 14.6.13.1.4 4); OK 14.9.10.4 14)
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3237C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F1 ASSIGN TO "pb3237c-absent.dat"
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           DELETE FILE F1
               ON EXCEPTION DISPLAY "ON EXCEPTION"
               NOT ON EXCEPTION DISPLAY "NOT ON EXCEPTION RAN FS=" FS1
           END-DELETE
           DISPLAY "AFTER DELETE FS=" FS1
           STOP RUN.
