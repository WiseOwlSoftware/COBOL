      *>--- conformance-case
      *> id: g2002-sr-10-5-2-3-function-signatures-same
      *> rules: [2002-SR-10.5.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:10.5.2 SR3 - "If a compilation group contains both a
      *> function definition and a function prototype definition with
      *> the same externalized name, the signatures of these two
      *> compilation units shall be the same." The signature is the
      *> external-repository information excluding the externalized
      *> name (§2002:8.13: "the description of the parameters" and "the
      *> description of the returning item of the source unit").
      *> ACCEPTED leg: prototype G2FN01 and definition G2FN01 both take
      *> USING PIC 9(2) and RETURN PIC 9(4), so the pair satisfies SR3
      *> and the group compiles (its refusal twin
      *> g2002-sr-10-5-2-3-function-signatures-differ changes only the
      *> returning PICTURE and must be refused). The definition computes
      *> L-R = L-X * 3; with W-X = 14 the call returns 42, MOVEd to
      *> W-R PIC 9(4) -> "R=0042". Fails if a matching pair is refused
      *> (SR3 applied to identical signatures) or if the invocation does
      *> not reach the in-group definition.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FN01 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(2).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION G2FN01.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FN01.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(2).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           COMPUTE L-R = L-X * 3.
           GOBACK.
       END FUNCTION G2FN01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CG01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2FN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC 9(2) VALUE 14.
       01 W-R PIC 9(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION G2FN01 (W-X) TO W-R.
           DISPLAY "R=" W-R.
           STOP RUN.
       END PROGRAM G2CG01.
