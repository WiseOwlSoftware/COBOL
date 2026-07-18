      *> ISO §14.9.18.2 / §14.9.18.4 GR3 — GOBACK … WITH {ERROR|NORMAL} STATUS [id|lit] (COBOL-2023: annex
      *> item 32 adds to GOBACK the same status phrase STOP allows). In a main program a GOBACK operates as a
      *> STOP statement carrying the status phrase (GR3), so the run unit terminates normally here. This slice
      *> parses + 2023-introduction-gates the phrase and binds it presence-only (matching the STOP sibling); the
      *> status VALUE → exit-code wiring is the shared STOP+GOBACK termination-status slice, so the golden keeps
      *> to NORMAL STATUS 0 (whose normal/0 termination coincides with the current default exit behavior).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBSTAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  RC  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "STATUS PHRASE OK".
           GOBACK WITH NORMAL STATUS RC.
