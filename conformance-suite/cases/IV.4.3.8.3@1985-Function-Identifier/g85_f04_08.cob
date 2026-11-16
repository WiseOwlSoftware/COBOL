      *>--- conformance-case
      *> id: g85_f04_08
      *> rules: [1985-SR-IV.4.3.8.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.3.3 SR3 - "A function-identifier which references
      *> an alphanumeric function may be specified anywhere in the general
      *> formats that an identifier is permitted" except as a receiving
      *> operand or where the item's characteristics are required. Each
      *> leg places an alphanumeric function where a sending identifier is
      *> permitted (UPPER-CASE, LOWER-CASE and REVERSE): V, the
      *> subject of EVALUATE; U, the sending item of UNSTRING; I, the
      *> inspected item of INSPECT TALLYING (only counted, never changed);
      *> N, the argument of another function (§1985:IV.4.3.8.3.3 SR1 allows
      *> an identifier as argument); D, a STRING delimiter. With W-TXT =
      *> "abc-de": V matches WHEN "ABC-DE"; U splits "ABC-DE" at "-" into
      *> ABC and DE; I counts one "-"; N reverses the upper-cased text to
      *> "ED-CBA"; D copies "ab", W-TXT up to the delimiter FUNCTION
      *> LOWER-CASE (W-C), which is "c". A compiler that
      *> refused a function in any of these places fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0408.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-TXT  PIC X(6) VALUE "abc-de".
       01  W-C    PIC X VALUE "c".
       01  W-P1   PIC X(6) VALUE SPACES.
       01  W-P2   PIC X(6) VALUE SPACES.
       01  W-N    PIC 99 VALUE 0.
       01  W-S    PIC X(6) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE FUNCTION UPPER-CASE (W-TXT)
               WHEN "ABC-DE" DISPLAY "V MATCH"
               WHEN OTHER DISPLAY "V NO MATCH"
           END-EVALUATE.
           UNSTRING FUNCTION UPPER-CASE (W-TXT) DELIMITED BY "-"
               INTO W-P1 W-P2
           END-UNSTRING.
           DISPLAY "U [" W-P1 "][" W-P2 "]".
           INSPECT FUNCTION UPPER-CASE (W-TXT)
               TALLYING W-N FOR ALL "-".
           DISPLAY "I " W-N.
           DISPLAY "N "
               FUNCTION REVERSE (FUNCTION UPPER-CASE (W-TXT)).
           STRING W-TXT DELIMITED BY FUNCTION LOWER-CASE (W-C)
               INTO W-S
           END-STRING.
           DISPLAY "D [" W-S "]".
           STOP RUN.
