      *>--- conformance-case
      *> id: pb5612_nested_sort_in_input_procedure_fatal_2002
      *> rules: [2002-GR-14.8.36.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5612 (VCR row 7.80). §2002:14.8.36.3 GR9 (cite.py --check): "If the range of the input procedure
      *> causes the execution of any MERGE, RETURN, or format 1 SORT statements, the EC-SORT-MERGE-ACTIVE exception
      *> condition is set to exist". EC-SORT-MERGE-ACTIVE is Fatal (Table 14), checking is on and no USE procedure
      *> applies: §2002:14.5.12.1.2 4) (cite.py --check): "If checking for the exception condition is enabled,
      *> execution of the run unit is terminated abnormally". GR9 leaves the SORT's results undefined, not the fatal
      *> procedure. Expected: "BEFORE-INNER", then the run unit ends at the inner SORT (stderr and exit status are not
      *> compared): "AFTER-INNER", "O=B" and "AFTER-OUTER" are never displayed.
       >>TURN EC-SORT-MERGE-ACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612NS2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb5612ns2-w.srt".
           SELECT SW2 ASSIGN TO "pb5612ns2-v.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR PIC X.
       SD SW2.
       01 SR2 PIC X.
       WORKING-STORAGE SECTION.
       01 EOFF PIC X.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           SORT SW ASCENDING KEY SR
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "AFTER-OUTER".
           STOP RUN.
       IN-P SECTION.
       IN-1.
           MOVE "B" TO SR.
           RELEASE SR.
           DISPLAY "BEFORE-INNER".
           SORT SW2 ASCENDING KEY SR2
               INPUT PROCEDURE IS IN2
               OUTPUT PROCEDURE IS OUT2.
           DISPLAY "AFTER-INNER".
       OUT-P SECTION.
       OUT-1.
           RETURN SW AT END DISPLAY "EOF" NOT AT END DISPLAY "O=" SR
           END-RETURN.
       IN2 SECTION.
       IN2-1.
           MOVE "Q" TO SR2.
           RELEASE SR2.
       OUT2 SECTION.
       OUT2-1.
           RETURN SW2 AT END DISPLAY "EOF2" NOT AT END DISPLAY "O2=" SR2
           END-RETURN.
