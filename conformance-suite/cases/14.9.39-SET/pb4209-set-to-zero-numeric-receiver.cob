*>--- conformance-case
*> id: pb4209-set-to-zero-numeric-receiver
*> rules: [SR-14.9.39.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR4: "If identifier-1 references a numeric data item,
      *> index-name-2 shall be specified." (cite.py: OK  §14.9.39.3 4)). The
      *> bare ZERO is arithmetic-expression-1, not index-name-2, so the
      *> statement is refused. kb/Work PB4209: the refusal quotes the sender AS
      *> THE PROGRAMMER WROTE IT. The token rewriter gives a bare ZERO in an
      *> arithmetic slot the text "0", and the message used to print TO '0'.
      *> Fails if the message spells the rewritten token instead of the source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4209A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET WS-N TO ZERO + 1
           DISPLAY WS-N
           STOP RUN.