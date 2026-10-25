*>--- conformance-case
*> id: pb1319-sign-group-comp-85
*> rules: [SR-13.18.52.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1319 - COBOL-85 requires "The numeric data description entries to which the SIGN clause applies"
      *> to be "described ... as USAGE IS DISPLAY" (ANSI X3.23-1985 as the vendor manuals transcribe it). H's USAGE
      *> COMP makes its signed subordinate GB binary, and the group's SIGN clause applies to GB, so at --std 85 the
      *> clause is refused with COBOLNET0900. The 2023 text of 13.18.52.3 SR2 speaks only of "an elementary item for
      *> which the SIGN clause is specified" (cite.py: OK  13.18.52.3 2)), and the derived edge is 2002 (VCR row
      *> 7.40); the positive is 2002/pb1319_sign_group_subjects_2002. This subject stood as row H of the 85 golden
      *> pb537_sign_clause_subjects until PB1319.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1319C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H USAGE COMP SIGN IS LEADING.
          05 GB PIC S9(4) VALUE -3.
       PROCEDURE DIVISION.
           DISPLAY GB
           STOP RUN.
