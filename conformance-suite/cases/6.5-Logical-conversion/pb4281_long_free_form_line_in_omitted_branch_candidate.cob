*>--- conformance-case
*> id: pb4281_long_free_form_line_in_omitted_branch_candidate
*> rules: [SR-6.1-3, GR-6.5-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 6.1 3) a): a free-form line has at most 255 character
*> positions, and 6.5 converts source text before conditional compilation
*> acts, so the limit binds a line of an omitted branch like any other.
*> The first >>IF group disagrees on the division (kb/Work PB4281), so the
*> AUTHOR line in the omitted second group is a comment-entry CANDIDATE
*> that the compilation never decides; its 308 positions are refused
*> COBOL2653 all the same (review finding R2-1: the round-2 fix had let it
*> through). Leg that can fail: the line accepted silently.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4281LL.
>>IF PB4281-B IS DEFINED
ENVIRONMENT DIVISION.
>>END-IF
>>IF PB4281-C IS DEFINED
AUTHOR. XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
>>END-IF
PROCEDURE DIVISION.
    DISPLAY "A".
    STOP RUN.
