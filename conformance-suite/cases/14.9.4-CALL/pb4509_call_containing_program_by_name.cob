      *>--- conformance-case
      *> id: pb4509_call_containing_program_by_name
      *> rules: [FMT-14.9.4.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4509 - CALL Format 2 with the bracket omitted:
      *> "CALL program-prototype-name-1", spelled like identifier-1.
      *> 8.4.6.8 (cite.py --check 8.4.6.8 -> OK): a program-prototype-
      *> name referenced in a source element is "the program-name of a
      *> containing program definition" or one REPOSITORY declares, so
      *> the nested PPNB may CALL PPNA by its name with no REPOSITORY
      *> paragraph. 14.9.4.4 GR3 (OK 14.9.4.4 3)): "If neither
      *> identifier-1 nor literal-1 is specified, program-prototype-
      *> name-1 determines the externalized program-name of the program
      *> being called". ISO/IEC 1989:2002 introduced program-prototype-
      *> names (its 8.4.5.7 carries the same sentence); at COBOL-85 the
      *> same source is refused (pb4509-call-containing-name-85).
      *> DERIVATION:
      *>   PPNA (RECURSIVE, 11.10.4 GR4) runs first: N=1, so it CALLs
      *>     the nested PPNB.
      *>   PPNB displays IN PPNB and CALLs PPNA by name: a second
      *>     activation of PPNA. N is working storage, shared by both
      *>     activations: N=2, so PPNA displays PPNA N=2 and returns.
      *>   PPNB displays PPNB DONE and returns; the first PPNA
      *>     activation displays PPNA END N=2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPNA IS RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO N
           DISPLAY "PPNA N=" N
           IF N = 1
               CALL "PPNB"
               DISPLAY "PPNA END N=" N
           END-IF
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPNB.
       PROCEDURE DIVISION.
       P.
           DISPLAY "IN PPNB"
           CALL PPNA
           DISPLAY "PPNB DONE"
           GOBACK.
       END PROGRAM PPNB.
       END PROGRAM PPNA.
