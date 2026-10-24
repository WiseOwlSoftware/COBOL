*>--- conformance-case
*> id: pb3340_retry_forever_own_run_unit_hold
*> rules: [GR-14.7.9.3-3, GR-14.7.9.3-4, GR-14.9.7.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3340 - a RETRY FOREVER phrase does not wait for a hold of its OWN run unit. ISO 14.7.9.3 GR3: "If the FOREVER
*> phrase is specified, the mass storage control system shall attempt to gain access to a locked resource until the
*> input-output operation has been completed." ISO 14.9.7.4 GR4: "For files specified in active APPLY COMMIT clauses that
*> have been closed and not reopened prior to the COMMIT statement, the file locks are released" - so until that COMMIT
*> they stand, and no wait inside the run unit can release them. docs/CONFORMANCE.md DOC-A.1-109 (ISO 9.1.13.8 2), "The
*> implementor shall specify under what conditions a deadlock is detected"): a FOREVER wait on a file held by another
*> connector of this run unit cannot complete, and lands that conflict's own status.
*> F (record sequential) and R (relative), both SHARING WITH NO OTHER under APPLY COMMIT, are written and CLOSEd: each
*> keeps its file lock. G and S name the same physical files and no APPLY COMMIT clause: their OPEN RETRY FOREVER answers
*> '61' at once (ISO 9.1.13.9 1)), their DELETE FILE RETRY FOREVER '62' (ISO 9.1.13.9 2)), and after the COMMIT both
*> OPENs answer '00'. A third file, J, written by an OPEN OUTPUT of an existing file under APPLY COMMIT and SHARING WITH
*> ALL OTHER, is held by the run unit's own replaced transaction (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.8),
*> which refuses K's writable OPEN the same way.
*> A wait for the run unit's own hold never ends: this case then never finishes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3340RF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3340rf.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS FS.
           SELECT G ASSIGN TO "pb3340rf.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER
               FILE STATUS IS GS.
           SELECT R ASSIGN TO "pb3340rr.dat"
               ORGANIZATION IS RELATIVE
               SHARING WITH NO OTHER
               FILE STATUS IS RS.
           SELECT S ASSIGN TO "pb3340rr.dat"
               ORGANIZATION IS RELATIVE
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER
               FILE STATUS IS SS.
           SELECT J ASSIGN TO "pb3340rj.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS JS.
           SELECT K ASSIGN TO "pb3340rj.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER
               FILE STATUS IS KS.
       I-O-CONTROL.
           APPLY COMMIT ON F R J.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X(2).
       FD G.
       01 GREC PIC X(2).
       FD R.
       01 RREC PIC X(2).
       FD S.
       01 SREC PIC X(2).
       FD J.
       01 JREC PIC X(2).
       FD K.
       01 KREC PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 GS PIC XX.
       01 RS PIC XX.
       01 SS PIC XX.
       01 JS PIC XX.
       01 KS PIC XX.
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           WRITE FREC FROM "F1"
           CLOSE F
           OPEN OUTPUT R
           WRITE RREC FROM "R1"
           CLOSE R
           OPEN OUTPUT J
           WRITE JREC FROM "J1"
           CLOSE J
           COMMIT
           OPEN EXTEND F
           WRITE FREC FROM "F2"
           CLOSE F
           OPEN EXTEND RETRY FOREVER G
           DISPLAY "G OPEN EXTEND RETRY FOREVER " GS
           DELETE FILE G RETRY FOREVER
           DISPLAY "G DELETE FILE RETRY FOREVER " GS
           OPEN EXTEND R
           WRITE RREC FROM "R2"
           CLOSE R
           OPEN I-O RETRY FOREVER S
           DISPLAY "S OPEN I-O RETRY FOREVER " SS
           DELETE FILE S RETRY FOREVER
           DISPLAY "S DELETE FILE RETRY FOREVER " SS
           OPEN OUTPUT J
           WRITE JREC FROM "J2"
           CLOSE J
           OPEN EXTEND RETRY FOREVER K
           DISPLAY "K OPEN EXTEND RETRY FOREVER " KS
           COMMIT
           OPEN EXTEND RETRY FOREVER G
           DISPLAY "G OPEN AFTER COMMIT " GS
           CLOSE G
           OPEN I-O RETRY FOREVER S
           DISPLAY "S OPEN AFTER COMMIT " SS
           CLOSE S
           OPEN EXTEND RETRY FOREVER K
           DISPLAY "K OPEN AFTER COMMIT " KS
           CLOSE K
           STOP RUN.
