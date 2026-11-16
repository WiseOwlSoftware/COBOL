      *>--- conformance-case
      *> id: g2eva03_cd_range_alphabet
      *> rules: [2002-SR-14.8.12.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.12.2 SR3 - "Alphabet-name-1 may be specified only
      *> when the literals or identifiers specified in the THROUGH
      *> phrase are of class alphabetic, alphanumeric, or national" ...
      *> "otherwise, alphabet-name-1 shall reference an alphabet that
      *> defines an alphanumeric collating sequence". The range operands
      *> are output-CD fields of class alphanumeric: SYMBOLIC
      *> DESTINATION SD1 (§2002:A.1.2.3 GR16d) and STATUS KEY SK
      *> (GR16c); both alphabets used define alphanumeric sequences.
      *> §2002:14.6.7: "When the IN alphabet-name phrase is specified,
      *> the collating sequence used for range evaluation is the
      *> collating sequence defined by that alphabet." Under REV
      *> (§2002:12.2.6.3 GR7k: "The order in which the literals appear
      *> in the ALPHABET clause specifies, in ascending sequence, the
      *> ordinal number of the character") Z = 0, M = 13, A = 25; the
      *> space, not in the literal, sorts above them all (GR7k 3).
      *> SD1 = "A", SK = "Z", subject X = "M". The program collating
      *> sequence is the native one.
      *>  E1 SD1 THRU SK IN ALPHA1 (NATIVE): A <= M <= Z    -> E1 IN
      *>  E2 SK THRU SD1 IN REV: Z(0) <= M(13) <= A(25)     -> E2 IN
      *>     (native order would make the range Z..A empty, so E2 fails
      *>     if the alphabet is ignored)
      *> The refusals beside it: numeric CD operands TL THRU DC with
      *> IN ALPHA1 (g2eva04), and the alphanumeric CD operands with the
      *> national alphabet NREV (g2eva05). COBOL0903 is a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVA03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPHA1 IS NATIVE
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET NREV FOR NATIONAL IS N"CBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(12) VALUE "M".
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "A" TO SD1.
           MOVE "Z" TO SK.
           EVALUATE X
             WHEN SD1 THRU SK IN ALPHA1 DISPLAY "E1 IN"
             WHEN OTHER DISPLAY "E1 OUT"
           END-EVALUATE.
           EVALUATE X
             WHEN SK THRU SD1 IN REV DISPLAY "E2 IN"
             WHEN OTHER DISPLAY "E2 OUT"
           END-EVALUATE.
           STOP RUN.
       END PROGRAM G2EVA03.
