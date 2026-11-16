      *>--- conformance-case
      *> id: g2002-sr-10-5-2-3-function-signatures-differ
      *> rules: [2002-SR-10.5.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:10.5.2 SR3 - "the signatures of these two compilation
      *> units shall be the same." REFUSED leg: the prototype G2FN02
      *> returns PIC X and the definition G2FN02 (same externalized
      *> name) returns PIC X(2). §2002:8.13 puts "the description of the
      *> returning item of the source unit" in the signature, so the
      *> signatures differ and the group violates SR3 (a prohibiting
      *> syntax rule; it grants no form). Fails if the pair is accepted.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FN02 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X.
       PROCEDURE DIVISION RETURNING R.
       END FUNCTION G2FN02.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FN02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X(2).
       PROCEDURE DIVISION RETURNING R.
           MOVE "A" TO R.
           GOBACK.
       END FUNCTION G2FN02.
