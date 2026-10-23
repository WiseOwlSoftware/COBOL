*>--- conformance-case
*> id: pb3338_apply_commit_gr6_set_address
*> rules: [GR-12.4.6.3.4-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.4 GR6: "If an attempt is made to change the address of a data item containing a
*> data-item subject to an APPLY COMMIT clause, the EC-FLOW-APPLY-COMMIT exception condition is set to exist. The
*> ability to detect this is processor dependent." The determination (docs/CONFORMANCE.md section 5, A.4.3): no
*> statement can make that attempt. The SET statement's address form requires a BASED item, ISO 14.9.39.3 SR18:
*> "Data-name-1 shall be a based data item." (COBOLNET0869), and ISO 12.4.6.3.3 SR10 keeps a BASED item out of the
*> clause, so an item the clause names can never be rebased. The shape: a LINKAGE item named in the clause, rebased
*> by SET ADDRESS OF. Fails if the SET compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338G6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON LK-ITEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC X(4) VALUE "BBBB".
       LINKAGE SECTION.
       01 LK-ITEM PIC X(4).
       PROCEDURE DIVISION USING LK-ITEM.
           SET ADDRESS OF LK-ITEM TO ADDRESS OF WS-B
           DISPLAY "UNREACHABLE"
           GOBACK.
