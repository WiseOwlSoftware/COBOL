      *>--- conformance-case
      *> id: g2dc01_defined_condition
      *> rules: [2002-GR-7.2.7.3.3-1, 2002-GR-7.2.7.3.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.3.3 GR1 - "A defined condition using the IS DEFINED
      *>   syntax evaluates TRUE if compilation-variable-name-1 is
      *>   currently defined."
      *> §2002:7.2.7.3.3 GR2 - "A defined condition using the IS NOT
      *>   DEFINED syntax evaluates TRUE if compilation-variable-name-1
      *>   is not currently defined."
      *> "Currently" follows the DEFINE directives processed so far: a
      *>   DEFINE ... OFF leaves the name usable only in a defined
      *>   condition (§2002:7.2.9.3 GR2), a later DEFINE after OFF
      *>   defines it again (§2002:7.2.9.2 SR2), and a DEFINE in omitted
      *>   text-1 of an IF directive is not in the resultant text
      *>   (§2002:7.2.13.2 SR6, §2002:7.2.13.3 GR3). IS is optional in
      *>   the general format (§2002:7.2.7.3.1).
      *> EXPECTED OUTPUT, DERIVED (GR1 = D lines, GR2 = N lines; each
      *> condition also has its opposite form, whose BAD branch would
      *> print):
      *>   D1  V after DEFINE V AS 1: V IS DEFINED TRUE (GR1).
      *>   N1  W never defined: W IS NOT DEFINED TRUE (GR2).
      *>   N2  W IS DEFINED is FALSE (GR1), ELSE branch.
      *>   D2  V IS NOT DEFINED is FALSE (GR2), ELSE branch.
      *>   N3  after DEFINE V AS OFF: V IS NOT DEFINED TRUE (GR2).
      *>   N4  V DEFINED (IS omitted) is FALSE (GR1), ELSE branch.
      *>   D3  after DEFINE V AS 5 (redefined after OFF): V IS DEFINED.
      *>   N5  Z's DEFINE sits in omitted text-1 (1 = 2 is FALSE): Z IS
      *>       NOT DEFINED TRUE (GR2).
      *>   D4  Y's DEFINE sits in included text-1 (1 = 1): Y DEFINED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V IS DEFINED
           DISPLAY "D1"
       >>ELSE
           DISPLAY "D1-BAD"
       >>END-IF
       >>IF W IS NOT DEFINED
           DISPLAY "N1"
       >>ELSE
           DISPLAY "N1-BAD"
       >>END-IF
       >>IF W IS DEFINED
           DISPLAY "N2-BAD"
       >>ELSE
           DISPLAY "N2"
       >>END-IF
       >>IF V IS NOT DEFINED
           DISPLAY "D2-BAD"
       >>ELSE
           DISPLAY "D2"
       >>END-IF
       >>DEFINE V AS OFF
       >>IF V IS NOT DEFINED
           DISPLAY "N3"
       >>ELSE
           DISPLAY "N3-BAD"
       >>END-IF
       >>IF V DEFINED
           DISPLAY "N4-BAD"
       >>ELSE
           DISPLAY "N4"
       >>END-IF
       >>DEFINE V AS 5
       >>IF V IS DEFINED
           DISPLAY "D3"
       >>ELSE
           DISPLAY "D3-BAD"
       >>END-IF
       >>IF 1 = 2
       >>DEFINE Z AS 1
       >>END-IF
       >>IF Z IS NOT DEFINED
           DISPLAY "N5"
       >>ELSE
           DISPLAY "N5-BAD"
       >>END-IF
       >>IF 1 = 1
       >>DEFINE Y AS 1
       >>END-IF
       >>IF Y IS DEFINED
           DISPLAY "D4"
       >>ELSE
           DISPLAY "D4-BAD"
       >>END-IF
           STOP RUN.
