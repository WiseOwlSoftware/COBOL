      *>--- conformance-case
      *> id: pb5386_return_into_subscript_after_return
      *> rules: [GR-14.9.34.4-5]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5386. RETURN ... INTO identifier-1 identifies
      *> identifier-1 after the record is returned: "Item
      *> identification of the data item referenced by identifier-1
      *> is done after the record has been read and immediately before
      *> it is moved to the data item" (ISO 14.9.34.4 GR5 b); in 1985
      *> "Any subscripting associated with identifier-1 is evaluated
      *> after the record has been read", XI.4.3.4 GR6b). The
      *> subscript SEQ is in the returned record, so each record goes
      *> to the element its OWN SEQ selects: A3 to WE (3), B1 to
      *> WE (1), C2 to WE (2). The SD's two record descriptions make
      *> SEQ a window of the shared record area, the subscript form
      *> that was evaluated before the RETURN and stored each record
      *> into the element the PREVIOUS record selected (WT[C2..B1]).
      *> The fourth RETURN is at end, so its INTO move does not occur
      *> and WN keeps its value (GR5 b: "The implied MOVE statement
      *> does not occur if the execution of the RETURN statement was
      *> unsuccessful").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5386RT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR.
           05 K1 PIC X.
           05 SEQ PIC 9.
       01  SR-LONG PIC X(6).
       WORKING-STORAGE SECTION.
       01  WT.
           05 WE PIC X(2) OCCURS 3 VALUE "..".
       01  WN PIC X(6) VALUE "INIT".
       PROCEDURE DIVISION.
       MS SECTION.
       M1.
           SORT SF ON ASCENDING KEY K1
               INPUT PROCEDURE IS IS1
               OUTPUT PROCEDURE IS OS1.
           DISPLAY "WT[" WT "] WN[" WN "]".
           STOP RUN.
       IS1 SECTION.
       I1.
           MOVE "B1" TO SR. RELEASE SR.
           MOVE "A3" TO SR. RELEASE SR.
           MOVE "C2XYZW" TO SR-LONG. RELEASE SR-LONG.
       OS1 SECTION.
       O1.
           MOVE 1 TO SEQ.
           PERFORM 3 TIMES
             RETURN SF INTO WE (SEQ) AT END DISPLAY "EARLY END"
             END-RETURN
           END-PERFORM.
           RETURN SF INTO WN AT END DISPLAY "AT END"
           END-RETURN.
