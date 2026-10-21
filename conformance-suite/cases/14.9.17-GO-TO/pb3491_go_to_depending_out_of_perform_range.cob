      *>--- conformance-case
      *> id: pb3491_go_to_depending_out_of_perform_range
      *> rules: [GR-14.9.17.4-1, GR-14.9.17.4-2, GR-14.9.28.4-1, GR-14.9.28.4-5, GR-14.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3491 - a GO TO ... DEPENDING ON whose selected target lies OUTSIDE the range of an active
      *> PERFORM must transfer there. WiseOwl dropped the transfer when the target was the paragraph that
      *> physically follows the PERFORM's exit paragraph: the PERFORM's return test took the target for the exit
      *> paragraph completing, returned, and the selected paragraph never ran. Present unchanged at all four
      *> editions (no per-edition copy - the behaviour does not differ; the version matrix covers the gating).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 14.9.17.4 GR2 - "When a GO TO statement represented by format 2 is executed, control is transferred to
      *>                  procedure-name-1, etc., depending on the value of identifier-1 being 1, 2, ... , n."
      *>                  and "the value of identifier-1 is anything other than the positive or unsigned integers
      *>                  1, 2, ... , n, then no transfer occurs and control passes to the next statement in the
      *>                  normal sequence for execution".
      *> 14.9.17.4 GR1 - the same for format 1 (the twin in V3).
      *> 14.9.28.4 GR1 - the range of a PERFORM "consists logically of all those statements that are executed as
      *>                  a result of executing the PERFORM statement through execution of the implicit transfer of
      *>                  control to the end of the PERFORM statement"; a statement executed after a GO TO left the
      *>                  performed paragraphs is not followed by any return.
      *> 14.9.28.4 GR5a - "If procedure-name-2 is not specified, the return mechanism is after the last statement
      *>                  of procedure-name-1": it is reached only by completing that last statement. A GO TO that
      *>                  names the next paragraph does not complete it, so the return mechanism never executes.
      *> 14.6.3 rule 1 - the implied transfer to the end of the range happens for "the last procedure in the range"
      *>                  when control falls out of it (V6: the out-of-range selector).
      *> Every MUST-NOT line is unreachable under the rules above; if one prints, the transfer was swallowed.
      *>
      *> The variants, each chained to the next by an explicit GO TO that leaves the PERFORM for good:
      *>   V1  the fuzz reduction: one target, selected, physically after the performed paragraph.
      *>   V2  three targets, K = 2 selects the SECOND (the first is the one physically after the paragraph).
      *>   V3  the format-1 GO TO twin of V1 (always correct).
      *>   V4  PERFORM THRU, the selected target INSIDE the range: runs, then the PERFORM returns normally.
      *>   V5  PERFORM THRU, DEPENDING in the exit paragraph's last statement, target after the range.
      *>   V6  the DEPENDING value is out of range (K = 9): no transfer, the paragraph completes, the PERFORM
      *>       returns to its caller.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3491DEP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "START".
           GO TO V1-START.
       V1-START.
           PERFORM V1-PARA.
           DISPLAY "V1-RETURNED-MUST-NOT".
           GO TO FINISH.
       V1-PARA.
           DISPLAY "V1".
           MOVE 1 TO K.
           GO TO V1-TARGET DEPENDING ON K.
       V1-TARGET.
           DISPLAY "V1-TARGET".
           GO TO V2-START.
       V2-START.
           PERFORM V2-PARA.
           DISPLAY "V2-RETURNED-MUST-NOT".
           GO TO FINISH.
       V2-PARA.
           DISPLAY "V2".
           MOVE 2 TO K.
           GO TO V2-FIRST V2-SECOND V2-THIRD DEPENDING ON K.
       V2-FIRST.
           DISPLAY "V2-FIRST-MUST-NOT".
           GO TO FINISH.
       V2-SECOND.
           DISPLAY "V2-SECOND".
           GO TO V3-START.
       V2-THIRD.
           DISPLAY "V2-THIRD-MUST-NOT".
           GO TO FINISH.
       V3-START.
           PERFORM V3-PARA.
           DISPLAY "V3-RETURNED-MUST-NOT".
           GO TO FINISH.
       V3-PARA.
           DISPLAY "V3".
           GO TO V3-TARGET.
       V3-TARGET.
           DISPLAY "V3-TARGET".
           GO TO V4-START.
       V4-START.
           PERFORM V4-A THRU V4-C.
           DISPLAY "V4-RETURNED".
           GO TO V5-START.
       V4-A.
           DISPLAY "V4-A".
           MOVE 2 TO K.
           GO TO V4-B V4-C DEPENDING ON K.
       V4-B.
           DISPLAY "V4-B-MUST-NOT".
       V4-C.
           DISPLAY "V4-C".
       V5-START.
           PERFORM V5-A THRU V5-B.
           DISPLAY "V5-RETURNED-MUST-NOT".
           GO TO FINISH.
       V5-A.
           DISPLAY "V5-A".
       V5-B.
           DISPLAY "V5-B".
           MOVE 1 TO K.
           GO TO V5-TARGET DEPENDING ON K.
       V5-TARGET.
           DISPLAY "V5-TARGET".
           GO TO V6-START.
       V6-START.
           PERFORM V6-PARA.
           DISPLAY "V6-RETURNED".
           GO TO FINISH.
       V6-PARA.
           DISPLAY "V6".
           MOVE 9 TO K.
           GO TO V6-X DEPENDING ON K.
       V6-X.
           DISPLAY "V6-X-MUST-NOT".
           GO TO FINISH.
       FINISH.
           DISPLAY "END".
           STOP RUN.
