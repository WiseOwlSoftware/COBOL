*>--- conformance-case
*> id: pb3697_rollback_restores_boolean_item_verbatim
*> rules: [GR-14.9.36.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3697 (branch review R1-2): ROLLBACK restores the data items of an active APPLY COMMIT clause to the state
*> they were in at the last COMMIT, and that state includes the characters ANOTHER DESCRIPTION of the item's storage holds:
*>   cite.py --check 14.9.36.4 "restored to the state they were in at the execution of the last COMMIT statement"
*>   -> OK  14.9.36.4 3)
*> BQ is a boolean item (PIC 1(4)) and XQ redefines its storage as four alphanumeric characters; BN is a USAGE NATIONAL
*> boolean item (PIC 1(4), eight bytes) and XN redefines it as PIC X(8). The determination D-B5 (docs/CONFORMANCE.md)
*> converts the characters of every STORE into a boolean item, but the restore is not a store of characters: it returns
*> the item's storage to the saved image, so the REDEFINES views read the characters they held at the COMMIT.
*> EXPECTED OUTPUT:
*>   L1 XQ was ABCD at the COMMIT, 1111 after it; the ROLLBACK returns ABCD.
*>   L2 XN was ABCDEFGH at the COMMIT, 11111111 after it; the ROLLBACK returns ABCDEFGH.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3697RB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON BQ BN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BQ PIC 1(4).
       01 XQ REDEFINES BQ PIC X(4).
       01 BN PIC 1(4) USAGE NATIONAL.
       01 XN REDEFINES BN PIC X(8).
       PROCEDURE DIVISION.
           MOVE "ABCD" TO XQ
           MOVE "ABCDEFGH" TO XN
           COMMIT
           MOVE "1111" TO XQ
           MOVE "11111111" TO XN
           ROLLBACK
           DISPLAY "L1 " XQ
           DISPLAY "L2 " XN
           STOP RUN.
