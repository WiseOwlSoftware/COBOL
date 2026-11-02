*>--- conformance-case
*> id: pb4299-move-fault-after-a-faulted-sentence-tail
*> rules: [FMT-14.9.25.2, FMT-14.9.2.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4299 - a fault whose sentence tail holds a user-defined word not followed by a period must not cost
*> the faults after it. ISO 14.9.2.2 prints the ADD formats with operands before TO
*>   cite.py: OK  14.9.2.2  (General formats) - "identifier-2"
*> so `ADD TO W-N` is the first fault, at TO. ISO 14.4.3 makes a paragraph "a paragraph-name followed by a separator
*> period" (cite.py OK 14.4.3), so the W-N of the ADD's tail, followed by END-IF, can begin no paragraph. The next
*> sentence's MOVE omits TO, which 14.9.25.2 Format 1 prints underlined before "{ identifier-2 }" (cite.py OK
*> 14.9.25.2): the second fault, which this refusal expects. It fails if the recovery stops at the tail's W-N as a
*> paragraph header: the sentence then cannot go on, the procedure division's recovery consumes every later
*> sentence, and the MOVE's fault went unreported.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4299TAIL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 THEN ADD TO W-N END-IF.
    MOVE 1 W-N.
    STOP RUN.
