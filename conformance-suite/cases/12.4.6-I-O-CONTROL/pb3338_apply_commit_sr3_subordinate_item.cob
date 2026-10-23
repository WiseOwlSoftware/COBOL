*>--- conformance-case
*> id: pb3338_apply_commit_sr3_subordinate_item
*> rules: [SR-12.4.6.3.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR3: "Multiple instances of Identifier-1 shall represent different non-overlapping 01 or 77 level data-items in the linkage, working-storage and local-storage sections."
*> The shape: identifier-1 is a level-05 item, not a 01 or 77 item. The clause is accepted for a RELATIVE file, so the only refusal this program can
*> draw is SR3's own, by name (COBOLNET4709). Fails if the clause is accepted or refused for another reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338S3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338s3.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-PART.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 WS-GRP.
           05 WS-PART PIC X(4).
           05 WS-REST PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
