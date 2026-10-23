*>--- conformance-case
*> id: pb3338_apply_commit_sr7_object_reference
*> rules: [SR-12.4.6.3.3-7]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR7: "APPLY COMMIT clauses shall not include data items containing object-reference data items."
*> The shape: identifier-1 is a level-01 universal object reference. The clause is accepted for a RELATIVE file, so the only refusal this program can
*> draw is SR7's own, by name (COBOLNET4709). Fails if the clause is accepted or refused for another reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338S7.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338s7.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-OBJ.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 WS-OBJ USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
