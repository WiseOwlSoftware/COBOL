      *>--- conformance-case
      *> id: 1985-SR-XII_3_3-1
      *> rules: [1985-SR-XII.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.3 SR1 (cite.py --check: OK): "A REPLACE statement may occur
      *> anywhere in the source program where a character-string may occur. It
      *> must be preceded by a separator period except when it is the first
      *> statement in a separately compiled program." Every REPLACE below is
      *> either the first statement of its program or follows the separator
      *> period of END PROGRAM, so the program is legal. Why it can fail: a
      *> compiler that reads a REPLACE only after a period would refuse the
      *> first statement of the file. Expected output derived from XII.3.4 GR1:
      *> each AAA is replaced by the literal its REPLACE names, ONE then TWO.
      *> The refused twin is pb5085-replace-not-after-period.
       REPLACE ==AAA== BY =="ONE"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R331A.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY AAA.
           CALL "R331B".
           STOP RUN.
       END PROGRAM R331A.
       REPLACE ==AAA== BY =="TWO"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R331B.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY AAA.
           EXIT PROGRAM.
       END PROGRAM R331B.
