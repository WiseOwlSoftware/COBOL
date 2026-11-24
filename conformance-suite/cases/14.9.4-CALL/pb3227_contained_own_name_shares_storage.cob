      *>--- conformance-case
      *> id: pb3227_contained_own_name_shares_storage
      *> rules: [GR-14.9.4.4-9, GR-14.2.3-8]
      *> accept: [2002, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3227, review R1-1. ISO 8.4.6.8: "Program-prototype-names referenced within a source element shall be
      *> either the program-name of a containing program definition or a program-prototype-name declared in the
      *> REPOSITORY paragraph", so INNERP's bare "CALL INNERP" names a program-prototype-name of INNERP itself: Format 2
      *> (14.9.4.2, the AS phrase omitted). GR9 a) 1. passes the keyword-less NUM BY REFERENCE to the BY REFERENCE
      *> formals A and B, whatever BY CONTENT an earlier argument wrote, and 14.2.3 GR8 makes each formal occupy NUM's
      *> storage area: the store through AX is B's value at once, so the second activation displays B: 5678.
      *> Why it can fail: NUM is OUTERP's GLOBAL item, so OUTERP claims its shared storage from its scan of INNERP's
      *> CALLs. That scan judged the target word in OUTERP's prototype scope, which holds OUTERP's name but not INNERP's,
      *> took the CALL for Format 1 (BY CONTENT carried) and claimed nothing; INNERP's CALL bind still passed NUM BY
      *> REFERENCE, so A and B each held a private copy and B displayed the old 1234.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NUM PIC 9(4) VALUE 1234 GLOBAL.
       01 N PIC 9(4) BINARY VALUE 7 GLOBAL.
       PROCEDURE DIVISION.
           CALL "INNERP" USING BY CONTENT N NUM NUM
           DISPLAY "AFTER: " NUM
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERP IS RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DEPTH PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 C PIC 9(4) BINARY.
       01 A PIC 9(4).
       01 AX REDEFINES A PIC X(4).
       01 B PIC 9(4).
       PROCEDURE DIVISION USING C A B.
           ADD 1 TO DEPTH
           IF DEPTH = 1
               CALL INNERP USING BY CONTENT N NUM NUM
               DISPLAY "INNER AFTER: " NUM
           ELSE
               MOVE "5678" TO AX
               DISPLAY "B: " B
           END-IF
           GOBACK.
       END PROGRAM INNERP.
       END PROGRAM OUTERP.
