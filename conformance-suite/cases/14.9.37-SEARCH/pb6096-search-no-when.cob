*>--- conformance-case
*> id: pb6096-search-no-when
*> rules: [FMT-14.9.37.2, 1985-FMT-VI.6.22.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6096. §2023:14.9.37.2 Format 1 prints `{ WHEN condition-1 { imperative-statement-2 | NEXT SENTENCE } } ...`
*> with the WHEN brace unbracketed, and §1985:VI.6.22.2 prints it the same way: a SEARCH carries at least one WHEN
*> phrase. This SEARCH has an AT END phrase and then END-SEARCH, so it is refused at every edition. WHAT THIS PINS: the
*> refusal names the missing WHEN phrase (COBOL2269, 'with no WHEN phrase'), not the bare COBOL0001 'unexpected END-SEARCH'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6096SW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K1 INDEXED BY IX.
              10 K1 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH E AT END DISPLAY "X"
           END-SEARCH.
           STOP RUN.
