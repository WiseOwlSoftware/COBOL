      *>--- conformance-case
      *> id: g2_exit_alone_in_paragraph
      *> rules: [2002-SR-14.8.13.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.2 SR1 - "The EXIT statement shall appear in a
      *>   sentence by itself that shall be the only sentence in the
      *>   paragraph." The two admitted positions, each accepted and
      *>   run: P1-EXIT and S2-X are named paragraphs whose only
      *>   sentence is EXIT; S3 SECTION is followed directly by EXIT,
      *>   which by §2002:14.3.2 ("if the paragraph-name is omitted, one
      *>   or more successive sentences following the procedure division
      *>   header or a section header") is an unnamed paragraph whose
      *>   only sentence is EXIT.
      *> §2002:14.8.13.3 GR1 - "An EXIT statement serves only to enable
      *>   the user to assign a procedure-name to a given point in a
      *>   procedure division. Such an EXIT statement has no other
      *>   effect on the compilation or execution."
      *> EXPECTED (derived): PERFORM P1 THRU P1-EXIT prints P1 and
      *>   returns at the end of P1-EXIT; PERFORM S2 prints S2-A and
      *>   passes the EXIT in S2-X with no effect; PERFORM S3 passes the
      *>   unnamed EXIT paragraph with no effect and prints S3-B; then
      *>   DONE.
      *> FAILS IF: either position is refused (SR1 read too narrowly,
      *>   e.g. the unnamed paragraph after a section header is not
      *>   counted as a paragraph), or EXIT transfers control (a missing
      *>   S2-A or S3-B line, or a lost return).
      *> The refusal leg of SR1 at 2002 is
      *>   conformance:cases/14.9.14-EXIT/pb397-exit-not-alone
      *>   (COBOL2966).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT07.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           PERFORM P1 THRU P1-EXIT.
           PERFORM S2.
           PERFORM S3.
           DISPLAY "DONE".
           STOP RUN.
       P1.
           DISPLAY "P1".
       P1-EXIT.
           EXIT.
       S2 SECTION.
       S2-A.
           DISPLAY "S2-A".
       S2-X.
           EXIT.
       S3 SECTION.
           EXIT.
       S3-B.
           DISPLAY "S3-B".
