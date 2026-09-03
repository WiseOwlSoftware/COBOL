*> reject-at: 2023
*> ISO 12.4.6.3 APPLY COMMIT clause - Annex A.4.3 item 2, the DECLINED commit-and-rollback facility's
*> declaration half and the owner of 17 of the module's 25 solely-conditioned rules. Before COBOLNET1709
*> the word APPLY was swallowed by the I-O-CONTROL genericClause and the parse then died on COMMIT with an
*> unnamed error. Refusing the clause is what keeps the rest of the decline coherent: with no clause ever
*> accepted, no APPLY COMMIT clause is ever ACTIVE, which is the state 14.9.7.4 GR1 / 14.9.36.4 GR1 give
*> COMMIT and ROLLBACK their CONTINUE behaviour in (pinned by conformance:2023/pb137_commit_inert).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLAPPLY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "dclapply.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
