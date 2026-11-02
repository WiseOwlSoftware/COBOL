*>--- conformance-case
*> id: pb4219_apply_commit_own_condition_name_beats_container_global_file
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4219 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source element B is
*> the referenced item." PB4219D declares the file RELF IS GLOBAL; PB4219E, contained in it, declares its own
*> condition-name RELF and writes APPLY COMMIT ON RELF. PB4219E's condition-name is the referenced item, and a
*> condition-name is not a data item, so the clause's identifier-1 references no data item (COBOL1639, "is declared as a
*> condition-name"). Fails if the clause is accepted, or refused under SR11 about the container's file (the farther
*> declaration).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb4219d.dat".
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS GLOBAL.
       01 RR PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X.
          88 RELF VALUE "A".
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB4219E.
       END PROGRAM PB4219D.
