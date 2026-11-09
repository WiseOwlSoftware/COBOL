*>--- conformance-case
*> id: pb5025_files_named_apply_commit
*> rules: [GR-12.4.6.4.4-2]
*> accept: [2002, 2014]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5025 - below COBOL-2023, APPLY and COMMIT are user-defined words: neither is in the ISO/IEC 1989:2002
*> or 2014 reserved-word list, APPLY is a COBOL-2023 context-sensitive word (ISO 2023 8.10, "APPLY | I-O-CONTROL
*> paragraph") and COMMIT a word 2023 reserves. The grammar spells the 2023 APPLY COMMIT clause at every edition and
*> the conformance pass refuses it below 2023 by its edition, so this program pins that two files NAMED APPLY and
*> COMMIT, written in the one I-O-CONTROL clause where the clause's spelling `APPLY COMMIT` appears verbatim, keep
*> their reading as file-names: `SAME RECORD AREA FOR APPLY COMMIT.` is a SAME clause naming two files.
*> It fails if APPLY COMMIT is read as the 2023 clause (a COBOL0900 refusal, or a SAME clause of one file).
*>
*> The values come from ISO 2023 12.4.6.4.4 GR2: "A record-area format SAME clause specifies that two or more files
*> referenced by file-name-1, file-name-2 are to share a memory area for processing the current logical record ...
*> This is equivalent to an implicit redefinition of the area with records aligned on the leftmost byte position."
*> With both files open, a MOVE to APPLY's record is the content of COMMIT's record: C=PB5025 on both lines, and
*> the record WRITTEN through COMMIT (that shared content) is the one COMMIT's file READs back: READ C=PB5025.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5025AC.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT APPLY ASSIGN TO "pb5025ac-a.dat"
        ORGANIZATION IS SEQUENTIAL.
    SELECT COMMIT ASSIGN TO "pb5025ac-c.dat"
        ORGANIZATION IS SEQUENTIAL.
I-O-CONTROL.
    SAME RECORD AREA FOR APPLY COMMIT.
DATA DIVISION.
FILE SECTION.
FD APPLY.
01 APPLY-REC PIC X(6).
FD COMMIT.
01 COMMIT-REC PIC X(6).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT APPLY
    OPEN OUTPUT COMMIT
    MOVE "PB5025" TO APPLY-REC
    DISPLAY "A=" APPLY-REC " C=" COMMIT-REC
    WRITE COMMIT-REC
    CLOSE APPLY
    CLOSE COMMIT
    OPEN INPUT COMMIT
    MOVE SPACES TO COMMIT-REC
    READ COMMIT AT END DISPLAY "EOF" END-READ
    DISPLAY "READ C=" COMMIT-REC
    CLOSE COMMIT
    STOP RUN.
