      *>--- conformance-case
      *> id: pb5473-copy-every-text-qualified
      *> rules: [1985-SR-XII.2.3-1]
      *> accept: [85]
      *> files: [PB5473TX.cpy, PB5473LA/PB5473TX.cpy, PB5473LB/PB5473TY.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR1 (cite.py --check: OK): "If more than one COBOL
      *> library is available during compilation, text-name-1 must be
      *> qualified by library-name-1 identifying the COBOL library in which
      *> the text associated with text-name-1 resides." The libraries
      *> available are the default library and each library a COPY
      *> statement names with OF or IN that a library place holds (the
      *> DOC-A.1-40 determination, kb/Work PB5473): PB5473LA and PB5473LB
      *> here, so each COPY qualifies its text-name and the program is
      *> legal at 85. The text the default library holds under the same
      *> name (PB5473TX.cpy, DEFAULT) is not taken. Expected: LIBRARY A,
      *> LIBRARY B. The refused twin is pb5473-copy-unqualified-text-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5473Q.
       PROCEDURE DIVISION.
       P1.
           COPY PB5473TX OF PB5473LA.
           COPY PB5473TY IN PB5473LB.
           STOP RUN.
