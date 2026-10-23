*>--- conformance-case
*> id: pb3340_sequential_file_under_apply_commit
*> rules: [GR-9.1.18.2-p1, GR-9.1.18.3-p1, GR-9.1.18.4-p1, GR-14.9.7.4-3, GR-14.9.36.4-3, GR-12.4.6.3.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3340 - a SEQUENTIAL file (record sequential, and line sequential) under an APPLY COMMIT clause.
*> ISO 9.1.18.2: "Apart from sort-merge files and report writer files, any type of file on a mass storage device may be
*> specified." ISO 14.9.7.4 GR3: "The execution of the COMMIT statement permanently applies any changes made to the files
*> specified in all the active APPLY COMMIT clauses". ISO 14.9.36.4 GR3: "All files and data-items referenced in active
*> APPLY COMMIT clauses are restored to the state they were in at the execution of the last COMMIT statement or the start
*> of the run unit if no COMMIT statements had previously been executed." "It also includes the file status data items".
*> ISO 14.9.10.4, the NOTE under GR15: "Such files can still be deleted, however if they are, then in the event of a
*> rollback they will be restored". ISO 14.9.27.4 GR18: "the successful execution of the OPEN statement creates the file",
*> so a file created since the last COMMIT is absent again after a ROLLBACK (the state then was "no file"); G is OPTIONAL,
*> so its OPEN INPUT then answers '05' (ISO 9.1.13.2 item 4 a)), a successful OPEN - a '35' would be a fatal condition on a
*> file subject to the clause, whose item 8 ROLLBACK (ISO 14.6.13.1.3) would restore the FILE STATUS item it was told in.
*> Each leg can fail on its own: an append a ROLLBACK keeps (C1 C2 X1), a rewritten record it keeps (Y1 C2), an OPEN
*> OUTPUT it keeps (Z1 alone), a DELETE FILE it keeps (status 35 at the reopen), a created file it keeps (status 00 for G
*> instead of 05), a line sequential append it keeps (3 lines), and a COMMIT that did not keep its change (no X2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3340SQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3340f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT OPTIONAL G ASSIGN TO "pb3340g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GS.
           SELECT L ASSIGN TO "pb3340l.txt"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS LS.
       I-O-CONTROL.
           APPLY COMMIT ON F G L.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X(2).
       FD G.
       01 GREC PIC X(2).
       FD L.
       01 LREC PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 GS PIC XX.
       01 LS PIC XX.
       01 N PIC 9.
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           WRITE FREC FROM "C1"
           WRITE FREC FROM "C2"
           CLOSE F
           OPEN OUTPUT L
           WRITE LREC FROM "L1"
           WRITE LREC FROM "L2"
           CLOSE L
           COMMIT
           PERFORM SHOW-F
      *> An append undone: the file is truncated to its length at the COMMIT.
           OPEN EXTEND F
           WRITE FREC FROM "X1"
           CLOSE F
           ROLLBACK
           PERFORM SHOW-F
      *> A record rewritten in place, undone.
           OPEN I-O F
           READ F
           MOVE "Y1" TO FREC
           REWRITE FREC
           CLOSE F
           ROLLBACK
           PERFORM SHOW-F
      *> An append committed.
           OPEN EXTEND F
           WRITE FREC FROM "X2"
           CLOSE F
           COMMIT
           PERFORM SHOW-F
      *> An OPEN OUTPUT of the existing file, undone: the file as it was.
           OPEN OUTPUT F
           WRITE FREC FROM "Z1"
           CLOSE F
           ROLLBACK
           PERFORM SHOW-F
      *> A DELETE FILE after a CLOSE, undone (the NOTE under 14.9.10.4 GR15).
           OPEN INPUT F
           CLOSE F
           DELETE FILE F
           DISPLAY "DELETE FILE F " FS
           ROLLBACK
           PERFORM SHOW-F
      *> A creation undone: the file is absent again.
           OPEN OUTPUT G
           WRITE GREC FROM "G1"
           CLOSE G
           ROLLBACK
           OPEN INPUT G
           DISPLAY "OPEN INPUT G AFTER ROLLBACK " GS
      *> A line sequential append undone.
           OPEN EXTEND L
           WRITE LREC FROM "L3"
           CLOSE L
           ROLLBACK
           MOVE 0 TO N
           OPEN INPUT L
           PERFORM UNTIL LS NOT = "00"
               READ L
                   AT END CONTINUE
                   NOT AT END ADD 1 TO N
               END-READ
           END-PERFORM
           CLOSE L
           DISPLAY "L RECORDS " N
           STOP RUN.
       SHOW-F.
           OPEN INPUT F
           IF FS NOT = "00"
               DISPLAY "OPEN INPUT F " FS
           ELSE
               DISPLAY "F:" WITH NO ADVANCING
               PERFORM UNTIL FS NOT = "00"
                   READ F
                       AT END DISPLAY " END"
                       NOT AT END DISPLAY " " FREC WITH NO ADVANCING
                   END-READ
               END-PERFORM
               CLOSE F
           END-IF.
