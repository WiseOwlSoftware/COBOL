      *>--- conformance-case
      *> id: pb4237-debug-all-identifier-removed-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The REFERENCES-OF-less spelling of the X3.23-1985 debug operand (ALL identifier-1, ALL OF
      *> identifier-1; kb/Work PB4237) at an edition that deleted the facility: ISO/IEC 1989:2002 removed
      *> USE FOR DEBUGGING and ISO/IEC 1989:2023 prints no debug format (VCR Table 7 row 7.17), so from
      *> 2002 the declarative draws COBOL0902 (use-for-debugging-removed-2002) whichever spelling its
      *> operand takes. Why it can fail: a parse that refused the short spelling would answer COBOL5506
      *> instead of naming the removed facility. Accepted at 85: pb4237_debug_all_identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4237DBGRM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A   PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-1 SECTION.
           USE FOR DEBUGGING ON ALL W-A  ALL OF W-A.
       DBG-1-PARA.
           CONTINUE.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           MOVE 7 TO W-A.
           STOP RUN.
