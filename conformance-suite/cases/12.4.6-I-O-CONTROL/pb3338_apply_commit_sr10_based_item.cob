*>--- conformance-case
*> id: pb3338_apply_commit_sr10_based_item
*> rules: [SR-12.4.6.3.3-10]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR10: "No data items referenced by the FILE STATUS clause, the LINAGE clause, the RECORD clause, or separately specified in the APPLY COMMIT clause shall be subject to a BASED clause."
*> The shape: identifier-1 is a BASED item. The clause is accepted for a RELATIVE file, so the only refusal this program can
*> draw is SR10's own, by name (COBOLNET4709). Fails if the clause is accepted or refused for another reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338SA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338sa.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-B.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 WS-B PIC X(10) BASED.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
