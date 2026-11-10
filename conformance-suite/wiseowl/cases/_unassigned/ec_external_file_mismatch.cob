*>--- conformance-case
*> id: ec_external_file_mismatch
*> rules: []
*> accept: [2023]
*> files: [ec_external_file_mismatch_xfmsub.cbl]
*> build: [ec_external_file_mismatch_xfmsub.cbl, "{self}"]
*> source-format: free
*> portable: unverified
*>---
      *> EC-EXTERNAL-FILE-MISMATCH (ISO §2023:14.8.4.4 / §2023:12.4.5.3 GR1(e); raise point
      *> §2023:14.9.4.4 GR3e; VCR 15). MAIN's file control entry for external file XFM-F is
      *> ORGANIZATION SEQUENTIAL; the CALLed sub's corresponding entry is ORGANIZATION
      *> RELATIVE — GR1(e) requires the same organization for every file control entry
      *> referencing an external file connector in the run unit. Checking enabled in both
      *> elements; at the CALL the activated element's registration detects the entry
      *> conflict, the Fatal condition is set, the call is not successful (GR3h #1 — the
      *> ON EXCEPTION phrase), and the sub never runs. No OPEN is needed: the §2023:14.8.4.4
      *> check is an activation-time conformance check, not an I-O operation.
*> Each called program is its own compilation unit (the build
*> steps): within ONE compilation group the entries' disagreement
*> is refused at compile time (COBOL6847, kb/Work PB5069), and
*> EC-EXTERNAL-FILE-MISMATCH is the check between separately
*> compiled runtime elements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFMMAIN.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XFM-F ASSIGN "xfm-data".
       DATA DIVISION.
       FILE SECTION.
       FD XFM-F IS EXTERNAL.
       01 XFM-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "XFMSUB"
               ON EXCEPTION
                   DISPLAY "SUB=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "NO-CONDITION"
           END-CALL
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM XFMMAIN.
