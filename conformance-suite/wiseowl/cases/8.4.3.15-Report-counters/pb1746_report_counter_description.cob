      *>--- conformance-case
      *> id: pb1746_report_counter_description
      *> rules: [GR-8.4.3.15.4-1, SR-8.4.3.15.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1746 - THE REPORT COUNTERS' IMPLICIT DESCRIPTION IS GNUCOBOL'S.
      *> ISO/IEC 1989:2023 8.4.3.15.4 GR1: "PAGE-COUNTER and LINE-COUNTER reference
      *> temporary unsigned integer data items of class and category numeric, which are
      *> maintained for each report." The standard names neither a size nor a usage, so
      *> CLAUDE.md rule 1 takes the next authority, GnuCOBOL: cobc 3.2.0 declares both
      *> registers USAGE UNSIGNED-INT (cobc/tree.c build_report), an unsigned 32-bit
      *> binary integer whose DISPLAY image is 10 digits. Before this they were PIC 9(18)
      *> USAGE DISPLAY, an implementor choice made without the survey (DISPLAY printed 18
      *> digits). docs/CONFORMANCE.md section 3, "the counter registers' declared
      *> description", records the determination.
      *>
      *> What each line below pins (the expected values are the 10-digit image of the
      *> counter's value, the leftmost-4 truncation 14.9.25.4 GR6 a) gives a PIC X(4)
      *> receiver, and the 32-bit store of a PAGE-COUNTER assignment):
      *>   PC            PAGE-COUNTER is 1 after INITIATE (14.9.21.4)      -> 0000000001
      *>   LC            LINE-COUNTER is the line just printed, FIRST DETAIL 2 -> 0000000002
      *>   X4 / X14      MOVE to an alphanumeric item keeps the image's leftmost characters
      *>   N12           MOVE to a PIC 9(12) item keeps the VALUE, not the image's width
      *>   WRAP          MOVE 5000000000 TO PAGE-COUNTER stores the 32-bit value
      *>                 5000000000 - 4294967296 = 705032704 (cobc 3.2.0 prints the same)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1746DSC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb1746dsc.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 X4   PIC X(4).
       01 X14  PIC X(14).
       01 N12  PIC 9(12).
       REPORT SECTION.
       RD R1 PAGE LIMIT 10 LINES HEADING 1 FIRST DETAIL 2
             LAST DETAIL 8 FOOTING 9.
       01 D1 TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPTF.
           INITIATE R1.
           GENERATE D1.
           DISPLAY "PC=[" PAGE-COUNTER OF R1 "]".
           DISPLAY "LC=[" LINE-COUNTER OF R1 "]".
           MOVE PAGE-COUNTER TO X4.
           DISPLAY "PC-X4=[" X4 "]".
           MOVE LINE-COUNTER TO X14.
           DISPLAY "LC-X14=[" X14 "]".
           MOVE LINE-COUNTER TO N12.
           DISPLAY "LC-N12=[" N12 "]".
           MOVE 5000000000 TO PAGE-COUNTER.
           DISPLAY "WRAP=[" PAGE-COUNTER "]".
           TERMINATE R1.
           CLOSE RPTF.
           STOP RUN.
