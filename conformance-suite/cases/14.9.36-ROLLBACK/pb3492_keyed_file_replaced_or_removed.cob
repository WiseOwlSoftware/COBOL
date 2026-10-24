*>--- conformance-case
*> id: pb3492_keyed_file_replaced_or_removed
*> rules: [GR-9.1.18.4-p1, GR-9.1.18.5-p1, GR-14.9.36.4-3, GR-14.9.7.4-3, GR-14.9.27.4-18, GR-14.9.10.4-15]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3492 - a RELATIVE and an INDEXED file under an APPLY COMMIT clause, each replaced by an OPEN OUTPUT, removed
*> by a DELETE FILE and created, since the last COMMIT. ISO 14.9.36.4 GR3: "All files and data-items referenced in active
*> APPLY COMMIT clauses are restored to the state they were in at the execution of the last COMMIT statement or the start
*> of the run unit if no COMMIT statements had previously been executed." ISO 14.9.27.4 GR18: "After the creation of the
*> file, the file contains no records" - the keyed store cannot undo that from inside the file it replaces, so the file
*> is captured into the run unit's transaction journal first (DESIGN-runtime-library.md 2.2.2.14). ISO 14.9.10.4, the NOTE
*> under GR15: "Such files can still be deleted, however if they are, then in the event of a rollback they will be
*> restored to the state they were in at the last commit" - and GR15's conflict is a file "locked by another file
*> connector", which the connector's own kept handle is not, so the DELETE FILE after a CLOSE answers 00, not 62.
*> ISO 14.9.7.4 GR3: "The execution of the COMMIT statement permanently applies any changes made to the files".
*> Both files are OPTIONAL, so an OPEN INPUT of an absent one answers 05 (ISO 9.1.13.2 item 4 a)) and no fatal 35 brings
*> item 8's ROLLBACK (ISO 14.6.13.1.3) in. Each leg fails on its own: a replacement a ROLLBACK keeps (record 1 absent,
*> record 2 present), an uncommitted change made before the capture that the ROLLBACK keeps (record 3 / K3 present), a
*> DELETE FILE refused 62 or kept (05 at the reopen after the ROLLBACK), a COMMIT that does not keep the replacement and
*> the removal, and a creation that the ROLLBACK keeps (00 instead of 05), whether its connector was CLOSEd at the
*> ROLLBACK or is still OPEN on the file (GR3 leaves the open mode: the connector stays open on no file, and its next
*> WRITE creates the file again).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3492KY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL R ASSIGN TO "pb3492r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT OPTIONAL X ASSIGN TO "pb3492x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XK
               FILE STATUS IS XS.
       I-O-CONTROL.
           APPLY COMMIT ON R X.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 RREC PIC X(4).
       FD X.
       01 XREC.
          05 XK PIC X(2).
          05 XD PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       01 XS PIC XX.
       01 N PIC 9.
       01 LINE-OUT PIC X(60).
       01 P PIC 99.
       PROCEDURE DIVISION.
       MAIN-LINE.
      *> The committed state: R holds record 1, X holds K1.
           OPEN OUTPUT R
           MOVE 1 TO RK
           MOVE "AAAA" TO RREC
           WRITE RREC
           CLOSE R
           OPEN OUTPUT X
           MOVE "K1" TO XK
           MOVE "AAAA" TO XD
           WRITE XREC
           CLOSE X
           COMMIT
           PERFORM SHOW-FILES
      *> Leg 1: an uncommitted change, then an OPEN OUTPUT that replaces each file; ROLLBACK restores both.
           OPEN I-O R
           MOVE 3 TO RK
           MOVE "CCCC" TO RREC
           WRITE RREC
           CLOSE R
           OPEN OUTPUT R
           DISPLAY "OPEN OUTPUT R " FS
           MOVE 2 TO RK
           MOVE "BBBB" TO RREC
           WRITE RREC
           CLOSE R
           OPEN I-O X
           MOVE "K3" TO XK
           MOVE "CCCC" TO XD
           WRITE XREC
           CLOSE X
           OPEN OUTPUT X
           DISPLAY "OPEN OUTPUT X " XS
           MOVE "K2" TO XK
           MOVE "BBBB" TO XD
           WRITE XREC
           CLOSE X
           ROLLBACK
           PERFORM SHOW-FILES
      *> Leg 2: an uncommitted change, then a DELETE FILE after the CLOSE; ROLLBACK restores both.
           OPEN I-O R
           MOVE 4 TO RK
           MOVE "DDDD" TO RREC
           WRITE RREC
           CLOSE R
           DELETE FILE R
           DISPLAY "DELETE FILE R " FS
           OPEN I-O X
           MOVE "K4" TO XK
           MOVE "DDDD" TO XD
           WRITE XREC
           CLOSE X
           DELETE FILE X
           DISPLAY "DELETE FILE X " XS
           OPEN INPUT R
           DISPLAY "OPEN INPUT R AFTER DELETE " FS
           CLOSE R
           ROLLBACK
           PERFORM SHOW-FILES
      *> Leg 3: a COMMIT keeps a replacement and a removal.
           OPEN OUTPUT R
           MOVE 2 TO RK
           MOVE "BBBB" TO RREC
           WRITE RREC
           CLOSE R
           DELETE FILE X
           DISPLAY "DELETE FILE X " XS
           COMMIT
           PERFORM SHOW-FILES
      *> Leg 4: a file created since the last COMMIT is absent again after a ROLLBACK.
           OPEN OUTPUT X
           DISPLAY "OPEN OUTPUT X CREATES " XS
           MOVE "K5" TO XK
           MOVE "EEEE" TO XD
           WRITE XREC
           CLOSE X
           ROLLBACK
           PERFORM SHOW-FILES
      *> Leg 5: a creation rolled back while its connector is still OPEN: the file is gone and the connector
      *> stays open on no file - a READ finds no record (23) and its next WRITE creates the file again, which
      *> the COMMIT keeps.
           OPEN I-O X
           DISPLAY "OPEN I-O X CREATES " XS
           MOVE "K6" TO XK
           MOVE "FFFF" TO XD
           WRITE XREC
           ROLLBACK
           MOVE "K6" TO XK
           READ X
               INVALID KEY DISPLAY "READ X AFTER ROLLBACK " XS
           END-READ
           MOVE "K3" TO XK
           MOVE "GGGG" TO XD
           WRITE XREC
           DISPLAY "WRITE X AFTER ROLLBACK " XS
           COMMIT
           CLOSE X
           PERFORM SHOW-FILES
      *> Leg 6: the same creation rolled back while OPEN with nothing written after it: no file, never the
      *> empty file the rolled-back OPEN made (05 at the reopen).
           DELETE FILE X
           DISPLAY "DELETE FILE X " XS
           COMMIT
           OPEN OUTPUT X
           DISPLAY "OPEN OUTPUT X CREATES " XS
           MOVE "K4" TO XK
           MOVE "HHHH" TO XD
           WRITE XREC
           ROLLBACK
           CLOSE X
           DISPLAY "CLOSE X AFTER ROLLBACK " XS
           PERFORM SHOW-FILES
           STOP RUN.
       SHOW-FILES.
           MOVE SPACES TO LINE-OUT
           MOVE 1 TO P
           OPEN INPUT R
           STRING "R " FS ":" DELIMITED BY SIZE INTO LINE-OUT WITH POINTER P
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 4
               MOVE N TO RK
               READ R
                   INVALID KEY CONTINUE
                   NOT INVALID KEY
                       STRING " " N "=" RREC DELIMITED BY SIZE INTO LINE-OUT WITH POINTER P
               END-READ
           END-PERFORM
           CLOSE R
           DISPLAY LINE-OUT
           MOVE SPACES TO LINE-OUT
           MOVE 1 TO P
           OPEN INPUT X
           STRING "X " XS ":" DELIMITED BY SIZE INTO LINE-OUT WITH POINTER P
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 5
               MOVE "K" TO XK(1:1)
               MOVE N TO XK(2:1)
               READ X
                   INVALID KEY CONTINUE
                   NOT INVALID KEY
                       STRING " " XK "=" XD DELIMITED BY SIZE INTO LINE-OUT WITH POINTER P
               END-READ
           END-PERFORM
           CLOSE X
           DISPLAY LINE-OUT.
