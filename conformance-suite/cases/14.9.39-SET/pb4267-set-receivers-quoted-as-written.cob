*>--- conformance-case
*> id: pb4267-set-receivers-quoted-as-written
*> rules: [FMT-14.9.39.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4267 - ISO 14.9.39.2: no SET general format admits a receiving list of an alphanumeric item and an integer
*> item with UP BY, so the statement is refused (COBOL2112), and the diagnostic quotes each receiver as the program
*> wrote it, qualifiers and spaces included: 'A OF G' 'N OF G'. Fails if the statement is accepted, or a receiver is
*> quoted glued ('AOFG').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4267S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X.
          05 N PIC 9.
       PROCEDURE DIVISION.
           SET A OF G N OF G UP BY 1
           STOP RUN.
