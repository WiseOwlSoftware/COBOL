      *>--- conformance-case
      *> id: pb5279_receive_into_subscript_reads_cd
      *> rules: []
      *> accept: [85, 2002]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5279. X3.23-1985 IV.4.3.8: "any subscripting and
      *> reference modification are evaluated only once as the first
      *> operation of the execution of that statement", and RECEIVE
      *> (XIV.3.5.4 general rules 3, 8 and 9) puts the text in "the area
      *> referenced by identifier-1", left aligned "without space fill",
      *> the rest of the area "not changed".
      *> Here identifier-1's subscript and reference modifier read TEXT
      *> LENGTH, which the MCS updates during the RECEIVE: the area is the
      *> one the subscript named BEFORE the update.
      *> The queue RECV of the mcs.json beside this program holds ABCD, EF
      *> and GHIJ.
      *> Why each leg can fail: a receipt stored where the NEW text length
      *> points lands in ENT (4) for ABCD, in ENT (2) for EF, and in
      *> TBL (5:4) for GHIJ; and the unchanged positions of the area
      *> (EF fills two of ENT (3)'s four) come from the area identified
      *> first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5279RS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           02 ENT PIC X(4) OCCURS 5 TIMES.
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS Q
           SYMBOLIC SUB-QUEUE-1 IS S1
           SYMBOLIC SUB-QUEUE-2 IS S2
           SYMBOLIC SUB-QUEUE-3 IS S3
           MESSAGE DATE IS MD
           MESSAGE TIME IS MT
           SYMBOLIC SOURCE IS SRC
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "RECV" TO Q.
           MOVE SPACES TO S1 S2 S3.
           MOVE ALL "-" TO TBL.
           MOVE 1 TO TL.
           RECEIVE CIN MESSAGE INTO ENT (TL)
               NO DATA DISPLAY "NODATA".
           DISPLAY "SUB    [" TBL "] " TL.
           MOVE ALL "-" TO TBL.
           MOVE 3 TO TL.
           RECEIVE CIN MESSAGE INTO ENT (TL)
               NO DATA DISPLAY "NODATA".
           DISPLAY "SHORT  [" TBL "] " TL.
           MOVE ALL "-" TO TBL.
           MOVE 0 TO TL.
           RECEIVE CIN MESSAGE INTO TBL (TL + 1:4)
               NO DATA DISPLAY "NODATA".
           DISPLAY "REFMOD [" TBL "] " TL.
           STOP RUN.
