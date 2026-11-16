      *>--- conformance-case
      *> id: g2-national-of-argument-2-national-one-char
      *> rules: [2002-AR-15.53.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.53.2 AR2 - "Argument-2 shall be of category national and
      *> shall be one character in length." Each leg writes a LEGAL
      *> argument-2 of a different shape (a PIC N(1) item, a one-character
      *> national literal N"#", a one-character reference modification of
      *> a PIC N(3) item) and must be ACCEPTED and evaluated; argument-1 is
      *> alphanumeric or alphabetic (§2002:15.53.2 AR1).
      *> Every character of argument-1 is a COBOL character with a national
      *> correspondent, so §2002:15.53.3 RV1 converts each one and the
      *> substitution of §2002:15.53.3 RV2 applies to none: the result with
      *> argument-2 equals the one-argument result (SAME=T, XY=T). Its
      *> length is one national position per character (§2002:15.53.3 RV4):
      *> "ABCDEFG" -> LEN=07. MOVE to PIC N(4) pads with a national space.
      *> A leg fails if a legal argument-2 is refused (COBOL1546 misfiring),
      *> if argument-2 replaces a character that has a correspondent, or if
      *> the returned length is not the character count.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NOF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AL      PIC A(3) VALUE "ABC".
       01  AN      PIC X(4) VALUE "xY1-".
       01  SUBC    PIC N(1) VALUE N"#".
       01  NN      PIC N(3) VALUE N"*#@".
       01  N4      PIC N(4).
       01  L       PIC 99.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION NATIONAL-OF(AN SUBC) TO N4
           DISPLAY "ITEM=[" N4 "]"
           MOVE FUNCTION NATIONAL-OF(AL N"#") TO N4
           DISPLAY "LIT=[" N4 "]"
           MOVE FUNCTION NATIONAL-OF(AN NN(2:1)) TO N4
           DISPLAY "REFMOD=[" N4 "]"
           MOVE FUNCTION LENGTH(FUNCTION NATIONAL-OF("ABCDEFG" N"#"))
             TO L
           DISPLAY "LEN=" L
           IF FUNCTION NATIONAL-OF(AN SUBC) = FUNCTION NATIONAL-OF(AN)
               DISPLAY "SAME=T"
           ELSE
               DISPLAY "SAME=F"
           END-IF
           IF FUNCTION NATIONAL-OF("XY" N"#") = N"XY"
               DISPLAY "XY=T"
           ELSE
               DISPLAY "XY=F"
           END-IF
           STOP RUN.
