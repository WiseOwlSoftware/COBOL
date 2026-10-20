       IDENTIFICATION DIVISION.
       PROGRAM-ID. FUZZ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC-COMPUTER.
       OBJECT-COMPUTER. GENERIC-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-RESULT     PIC 9(10) VALUE 0.
       01  WS-A          PIC 9(5) VALUE 2.
       01  WS-B          PIC 9(5) VALUE 3.
       01  WS-C          PIC 9(5) VALUE 4.
       01  WS-D          PIC 9(5) VALUE 5.
       01  WS-E          PIC 9(5) VALUE 6.
       01  WS-F          PIC 9(5) VALUE 7.
       01  WS-G          PIC 9(5) VALUE 8.
       01  WS-H          PIC 9(5) VALUE 9.
       01  WS-I          PIC 9(5) VALUE 10.
       01  WS-J          PIC 9(5) VALUE 11.
       01  WS-K          PIC 9(5) VALUE 12.
       01  WS-L          PIC 9(5) VALUE 13.
       01  WS-M          PIC 9(5) VALUE 14.
       01  WS-N          PIC 9(5) VALUE 15.
       01  WS-O          PIC 9(5) VALUE 16.
       01  WS-P          PIC 9(5) VALUE 17.
       01  WS-Q          PIC 9(5) VALUE 18.
       01  WS-R          PIC 9(5) VALUE 19.
       01  WS-S          PIC 9(5) VALUE 20.
       01  WS-T          PIC 9(5) VALUE 21.
       01  WS-U          PIC 9(5) VALUE 22.
       01  WS-V          PIC 9(5) VALUE 23.
       01  WS-W          PIC 9(5) VALUE 24.
       01  WS-X          PIC 9(5) VALUE 25.
       01  WS-Y          PIC 9(5) VALUE 26.
       01  WS-Z          PIC 9(5) VALUE 27.
       01  WS-NEG        PIC 9(5) VALUE 5.
       PROCEDURE DIVISION.
           MOVE 1 TO WS-RESULT.
           DISPLAY 'TEST 1: PARENTHESIS NESTING'
           COMPUTE WS-RESULT = (WS-A + WS-B) * (WS-C - WS-D)
           DISPLAY 'RESULT 1: ' WS-RESULT
           COMPUTE WS-RESULT = ((WS-A + WS-B) * WS-C) - WS-D
           DISPLAY 'RESULT 2: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + (WS-B * WS-C)) - WS-D
           DISPLAY 'RESULT 3: ' WS-RESULT
           COMPUTE WS-RESULT = ((WS-A + WS-B) * (WS-C - WS-D)) + WS-E
           DISPLAY 'RESULT 4: ' WS-RESULT
           DISPLAY 'TEST 2: EXPONENTIATION'
           COMPUTE WS-RESULT = WS-A ** 2
           DISPLAY 'RESULT 5: ' WS-RESULT
           COMPUTE WS-RESULT = WS-B ** 3
           DISPLAY 'RESULT 6: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + WS-B) ** 2
           DISPLAY 'RESULT 7: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** (WS-B - WS-C)
           DISPLAY 'RESULT 8: ' WS-RESULT
           DISPLAY 'TEST 3: OPERATOR PRECEDENCE'
           COMPUTE WS-RESULT = WS-A + WS-B * WS-C
           DISPLAY 'RESULT 9: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * WS-B + WS-C
           DISPLAY 'RESULT 10: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** WS-B + WS-C
           DISPLAY 'RESULT 11: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + WS-B ** WS-C
           DISPLAY 'RESULT 12: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * WS-B ** WS-C
           DISPLAY 'RESULT 13: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** WS-B * WS-C
           DISPLAY 'RESULT 14: ' WS-RESULT
           DISPLAY 'TEST 4: UNARY OPERATORS'
           COMPUTE WS-RESULT = -WS-A + WS-B
           DISPLAY 'RESULT 15: ' WS-RESULT
           COMPUTE WS-RESULT = +WS-A + WS-B
           DISPLAY 'RESULT 16: ' WS-RESULT
           COMPUTE WS-RESULT = -(WS-A + WS-B)
           DISPLAY 'RESULT 17: ' WS-RESULT
           COMPUTE WS-RESULT = -(-WS-A + WS-B)
           DISPLAY 'RESULT 18: ' WS-RESULT
           COMPUTE WS-RESULT = (-WS-A) * WS-B
           DISPLAY 'RESULT 19: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * (-WS-B)
           DISPLAY 'RESULT 20: ' WS-RESULT
           DISPLAY 'TEST 5: COMBINATION TABLE'
           COMPUTE WS-RESULT = WS-A + WS-B
           DISPLAY 'RESULT 21: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A - WS-B
           DISPLAY 'RESULT 22: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * WS-B
           DISPLAY 'RESULT 23: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A / WS-B
           DISPLAY 'RESULT 24: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** WS-B
           DISPLAY 'RESULT 25: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + WS-B) * WS-C - WS-D
           DISPLAY 'RESULT 26: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + WS-B * WS-C - WS-D
           DISPLAY 'RESULT 27: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** WS-B ** WS-C
           DISPLAY 'RESULT 28: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A ** WS-B) ** WS-C
           DISPLAY 'RESULT 29: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + WS-B * WS-C ** WS-D
           DISPLAY 'RESULT 30: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + WS-B) * (WS-C - WS-D) / WS-E
           DISPLAY 'RESULT 31: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** WS-B + WS-C ** WS-D
           DISPLAY 'RESULT 32: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * WS-B + WS-C - WS-D
           DISPLAY 'RESULT 33: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + WS-B * WS-C / WS-D
           DISPLAY 'RESULT 34: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + WS-B) ** (WS-C - WS-D)
           DISPLAY 'RESULT 35: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** (WS-B + WS-C) * WS-D
           DISPLAY 'RESULT 36: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * (WS-B ** WS-C) + WS-D
           DISPLAY 'RESULT 37: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + (WS-B - WS-C) * WS-D
           DISPLAY 'RESULT 38: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + WS-B) / (WS-C - WS-D)
           DISPLAY 'RESULT 39: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + WS-B + WS-C + WS-D + WS-E
           DISPLAY 'RESULT 40: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * WS-B * WS-C * WS-D * WS-E
           DISPLAY 'RESULT 41: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + WS-B) * (WS-C + WS-D)
           DISPLAY 'RESULT 42: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** WS-B ** WS-C ** WS-D
           DISPLAY 'RESULT 43: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A / WS-B / WS-C
           DISPLAY 'RESULT 44: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + WS-B - WS-C + WS-D - WS-E
           DISPLAY 'RESULT 45: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A * WS-B / WS-C * WS-D / WS-E
           DISPLAY 'RESULT 46: ' WS-RESULT
           COMPUTE WS-RESULT = (WS-A + WS-B) ** WS-C + WS-D
           DISPLAY 'RESULT 47: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A + (WS-B * WS-C) - WS-D
           DISPLAY 'RESULT 48: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A ** (WS-B + WS-C) / WS-D
           DISPLAY 'RESULT 49: ' WS-RESULT
           COMPUTE WS-RESULT = WS-A / (WS-B * WS-C) + WS-D
           DISPLAY 'RESULT 50: ' WS-RESULT
           STOP RUN.
