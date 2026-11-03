      *>--- conformance-case
      *> id: pb4305-inline-perform-after-at-85
      *> rules: [FMT-14.9.28.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_inline_perform_after_2002. COBOL-85 allowed no AFTER
      *>   phrase in an inline PERFORM (2002 Annex F.2 item 91).
      *>   Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NIP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9.
       01 J PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                   AFTER J FROM 1 BY 1 UNTIL J > 2
               DISPLAY I J
           END-PERFORM
           STOP RUN.
