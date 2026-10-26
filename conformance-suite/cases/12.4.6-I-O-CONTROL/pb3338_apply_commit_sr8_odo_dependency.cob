*>--- conformance-case
*> id: pb3338_apply_commit_sr8_odo_dependency
*> rules: [SR-12.4.6.3.3-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR8: "the 01 or 77 level data items that contain or refer to them shall each either or neither be specified in the list of identifiers"
*> The shape: WS-T (OCCURS DEPENDING ON WS-N) named without WS-N, the 01 item it depends on. The clause is accepted for a RELATIVE file, so the only refusal this program can
*> draw is SR8's own, by name (COBOL4709). Fails if the clause is accepted or refused for another reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338S8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338s8.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-T.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 WS-N PIC 9 VALUE 1.
       01 WS-T.
           05 WS-E PIC X OCCURS 1 TO 5 DEPENDING ON WS-N.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
