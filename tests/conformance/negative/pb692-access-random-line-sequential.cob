*> reject-at: 2002 2014 2023
*> ISO 1989:2023 12.4.5.5.2 syntax rule 2 on the OTHER sequential organization. That a LINE
*> SEQUENTIAL file IS "a sequential file" in SR2's sense is DERIVED, not assumed: 12.4.5.10.3
*> general rule 2 puts the LINE SEQUENTIAL phrase in the ORGANIZATION clause, that clause is
*> written only in the Format 3 file control entry, and 12.4.5.2 syntax rule 11 says "Format 3
*> shall be specified only for a sequential file or a report file" -- so RANDOM is closed out
*> of it exactly as it is for record sequential (and Format 3's own general format admits only
*> [ ACCESS MODE IS SEQUENTIAL ]).
*> reject-at names 2002 2014 2023, not 85: ANSI X3.23-1985 has no line sequential organization
*> at all, so the 1985 verdict on this source belongs to the edition gate for the organization
*> (kb/Work PB688), not to this rule. COBOLNET1858 (kb/Work PB692).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB692N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb692n4.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               ACCESS MODE IS RANDOM.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           CLOSE F.
           STOP RUN.
