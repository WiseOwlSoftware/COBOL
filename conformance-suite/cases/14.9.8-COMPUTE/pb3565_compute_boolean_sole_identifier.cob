*>--- conformance-case
*> id: pb3565_compute_boolean_sole_identifier
*> rules: [GR-14.9.8.4-3, SR-8.8.2-1, GR-8.4.3.2.4-1, GR-15.2-2, RV-15.13.4-1, GR-13.10.4-1, FMT-14.9.8.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3565 - COMPUTE Format 2 whose boolean-expression-1 is a
      *> SOLE identifier that is not a bare data-name.
      *> ISO 8.8.2: a boolean expression may be "an identifier referencing a
      *> boolean data item" and "a boolean expression enclosed in
      *> parentheses". ISO 15.2 2): "Boolean functions. These are of the
      *> class and category boolean. Boolean functions have an implicit
      *> usage bit." ISO 8.4.3.2.4 1): "A function-identifier references a
      *> temporary data item whose value is determined when the function is
      *> referenced at runtime" - so a boolean function-identifier, an
      *> intrinsic or a user-defined one, IS an identifier referencing a
      *> boolean data item. ISO 13.10.4 1): a constant-name is "as if
      *> literal-1 ... were written where constant-name-1 is written", so a
      *> boolean constant-name is the boolean literal it stands for.
      *> ISO 14.9.8.4 3): the value stored has "the number of boolean
      *> positions in the largest boolean item referenced in the
      *> expression", and 14.6.8.6 stores it left-aligned, zero-filled on
      *> the right, into each receiver.
      *> The statement parses as Format 1 (a sole identifier predicts the
      *> arithmetic alternative) and the binder re-routes it to Format 2;
      *> the re-route used to recognize an unparenthesized DATA reference
      *> only, so each leg below but the control was refused (COBOL1511).
      *> C0 - control: a sole boolean item, the one form the re-route knew.
      *> F1 - BOOLEAN-OF-INTEGER(5, 8): 5 is 101 in eight positions.
      *> F2 - the same function in parentheses: 3 is 00000011.
      *> F3 - BOOLEAN-OF-INTEGER(3, 2): a two-position item holds 11, and
      *>      GR3's width is that item's two positions, so the eight-position
      *>      receiver takes 11 and zero-fills right: 11000000.
      *> F4 - two receivers: 9 is 00001001 in both (Format 2 takes several).
      *> U1 - a user-defined function RETURNING a boolean item: the
      *>      temporary takes the RETURNING item's description, class and
      *>      category (8.4.3.2.4 1), so the function returns 00001010.
      *> U2 - the same user-defined function in parentheses.
      *> P1 - a parenthesized boolean item (C8 holds 00001111).
      *> K1 - a boolean constant-name (KB stands for B"00110011").
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB3565UBIT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9.
       01 L-R PIC 1(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       MAIN.
           MOVE B"00001010" TO L-R
           GOBACK.
       END FUNCTION PB3565UBIT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3565SOLE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB3565UBIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8 USAGE BIT PICTURE 1(8).
       01 D8 USAGE BIT PICTURE 1(8).
       01 C8 USAGE BIT PICTURE 1(8) VALUE B"00001111".
       01 U8 PIC 1(8).
       01 KB CONSTANT AS B"00110011".
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE B8 = C8
           DISPLAY "C0=" B8
           COMPUTE B8 = FUNCTION BOOLEAN-OF-INTEGER(5, 8)
           DISPLAY "F1=" B8
           COMPUTE B8 = (FUNCTION BOOLEAN-OF-INTEGER(3, 8))
           DISPLAY "F2=" B8
           COMPUTE B8 = FUNCTION BOOLEAN-OF-INTEGER(3, 2)
           DISPLAY "F3=" B8
           COMPUTE B8 D8 = FUNCTION BOOLEAN-OF-INTEGER(9, 8)
           DISPLAY "F4=" B8 "," D8
           COMPUTE U8 = FUNCTION PB3565UBIT(3)
           DISPLAY "U1=" U8
           COMPUTE U8 = (FUNCTION PB3565UBIT(3))
           DISPLAY "U2=" U8
           COMPUTE B8 = (C8)
           DISPLAY "P1=" B8
           COMPUTE B8 = KB
           DISPLAY "K1=" B8
           STOP RUN.
       END PROGRAM PB3565SOLE.
