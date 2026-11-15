      *>--- conformance-case
      *> id: pb5473-copy-unqualified-text-name
      *> rules: [1985-SR-XII.2.3-1]
      *> files: [PB5473TX.cpy, PB5473LA/PB5473TX.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR1 (cite.py --check: OK): "If more than one COBOL
      *> library is available during compilation, text-name-1 must be
      *> qualified by library-name-1". The second COPY makes the library
      *> PB5473LA available beside the default library, so the first COPY,
      *> which names no library, is refused at 85 (COBOL0900, the
      *> introduction gate of the 2002 relaxation, constructs row
      *> copy-unqualified-text-name-2002; kb/Work PB5473) - although it is
      *> written before the COPY that names the second library. 2002 takes
      *> the unqualified text from the default library (§2002:7.1.2.3 GR3).
      *> The accepted twin is pb5473-copy-every-text-qualified.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5473U.
       PROCEDURE DIVISION.
       P1.
           COPY PB5473TX.
           COPY PB5473TX OF PB5473LA.
           STOP RUN.
