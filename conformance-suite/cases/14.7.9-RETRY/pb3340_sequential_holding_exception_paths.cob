*>--- conformance-case
*> id: pb3340_sequential_holding_exception_paths
*> rules: [GR-9.1.18.2-p1, GR-14.9.7.4-3, GR-14.7.9.3-4, GR-9.1.13.8-1, GR-9.1.13.8-2, GR-9.1.16-p4, GR-9.1.16-p7, GR-9.1.16-p9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3340 - the exception paths of a SEQUENTIAL file under an APPLY COMMIT clause, met by three connectors of the
*> SAME run unit that no APPLY COMMIT clause names (G, with a USE declarative and a FILE STATUS item; H, with a FILE STATUS
*> item alone; K, with neither; all LOCK MODE IS MANUAL, so their READs lock nothing), all four on one physical file under
*> SHARING WITH ALL OTHER. F's transaction holds the end of the file and the record it appended (docs/CONFORMANCE.md DOC-A.1-152, the circumstance ISO 9.1.16 lets the implementor specify: "The
*> implementor may specify circumstances other than a locked logical record that result in the return of a locked record
*> status.").
*> - G's WRITE answers '51' (ISO 9.1.13.8 1): "I-O status = 51. The input-output statement is unsuccessful due to an attempt
*>   to access a record that is currently locked by another file connector"), and its USE AFTER STANDARD EXCEPTION
*>   declarative runs first (ISO 9.1.13.1: EC-I-O-RECORD-OPERATION).
*> - RETRY 2 TIMES ends with '51' (ISO 14.7.9.3 4), closing paragraph: "Otherwise, the statement is unsuccessful"); the
*>   holder is this run unit's own transaction, which cannot end while the statement runs, so RETRY FOREVER ends with the
*>   deadlock '52' (ISO 9.1.13.8 2), "The implementor shall specify under what conditions a deadlock is detected";
*>   docs/CONFORMANCE.md DOC-A.1-109).
*> - H's READ of the record F appended answers '51' with no declarative: the FILE STATUS item tells it and execution goes
*>   on; a READ of a record F did not change answers '00'. A READ IGNORING LOCK makes the held record available ('00',
*>   ISO 14.9.30.4 GR12), and H's REWRITE of it answers '51': it is still held.
*> - K's WRITE meets the same holding with no FILE STATUS item and no declarative: the condition is nonfatal, so execution
*>   continues (ISO 14.6.13.1.4: "Execution of the statement continues as specified in the rules for that statement") and
*>   K's record is not written.
*> - H's OPEN OUTPUT answers '61' (ISO 9.1.13.9 1)) and its DELETE FILE '62' (ISO 9.1.13.9 2)).
*> - After the COMMIT nothing holds the file (ISO 14.9.7.4 GR3: it "permanently applies any changes made to the files
*>   specified in all the active APPLY COMMIT clauses and releases all record locks associated with those files", and GR4
*>   releases the file locks of a closed file): G's WRITE answers '00', and the file holds C1, F's X2 and G's G2.
*> Each line can fail on its own: a statement that ignored the holding answers '00' (and the file shows its record), a
*> RETRY that waited on its own run unit never ends, and a declarative that did not run omits its "USE G" line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3340EX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3340e.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS FS.
           SELECT G ASSIGN TO "pb3340e.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER
               FILE STATUS IS GS.
           SELECT H ASSIGN TO "pb3340e.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER
               FILE STATUS IS HS.
           SELECT K ASSIGN TO "pb3340e.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X(2).
       FD G.
       01 GREC PIC X(2).
       FD H.
       01 HREC PIC X(2).
       FD K.
       01 KREC PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 GS PIC XX.
       01 HS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       G-ERROR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON G.
       G-SHOW.
           DISPLAY "USE G " GS.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       BEGIN.
           OPEN OUTPUT F
           WRITE FREC FROM "C1"
           CLOSE F
           COMMIT
           OPEN EXTEND F
           WRITE FREC FROM "X2"
           DISPLAY "F WRITE " FS
           OPEN EXTEND G
           WRITE GREC FROM "G2"
           DISPLAY "G WRITE " GS
           WRITE GREC FROM "G2" RETRY 2 TIMES
           DISPLAY "G WRITE RETRY 2 TIMES " GS
           WRITE GREC FROM "G2" RETRY FOREVER
           DISPLAY "G WRITE RETRY FOREVER " GS
           CLOSE G
           OPEN EXTEND K
           WRITE KREC FROM "K2"
           DISPLAY "K WRITE CONTINUES"
           CLOSE K
           OPEN INPUT H
           READ H
           DISPLAY "H READ " HS " " HREC
           READ H
           DISPLAY "H READ " HS
           CLOSE H
           OPEN I-O H
           READ H
           READ H IGNORING LOCK
           DISPLAY "H READ IGNORING LOCK " HS " " HREC
           MOVE "H2" TO HREC
           REWRITE HREC
           DISPLAY "H REWRITE " HS
           CLOSE H
           CLOSE F
           OPEN OUTPUT H
           DISPLAY "H OPEN OUTPUT " HS
           DELETE FILE H
           DISPLAY "H DELETE FILE " HS
           COMMIT
           OPEN EXTEND G
           WRITE GREC FROM "G2"
           DISPLAY "G WRITE AFTER COMMIT " GS
           CLOSE G
           OPEN INPUT H
           DISPLAY "H:" WITH NO ADVANCING
           PERFORM UNTIL HS NOT = "00"
               READ H
                   AT END DISPLAY " END"
                   NOT AT END DISPLAY " " HREC WITH NO ADVANCING
               END-READ
           END-PERFORM
           CLOSE H
           STOP RUN.
