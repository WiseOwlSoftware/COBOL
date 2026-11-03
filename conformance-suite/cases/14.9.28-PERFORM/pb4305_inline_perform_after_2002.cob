      *>--- conformance-case
      *> id: pb4305_inline_perform_after_2002
      *> rules: [FMT-14.9.28.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - The inline PERFORM format takes the
      *>   varying-phrase whole, AFTER phrases included (2002 Annex F.2
      *>   item 91). I runs 1 to 2 and, for each, J runs 1 to 2.
      *>   Expected: "11", "12", "21", "22". It fails if the AFTER
      *>   phrase is refused or varied in the wrong order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305IP.
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
