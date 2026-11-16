      *>--- conformance-case
      *> id: g85_bf03_04
      *> rules: [1985-GR-VI.4.5.4-8, 1985-GR-VI.4.5.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR8 - without IN, symbolic-character-1 is the
      *> character at native ordinal integer-1 (SA: 66, "A"); with IN,
      *> integer-1 is the ordinal in alphabet-name-2's character set
      *> (S1 S2 S3: 1 2 3 IN AL2 = "Q" "R" "S", ALSO "T" sharing
      *> ordinal 2 with "R" and literal-1 alone represents it, GR4d 6;
      *> SB: 67 IN STD, STANDARD-1 = X3.4, its 67th character "B").
      *> GR9 - the internal representation is that of the native
      *> character, so each compares equal to the native literal. A
      *> compiler that read IN ordinals as native ordinals prints
      *> control characters, or "T" for S2, and fails the EQ lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0304.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL2 IS "Q" "R" ALSO "T" "S"
           ALPHABET STD IS STANDARD-1
           SYMBOLIC CHARACTERS SA IS 66
           SYMBOLIC CHARACTERS S1 S2 S3 ARE 1 2 3 IN AL2
           SYMBOLIC CHARACTERS SB IS 67 IN STD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X1 PIC X.
       01  X3 PIC X(3).
       01  W3 PIC X(3) VALUE ALL S2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "SA=" SA " S=" S1 S2 S3 " SB=" SB.
           MOVE SA TO X1.
           IF X1 = "A" DISPLAY "SA-EQ" ELSE DISPLAY "SA-NE".
           MOVE S2 TO X1.
           IF X1 = "R" DISPLAY "S2-EQ" ELSE DISPLAY "S2-NE".
           MOVE SB TO X1.
           IF X1 = "B" DISPLAY "SB-EQ" ELSE DISPLAY "SB-NE".
           MOVE ALL S1 TO X3.
           DISPLAY "ALL-S1=" X3 " W3=" W3.
           STOP RUN.
