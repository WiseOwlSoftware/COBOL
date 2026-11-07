      *>--- conformance-case
      *> id: pb4854-debug-odo-count-removed-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4854, refused at the later editions: ISO/IEC 1989:2002 deleted the USE FOR DEBUGGING facility (COBOL0902), so the
      *> program of pb4854-debug-odo-count-is-implicit is accepted at --std 85 only. X3.23-1985 USE FOR DEBUGGING general rule 5: an
      *> ALL REFERENCES OF section runs "for every statement that
      *> explicitly references identifier-1". A table whose OCCURS
      *> clause has a DEPENDING ON item makes the statement READ that
      *> item for the table's current size, but the program wrote only
      *> the table, so the item is no explicit reference.
      *> A: INITIALIZE TB (the group holding the table E) references TB
      *> and not N, the item that sets E's size, and not E (rule 9: a
      *> group reference does not reference its subordinates).
      *> B: SEARCH E scans E for the size N gives it; its WHEN phrase
      *> writes E, so the section for E runs after the statement, and
      *> the section for N does not.
      *> Why each leg can fail: a reach walk that counts every operand a
      *> lowered node holds runs the section for N on both statements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4854R2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 3.
       01 TB.
          02 E PIC 9 OCCURS 1 TO 5 DEPENDING ON N INDEXED BY XI.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBN SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF N.
       DBN-B.
           DISPLAY "  DBN".
       DBT SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF TB.
       DBT-B.
           DISPLAY "  DBT".
       DBE SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF E.
       DBE-B.
           DISPLAY "  DBE".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "A: INITIALIZE TB".
           INITIALIZE TB.
           SET XI TO 1.
           DISPLAY "B: SEARCH E".
           SEARCH E AT END DISPLAY "NONE"
              WHEN E (XI) = 9 DISPLAY "HIT".
           DISPLAY "END".
           STOP RUN.
