*>--- conformance-case
*> id: sort-sd-record-with-managed-slots
*> rules: [GR-14.9.40.4-2, GR-14.9.32.4-2, GR-14.9.34.4-3, GR-14.9.34.4-5, DOC-A.1-212]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4772 - a SORT whose SD record is a strongly-typed group holding a pointer and a message-tag leaf
      *> (13.18.60.3 SR14 admits both only in a strongly-typed group), sorted on another item of the record.
      *> 14.9.32.4 GR2: "The execution of a RELEASE statement causes the record named by record-name-1 to be released to
      *> the initial phase of a sort operation." 14.9.34.4 GR3: the RETURN causes "the next existing record in the file
      *> referenced by file-name-1, as determined by the keys listed in the SORT or MERGE statement, to be made available
      *> in the record area" - the whole record, so the pointer a record was released with is the pointer it returns with,
      *> whichever position the key order gives it. GR5 b): the RETURN INTO phrase then moves "the current record" to
      *> identifier-1, so the receiver holds the same leaves. The message tag is NULL (13.18.63.4 GR4) in every record and
      *> stays NULL. (cite.py --check OK for 14.9.32.4 2) and 14.9.34.4 3).)
      *> The keys 3, 1, 2 are released in that order and come back 1, 2, 3; each returned record's pointer is set to the
      *> item that was addressed when ITS key was released (key 1 -> WS-A, 2 -> WS-B, 3 -> WS-C), which is shown by reading
      *> the based item through the returned pointer, in the record area and in the INTO receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SORTSLOTS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "sortslots.wrk".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SREC TYPE T-REC.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 SKEY PIC 9(2).
          05 SPTR USAGE POINTER.
          05 STAG USAGE MESSAGE-TAG.
       01 WS-OUT TYPE T-REC.
       01 WS-SRC TYPE T-REC.
       01 WS-A PIC X(5) VALUE "AAAAA".
       01 WS-B PIC X(5) VALUE "BBBBB".
       01 WS-C PIC X(5) VALUE "CCCCC".
       01 BX PIC X(5) BASED.
       01 WS-I PIC 9(2).
       PROCEDURE DIVISION.
       MAIN SECTION.
           SORT SF ON ASCENDING KEY SKEY OF SREC
               INPUT PROCEDURE IS INP
               OUTPUT PROCEDURE IS OUTP.
           STOP RUN.
       INP SECTION.
           PERFORM VARYING WS-I FROM 3 BY -1 UNTIL WS-I = 0
              MOVE WS-I TO SKEY OF WS-SRC
              EVALUATE WS-I
                 WHEN 1 SET SPTR OF WS-SRC TO ADDRESS OF WS-A
                 WHEN 2 SET SPTR OF WS-SRC TO ADDRESS OF WS-B
                 WHEN 3 SET SPTR OF WS-SRC TO ADDRESS OF WS-C
              END-EVALUATE
              SET STAG OF WS-SRC TO NULL
              RELEASE SREC FROM WS-SRC
           END-PERFORM.
       OUTP SECTION.
           PERFORM 3 TIMES
              RETURN SF INTO WS-OUT AT END EXIT PERFORM END-RETURN
              SET ADDRESS OF BX TO SPTR OF SREC
              DISPLAY SKEY OF SREC " " BX " area"
              SET ADDRESS OF BX TO SPTR OF WS-OUT
              DISPLAY SKEY OF WS-OUT " " BX " into"
              IF STAG OF SREC = NULL AND STAG OF WS-OUT = NULL
                 DISPLAY "tags null"
              END-IF
           END-PERFORM.
