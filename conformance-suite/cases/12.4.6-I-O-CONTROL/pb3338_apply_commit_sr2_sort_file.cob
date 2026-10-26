*>--- conformance-case
*> id: pb3338_apply_commit_sr2_sort_file
*> rules: [SR-12.4.6.3.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR2: "File-name-1 shall not represent a sort-merge file, nor a file used by report writer."
*> The shape: a sort-merge file (SD SRT) named beside a RELATIVE file. The clause is accepted for a RELATIVE file, so the only refusal this program can
*> draw is SR2's own, by name (COBOL4709). Fails if the clause is accepted or refused for another reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338S2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338s2.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
           SELECT SRT ASSIGN TO "pb3338s2.srt".
       I-O-CONTROL.
           APPLY COMMIT ON RELF SRT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       SD SRT.
       01 SREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).

       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
