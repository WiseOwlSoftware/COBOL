*>--- conformance-case
*> id: pb6096-search-all-no-when
*> rules: [FMT-14.9.37.2, 1985-FMT-VI.6.22.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6096. §2023:14.9.37.2 Format 2 prints exactly one WHEN phrase, required (§1985:VI.6.22.2 likewise). A SEARCH ALL
*> whose sentence ends after the AT END phrase is refused at every edition. WHAT THIS PINS: the refusal names the missing
*> WHEN phrase (COBOL2269, 'with no WHEN phrase'), not a DISPLAY statement that 'ends before this token'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6096SAW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K1 INDEXED BY IX.
              10 K1 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E AT END DISPLAY "X".
           STOP RUN.
