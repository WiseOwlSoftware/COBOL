      *> ISO 14.9.25.3 SR10, Table 16 - the ADMITTED cells the PB72/PB59-7 landing must not lose, each the
      *> "Yes" twin of a newly refused "No" cell:
      *> RB: a REF-MOD view is plain class-and-category ALPHANUMERIC (8.4.3.3.4 GR2/GR6 - never the inner
      *>     item's alphabetic/edited row), so an edited item's slice moves to a BOOLEAN receiver - the
      *>     alphanumeric-to-boolean "Yes" cell. Before the fix the inner item's flags rode the view and this
      *>     legal MOVE was refused 0819.
      *> RA: the same erasure on an ALPHABETIC item's slice - alphanumeric-to-boolean "Yes". The receiver
      *>     stores the view's characters verbatim on the D-B1 '0'/'1' substrate; content validity of a
      *>     boolean receiver is not Table 16's question and is not adjudicated here.
      *> XB: FUNCTION CONCAT over plain PIC X arguments is 15.18.4 r3's "otherwise" arm - ALPHANUMERIC -
      *>     and moves to a boolean receiver (the discriminating twin of the rejected all-PIC-A form,
      *>     negative pb59-concat-alphabetic-to-boolean).
      *> MX: a MIXED alphabetic+alphanumeric argument list is likewise the "otherwise" arm - r3 demands
      *>     argument-1 AND all argument-2 be class alphabetic for the alphabetic result.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB72ADMIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-AE PIC XXBXX VALUE "01 01".
       01 WS-A  PIC A(4)  VALUE "ABCD".
       01 WS-A1 PIC A(2)  VALUE "AB".
       01 WS-X1 PIC X(2)  VALUE "01".
       01 WS-B  PIC 1(2).
       01 WS-B4 PIC 1(4).
       PROCEDURE DIVISION.
           MOVE WS-AE(1:2) TO WS-B
           DISPLAY "RB=" WS-B
           MOVE WS-A(1:2) TO WS-B
           DISPLAY "RA=" WS-B
           MOVE FUNCTION CONCAT(WS-X1 WS-X1) TO WS-B4
           DISPLAY "XB=" WS-B4
           MOVE FUNCTION CONCAT(WS-A1 WS-X1) TO WS-B4
           DISPLAY "MX=" WS-B4
           STOP RUN.
       END PROGRAM PB72ADMIT.
