       IDENTIFICATION DIVISION.
       PROGRAM-ID. FUZZ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC-COMPUTER.
       OBJECT-COMPUTER. GENERIC-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-RESULT     PIC 9(10)V9(10).
       01 WS-TEST-VAL   PIC 9(5)V9(5) VALUE 2.0.
       01 WS-NEG-VAL    PIC S9(5)V9(5) VALUE -2.0.
       01 WS-ZERO       PIC S9(5)V9(5) VALUE 0.0.
       01 WS-ONE        PIC S9(5)V9(5) VALUE 1.0.
       01 WS-TWO        PIC S9(5)V9(5) VALUE 2.0.
       01 WS-THREE      PIC S9(5)V9(5) VALUE 3.0.
       01 WS-FOUR       PIC S9(5)V9(5) VALUE 4.0.
       01 WS-FIVE       PIC S9(5)V9(5) VALUE 5.0.
       01 WS-NEG-ONE    PIC S9(5)V9(5) VALUE -1.0.
       01 WS-NEG-TWO    PIC S9(5)V9(5) VALUE -2.0.
       01 WS-NEG-THREE  PIC S9(5)V9(5) VALUE -3.0.
       01 WS-HALF       PIC S9(5)V9(5) VALUE 0.5.
       01 WS-NEG-HALF   PIC S9(5)V9(5) VALUE -0.5.
       01 WS-NEG-FOUR   PIC S9(5)V9(5) VALUE -4.0.
       01 WS-NEG-FIVE   PIC S9(5)V9(5) VALUE -5.0.
       01 WS-EXPONENT   PIC S9(5)V9(5).
       01 WS-RESULT-1   PIC 9(10)V9(10).
       01 WS-RESULT-2   PIC 9(10)V9(10).
       01 WS-RESULT-3   PIC 9(10)V9(10).
       01 WS-RESULT-4   PIC 9(10)V9(10).
       01 WS-RESULT-5   PIC 9(10)V9(10).
       01 WS-RESULT-6   PIC 9(10)V9(10).
       01 WS-RESULT-7   PIC 9(10)V9(10).
       01 WS-RESULT-8   PIC 9(10)V9(10).
       01 WS-RESULT-9   PIC 9(10)V9(10).
       01 WS-RESULT-10  PIC 9(10)V9(10).
       01 WS-RESULT-11  PIC 9(10)V9(10).
       01 WS-RESULT-12  PIC 9(10)V9(10).
       01 WS-RESULT-13  PIC 9(10)V9(10).
       01 WS-RESULT-14  PIC 9(10)V9(10).
       01 WS-RESULT-15  PIC 9(10)V9(10).
       01 WS-RESULT-16  PIC 9(10)V9(10).
       01 WS-RESULT-17  PIC 9(10)V9(10).
       01 WS-RESULT-18  PIC 9(10)V9(10).
       01 WS-RESULT-19  PIC 9(10)V9(10).
       01 WS-RESULT-20  PIC 9(10)V9(10).
       PROCEDURE DIVISION.
           COMPUTE WS-RESULT-1 = WS-TEST-VAL ** WS-ZERO
           DISPLAY "2.0 ** 0.0 = " WS-RESULT-1
           
           COMPUTE WS-RESULT-2 = WS-TEST-VAL ** WS-ONE
           DISPLAY "2.0 ** 1.0 = " WS-RESULT-2
           
           COMPUTE WS-RESULT-3 = WS-TEST-VAL ** WS-TWO
           DISPLAY "2.0 ** 2.0 = " WS-RESULT-3
           
           COMPUTE WS-RESULT-4 = WS-TEST-VAL ** WS-THREE
           DISPLAY "2.0 ** 3.0 = " WS-RESULT-4
           
           COMPUTE WS-RESULT-5 = WS-TEST-VAL ** WS-FOUR
           DISPLAY "2.0 ** 4.0 = " WS-RESULT-5
           
           COMPUTE WS-RESULT-6 = WS-NEG-VAL ** WS-ZERO
           DISPLAY "-2.0 ** 0.0 = " WS-RESULT-6
           
           COMPUTE WS-RESULT-7 = WS-NEG-VAL ** WS-ONE
           DISPLAY "-2.0 ** 1.0 = " WS-RESULT-7
           
           COMPUTE WS-RESULT-8 = WS-NEG-VAL ** WS-TWO
           DISPLAY "-2.0 ** 2.0 = " WS-RESULT-8
           
           COMPUTE WS-RESULT-9 = WS-NEG-VAL ** WS-THREE
           DISPLAY "-2.0 ** 3.0 = " WS-RESULT-9
           
           COMPUTE WS-RESULT-10 = WS-NEG-VAL ** WS-FOUR
           DISPLAY "-2.0 ** 4.0 = " WS-RESULT-10
           
           COMPUTE WS-RESULT-11 = WS-TEST-VAL ** WS-NEG-ONE
           DISPLAY "2.0 ** -1.0 = " WS-RESULT-11
           
           COMPUTE WS-RESULT-12 = WS-TEST-VAL ** WS-NEG-TWO
           DISPLAY "2.0 ** -2.0 = " WS-RESULT-12
           
           COMPUTE WS-RESULT-13 = WS-TEST-VAL ** WS-NEG-THREE
           DISPLAY "2.0 ** -3.0 = " WS-RESULT-13
           
           COMPUTE WS-RESULT-14 = WS-NEG-VAL ** WS-NEG-ONE
           DISPLAY "-2.0 ** -1.0 = " WS-RESULT-14
           
           COMPUTE WS-RESULT-15 = WS-NEG-VAL ** WS-NEG-TWO
           DISPLAY "-2.0 ** -2.0 = " WS-RESULT-15
           
           COMPUTE WS-RESULT-16 = WS-NEG-VAL ** WS-NEG-THREE
           DISPLAY "-2.0 ** -3.0 = " WS-RESULT-16
           
           COMPUTE WS-RESULT-17 = WS-HALF ** WS-TWO
           DISPLAY "0.5 ** 2.0 = " WS-RESULT-17
           
           COMPUTE WS-RESULT-18 = WS-NEG-HALF ** WS-TWO
           DISPLAY "-0.5 ** 2.0 = " WS-RESULT-18
           
           COMPUTE WS-RESULT-19 = WS-TEST-VAL ** WS-FIVE
           DISPLAY "2.0 ** 5.0 = " WS-RESULT-19
           
           COMPUTE WS-RESULT-20 = WS-NEG-VAL ** WS-FIVE
           DISPLAY "-2.0 ** 5.0 = " WS-RESULT-20
           
           STOP RUN.
