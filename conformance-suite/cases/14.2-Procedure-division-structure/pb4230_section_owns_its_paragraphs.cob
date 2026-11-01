      *>--- conformance-case
      *> id: pb4230_section_owns_its_paragraphs
      *> rules: [FMT-14.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4230. Every paragraph after a section header is that
      *> section's, up to the next section header.
      *> RULE (14.4.2): "A section consists of a section header followed
      *> by zero, one, or more successive paragraphs. A section ends
      *> immediately before the next section or at the end of the
      *> procedure division"
      *> cite.py --check 14.4.2 "A section consists of a section header
      *>   followed by zero, one, or more successive paragraphs" -> OK
      *> cite.py --check 14.4.2 "A section ends immediately before the
      *>   next section or at the end of the procedure division" -> OK
      *> cite.py --check 14.9.28.4 "the specified set of statements
      *>   consists of all statements beginning with the first statement
      *>   of procedure-name-1 and ending with the last statement of
      *>   procedure-name-2, or, if procedure-name-2 is not specified,
      *>   the last statement of procedure-name-1" -> OK  14.9.28.4 4)
      *> cite.py --check 8.4.2.2.3 "A paragraph-name need not be
      *>   qualified when referred to from within the same section"
      *>   -> OK  8.4.2.2.3 7)
      *> COMMON-P is defined in A-S and in B-S, so it is referenced
      *> qualified from MAIN-S; COMMON-P IN A-S resolves only because
      *> the first COMMON-P is in A-S.
      *> DERIVATION of every output line:
      *>  1. PERFORM A-S runs A-S from its first statement to its last:
      *>     the unnamed sentence (+1), A-1 (+10) and COMMON-P (+100).
      *>     A-S ends before B-S, so B-S's +1000 does not run: 0111.
      *>  2. PERFORM COMMON-P OF B-S runs only B-S's COMMON-P: 1000.
      *>  3. PERFORM COMMON-P IN A-S runs only A-S's COMMON-P: 0100.
      *>  4. STOP RUN ends the run before control falls into A-S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4230P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           PERFORM A-S.
           DISPLAY "A-S " W-N.
           MOVE 0 TO W-N.
           PERFORM COMMON-P OF B-S.
           DISPLAY "B-S " W-N.
           MOVE 0 TO W-N.
           PERFORM COMMON-P IN A-S.
           DISPLAY "A-S " W-N.
           STOP RUN.
       A-S SECTION.
           ADD 1 TO W-N.
       A-1.
           ADD 10 TO W-N.
       COMMON-P.
           ADD 100 TO W-N.
       B-S SECTION.
       COMMON-P.
           ADD 1000 TO W-N.
