      *>--- conformance-case
      *> id: pb5291_debug_section_is_comment_without_mode_85
      *> rules: [1985-GR-XV.2.1.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5291. §1985:XV.2.1.3 GR2: without WITH DEBUGGING
      *>   MODE "any USE FOR DEBUGGING statements and all associated
      *>   debugging sections are compiled as if they were comment
      *>   lines". A comment line may hold any text, so the body of
      *>   DBG below, which is not COBOL syntax (and whose paragraph
      *>   name DBG-P is written again in MAIN), is no more a
      *>   compile error than a comment. The program has no
      *>   WITH DEBUGGING MODE. Expected output: P1 A and the line
      *>   of MAIN's own DBG-P. Fails if the text of the section is
      *>   parsed (COBOL5506) or its paragraph name is declared.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5291CM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X VALUE "A".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON NO-SUCH-PROC ALL PROCEDURES.
       DBG-P.
           MOVE UNDEFINED-NAME TO DEBUG-NAME
           .  THIS IS NOT COBOL
           GO TO P1.
       DBG-Q.
           DISPLAY DEBUG-CONTENTS.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           DISPLAY "P1 " X.
       DBG-P.
           DISPLAY "MAIN DBG-P".
           STOP RUN.
