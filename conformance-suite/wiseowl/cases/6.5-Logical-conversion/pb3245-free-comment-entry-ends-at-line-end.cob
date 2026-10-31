*>--- conformance-case
*> id: pb3245-free-comment-entry-ends-at-line-end
*> rules: [GR-6.5-5]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3245 review R1-1, docs/CONFORMANCE.md D-CE1 - a free-form
*>   comment-entry is the rest of its header line and no more (GnuCOBOL
*>   3.2's -free reading, CLAUDE.md rule 1: free form has no Area A to
*>   end it the way COBOL-85 ended a fixed-form one). The second line
*>   below is therefore program text again, and 'friends' is no
*>   paragraph of the IDENTIFICATION DIVISION (COBOL1971). GnuCOBOL 3.2
*>   -free refuses the same program at the same line.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3245CL.
AUTHOR. Pat O'Brien and
   friends.
PROCEDURE DIVISION.
    DISPLAY "OK".
    STOP RUN.
