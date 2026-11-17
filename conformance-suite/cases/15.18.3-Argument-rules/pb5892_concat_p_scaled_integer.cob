      *>--- conformance-case
      *> id: pb5892_concat_p_scaled_integer
      *> rules: [AR-15.18.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5892 - a numeric CONCAT argument is an unsigned
      *> integer by its VALUE, not by the spelling of its PICTURE.
      *>
      *> §2023:15.18.3 AR3 (cite.py --check): "If argument-1 or
      *> argument-2 is numeric, it shall be usage display or national
      *> and shall be an unsigned integer." U PIC 99P holds 40: the P
      *> is an assumed zero to the right, so every value of U is an
      *> integer (NumericLanes.IsIntegerByValue: scale -1). Before the
      *> fix it was refused COBOL1627 as "a non-integer numeric item".
      *> §2023:15.18.4 RV1 (cite.py --check): "The returned value of
      *> the function shall contain all of the characters in
      *> argument-1 followed by all of the characters in argument-2."
      *> CONCAT is not one of the operations that use the algebraic
      *> value, and §2023:13.18.40.4 GR14d (cite.py --check): "the
      *> digit positions specified with the symbol 'P' are ignored
      *> and are not counted in the size of the operand", so U's
      *> characters are its two stored digits "04" and the value is
      *> "A04", which MOVE pads to S PIC X(10).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5892A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U   PIC 99P VALUE 40.
       01  S   PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION CONCAT ("A" U) TO S.
           DISPLAY "[" S "]".
           STOP RUN.
