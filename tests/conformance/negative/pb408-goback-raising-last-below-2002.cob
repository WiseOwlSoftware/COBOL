      *> reject-at: 85
      *> kb/Work PB408. GOBACK (ISO 14.9.18) and its RAISING phrase are COBOL-2002
      *> introductions, so the whole of 14.9.18.4 GR1 b) - including GR1b3a's
      *> EC-RAISING-NOT-SPECIFIED substitution, whose LAST phrase this statement writes -
      *> has no COBOL-85 spelling at all. The four-compilers rule: the activator-side
      *> enablement behaviour the 2002 positive golden pins shall be unreachable below the
      *> edition that introduced the statement, and the compiler shall say so rather than
      *> accept it.
      *> POSITIVE CONTROL: 2002/pb408_raising_enablement_locus.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB408.
       PROCEDURE DIVISION.
       MAIN-P.
           GOBACK RAISING LAST EXCEPTION.
