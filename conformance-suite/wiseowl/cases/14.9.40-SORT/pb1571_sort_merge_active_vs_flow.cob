*>--- conformance-case
*> id: pb1571_sort_merge_active_vs_flow
*> rules: [GR-14.9.40.4-10, GR-14.9.40.4-13, GR-14.9.32.4-1, GR-14.9.34.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1571 -- which exception condition a RETURN executed in the range of
      *> an input procedure, and a RELEASE or a format 1 SORT executed in the range of
      *> an output procedure, is set to raise (docs/CONFORMANCE.md 3, D-SMA (a)).
      *> ISO 14.9.40.4 GR10: "If the range of the input procedure causes the
      *> execution of any MERGE, RETURN, or format 1 SORT statements, the
      *> EC-SORT-MERGE-ACTIVE exception condition is set to exist." GR13 says the same
      *> of an output procedure and MERGE, RELEASE or a format 1 SORT. The same RETURN
      *> also fails 14.9.34.4 GR1 ("If it is executed at any other time, the
      *> EC-FLOW-RETURN exception condition is set to exist") and the same RELEASE
      *> 14.9.32.4 GR1 (EC-FLOW-RELEASE). 14.6.13.1.1: "if more than one exception is
      *> detected during the execution of a statement, the one that is set to exist
      *> is undefined" -- so the standard does not say which, and WiseOwl COBOL
      *> raises EC-SORT-MERGE-ACTIVE when it is enabled and the EC-FLOW name only when
      *> it is not. GnuCOBOL 3.2 raises neither (measured), IBM Enterprise COBOL
      *> documents the restriction with no condition, and Micro Focus documents one
      *> fatal run-time error (COBRT099) for both (the survey in D-SMA (a)), so the
      *> order is the implementor's own. Every declarative here ends in RESUME AT NEXT
      *> STATEMENT, a completion that is not normal (14.6.13.1.2), so 14.9.40.4 GR17
      *> ("If a USE procedure invoked while a format 1 SORT statement is active does
      *> not complete normally, the SORT statement is terminated") ends the active
      *> SORT at the declarative: nothing after the raise in that SORT runs.
      *> A statement may only be executed in the range of a procedure by a transfer
      *> of control (a SORT or RETURN WRITTEN in the range is refused at compile
      *> time, 14.9.40.3 SR3), so each runs in a paragraph outside every range that
      *> the procedure PERFORMs.
      *>
      *> The program is in two regions: EC-SORT-MERGE-ACTIVE and EC-FLOW-* are all
      *> enabled down to OUT-G, EC-SORT-MERGE-ACTIVE is turned OFF for the two
      *> STRAY-*-FLOW paragraphs after it. The checking applies where the statement
      *> is WRITTEN.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT, in execution order --
      *>   1  H=EC-SORT-MERGE-ACTIVE   RETURN in IN-1's range, all enabled; the SORT
      *>                               ends (no "IN-1 END", no STRAY-GOT, no record)
      *>   2  H=EC-SORT-MERGE-ACTIVE   RELEASE in OUT-2's range; the SORT ends (no
      *>                               "OUT-2 NEXT")
      *>   3  H=EC-FLOW-RETURN         RETURN in IN-3's range, -ACTIVE not enabled
      *>   4  H=EC-FLOW-RELEASE        RELEASE in OUT-4's range, -ACTIVE not enabled
      *>   5  H=EC-SORT-MERGE-ACTIVE   a format 1 SORT of SG in OUT-5's range: the
      *>                               inner SORT is terminated by GR13 (FEED-G, OUT-G
      *>                               never run) and the RESUME ends the outer one
      *>   6  R=FFF                    OUT-6 returns its one record,
      *>      H=EC-SORT-MERGE-RETURN   then a RETURN after AT END in the current output
      *>                               procedure (14.9.34.4 GR3), which no ACTIVE or
      *>                               FLOW rule pre-empts
      *>      DONE
      >>TURN EC-SORT-MERGE-ACTIVE EC-FLOW-RETURN EC-FLOW-RELEASE EC-SORT-MERGE-RETURN CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1571ACTFLOW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb1571-f.srt".
           SELECT SG ASSIGN TO "pb1571-g.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SF-REC PIC X(3).
       SD SG.
       01 SG-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SORT-MERGE-ACTIVE
               EC-FLOW-RETURN EC-FLOW-RELEASE EC-SORT-MERGE-RETURN.
       H-P.
           DISPLAY "H=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 0 TO WS-EOF.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS IN-1 OUTPUT PROCEDURE IS OUT-PLAIN.
           MOVE 0 TO WS-EOF.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS IN-2 OUTPUT PROCEDURE IS OUT-2.
           MOVE 0 TO WS-EOF.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS IN-3 OUTPUT PROCEDURE IS OUT-PLAIN.
           MOVE 0 TO WS-EOF.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS IN-4 OUTPUT PROCEDURE IS OUT-4.
           MOVE 0 TO WS-EOF.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS IN-5 OUTPUT PROCEDURE IS OUT-5.
           MOVE 0 TO WS-EOF.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS IN-6 OUTPUT PROCEDURE IS OUT-6.
           DISPLAY "DONE".
           STOP RUN.
       IN-1.
           MOVE "BBB" TO SF-REC.
           RELEASE SF-REC.
           PERFORM STRAY-RET-BOTH.
           DISPLAY "IN-1 END".
       IN-2.
           MOVE "AAA" TO SF-REC.
           RELEASE SF-REC.
       IN-3.
           MOVE "CCC" TO SF-REC.
           RELEASE SF-REC.
           PERFORM STRAY-RET-FLOW.
           DISPLAY "IN-3 END".
       IN-4.
           MOVE "DDD" TO SF-REC.
           RELEASE SF-REC.
       IN-5.
           MOVE "EEE" TO SF-REC.
           RELEASE SF-REC.
       IN-6.
           MOVE "FFF" TO SF-REC.
           RELEASE SF-REC.
       OUT-PLAIN.
           PERFORM RETURN-ALL.
       OUT-2.
           PERFORM STRAY-REL-BOTH.
           DISPLAY "OUT-2 NEXT".
           PERFORM RETURN-ALL.
       OUT-4.
           PERFORM STRAY-REL-FLOW.
           DISPLAY "OUT-4 NEXT".
           PERFORM RETURN-ALL.
       OUT-5.
           PERFORM INNER-SORT.
           DISPLAY "OUT-5 NEXT".
           PERFORM RETURN-ALL.
       OUT-6.
           PERFORM RETURN-ALL.
           RETURN SF
               AT END DISPLAY "SECOND-AT-END"
           END-RETURN.
       RETURN-ALL.
           PERFORM UNTIL WS-EOF = 1
               RETURN SF
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "R=" SF-REC
               END-RETURN
           END-PERFORM.
       STRAY-RET-BOTH.
           RETURN SF
               AT END DISPLAY "STRAY-AT-END"
               NOT AT END DISPLAY "STRAY-GOT"
           END-RETURN.
       STRAY-REL-BOTH.
           MOVE "ZZZ" TO SF-REC.
           RELEASE SF-REC.
       INNER-SORT.
           SORT SG ON ASCENDING KEY SG-REC
               INPUT PROCEDURE IS FEED-G OUTPUT PROCEDURE IS OUT-G.
       FEED-G.
           DISPLAY "FEED-G MUST NOT RUN".
       OUT-G.
           DISPLAY "OUT-G MUST NOT RUN".
      >>TURN EC-SORT-MERGE-ACTIVE CHECKING OFF
       STRAY-RET-FLOW.
           RETURN SF
               AT END DISPLAY "STRAY-AT-END"
               NOT AT END DISPLAY "STRAY-GOT"
           END-RETURN.
       STRAY-REL-FLOW.
           MOVE "ZZZ" TO SF-REC.
           RELEASE SF-REC.
