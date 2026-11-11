      *>--- conformance-case
      *> id: pb5240_call_function_program_pointer
      *> rules: [SR-14.9.4.3-1, GR-14.9.4.4-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> CALL identifier-1 where identifier-1 is a user-defined
      *> function-identifier whose RETURNING item is USAGE PROGRAM-
      *> POINTER (kb/Work PB5240, review R1-1). The result temporary
      *> has the returning item's category (8.4.3.2.4 GR1), so it is
      *> the program-pointer data item 14.9.4.3 SR1 admits, and
      *> 14.9.4.4 GR3 b) makes the program called the one whose
      *> location it holds. GETP returns a pointer to PCFPB, never to
      *> a program named by its image: the call reaches PCFPB once per
      *> CALL, and its WORKING-STORAGE count shows 1 then 2.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PCFPGETP.
       DATA DIVISION.
       LINKAGE SECTION.
       01  RP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION RETURNING RP.
           SET RP TO ENTRY "PCFPB".
           GOBACK.
       END FUNCTION PCFPGETP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PCFPMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PCFPGETP.
       PROCEDURE DIVISION.
           CALL FUNCTION PCFPGETP.
           CALL FUNCTION PCFPGETP.
           DISPLAY "PCFP DONE".
           STOP RUN.
       END PROGRAM PCFPMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PCFPB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       01  CNT-X REDEFINES CNT PIC X.
       PROCEDURE DIVISION.
           ADD 1 TO CNT.
           DISPLAY "PCFPB CALL " CNT-X.
           GOBACK.
       END PROGRAM PCFPB.
