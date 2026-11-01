*>--- conformance-case
*> id: pb4335-goback-returning-below-2002
*> rules: [GR-14.9.18.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4335. GOBACK is a COBOL-2002 introduction and its RETURNING phrase is an implementor extension
      *> gated at the statement's own introduction, so the main-program exit-status use of the extension
      *> (positive control: 2002/pb4335_goback_returning_no_header_item.cob) has no COBOL-85 spelling at all.
      *> The compiler shall say so rather than accept it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4335.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RC PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           GOBACK RETURNING RC.
