      *>--- conformance-case
      *> id: pb5603-send-identifier-formats
      *> rules: [2002-GR-8.4.2.1.3-1, 2002-SR-8.4.2.4.2-2,
      *>    2002-SR-8.4.2.5.2-1, 2002-GR-8.4.2.5.3-1,
      *>    2002-GR-8.4.2.5.3-6, 2002-GR-8.4.2.5.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5603 - the SEND statement (ISO/IEC 1989:2002 A.2.6)
      *> writes identifier-1, identifier-2 and identifier-3, and an
      *> identifier has a function-identifier format and an inline
      *> method invocation format (8.4.2.1.1 Format 1 and Format 4).
      *> "Inline method invocation references a temporary data item
      *> returned from invocation of a method" (8.4.2.4), "with the same
      *> class, category, and content" (8.4.2.4.3 GR1). FROM took no
      *> inline invocation and WITH and ADVANCING took neither format,
      *> so each SEND below was a parse error. Every leg can fail:
      *> - SEND 1, FROM OB :: "GETX": a portion of a message (A.2.6.3
      *>   GR6) of the three leftmost characters "ABC" (GR3);
      *> - SEND 2, FROM OU AS RQ5603C :: "GETY": OU holds the object
      *>   through a UNIVERSAL view (8.4.2.5.3 GR7) and the inline
      *>   receiver is a class view (SR1, GR1) returning "DEF". WITH
      *>   OB AS RQ5603I :: "GETI" is identifier-2 through an interface
      *>   view (GR6); GETI returns a PIC 9 holding 2, the end of
      *>   message indicator (Table A.3), so the message ABCDEF is
      *>   complete. AFTER ADVANCING FUNCTION MOD (5 3) LINES is an
      *>   integer function as identifier-3 (A.2.6.2 SR4; 8.4.2.2.2 SR9
      *>   bars only a numeric one), which this MCS ignores (GR12);
      *> - SEND 3, FROM FUNCTION UPPER-CASE ("ghi") WITH EMI BEFORE
      *>   ADVANCING OB :: "GETN" LINES: identifier-3 is an inline
      *>   invocation returning a PIC 99 integer; the message is GHI.
      *> Each status key is 00 (Table A.1). RECEIVE takes the two
      *> messages in order, their text lengths 6 and 3 (A.2.5.3).
      *> At --std 85 an integer function-identifier stands only in an
      *> arithmetic expression (the 1989 amendment, kb/Work PB5108).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5603G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS RQ5603C
           INTERFACE RQ5603I.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  OB USAGE OBJECT REFERENCE RQ5603C.
       01  OU USAGE OBJECT REFERENCE.
       01  W  PIC X(10).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS OUT-CNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""HOLD""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""LOOP"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""HOLD""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           INVOKE RQ5603C "NEW" RETURNING OB.
           SET OU TO OB AS UNIVERSAL.
           MOVE "HOLD" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE 1 TO OUT-CNT.
           MOVE "LOOP" TO OUT-DEST (1).
           MOVE 3 TO OUT-LEN.
           SEND COUT FROM OB :: "GETX".
           DISPLAY "S1 " OUT-STAT.
           SEND COUT FROM OU AS RQ5603C :: "GETY"
               WITH OB AS RQ5603I :: "GETI"
               AFTER ADVANCING FUNCTION MOD (5 3) LINES.
           DISPLAY "S2 " OUT-STAT.
           SEND COUT FROM FUNCTION UPPER-CASE ("ghi") WITH EMI
               BEFORE ADVANCING OB :: "GETN" LINES.
           DISPLAY "S3 " OUT-STAT.
           PERFORM 2 TIMES
               MOVE SPACES TO W
               RECEIVE CIN MESSAGE INTO W
                   NO DATA DISPLAY "NO DATA"
                   WITH DATA DISPLAY "MESSAGE " IN-LEN " [" W "]"
               END-RECEIVE
           END-PERFORM.
           STOP RUN.
       END PROGRAM PB5603G.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. RQ5603I.
       PROCEDURE DIVISION.
       METHOD-ID. GETI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-I PIC 9.
       PROCEDURE DIVISION RETURNING LK-I.
       END METHOD GETI.
       END INTERFACE RQ5603I.

       IDENTIFICATION DIVISION.
       CLASS-ID. RQ5603C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE RQ5603I.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS RQ5603I.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC X(10).
       PROCEDURE DIVISION RETURNING LK-X.
           MOVE "ABC" TO LK-X.
       END METHOD GETX.
       METHOD-ID. GETY.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-Y PIC X(10).
       PROCEDURE DIVISION RETURNING LK-Y.
           MOVE "DEF" TO LK-Y.
       END METHOD GETY.
       METHOD-ID. GETI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-I PIC 9.
       PROCEDURE DIVISION RETURNING LK-I.
           MOVE 2 TO LK-I.
       END METHOD GETI.
       METHOD-ID. GETN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 99.
       PROCEDURE DIVISION RETURNING LK-N.
           MOVE 3 TO LK-N.
       END METHOD GETN.
       END OBJECT.
       END CLASS RQ5603C.
