*>--- conformance-case
*> id: pb4827_median_under_a_standard_mode_is_the_sdidi_mean
*> rules: [RV-15.61.4-2, RV-15.62.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4827 review R2-1: under ARITHMETIC IS STANDARD-DECIMAL (ISO 11.9.4 GR1) the mean of the two middle values of an even MEDIAN
*> list (ISO 15.61.4 2)) is the arithmetic expression (A + B) / 2 whose every step is an SDIDI of 34 digits (ISO 15.4.1), so DISPLAY
*> shows the same value COMPUTE stores and MIDRANGE shows. A = 12345678901234567890123456, B = .004999999: A + B has 35 digits,
*> ...456.004999999, and its 34-digit form is ...456.00500000 (the dropped digit 9 rounds up under NEAREST-EVEN); half of it is
*> 6172839450617283945061728.0025. (Native arithmetic shows the exact ...728.0024999995, the PB4826 golden.)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4827STD.
OPTIONS.
    ARITHMETIC IS STANDARD-DECIMAL
    INTERMEDIATE ROUNDING IS NEAREST-EVEN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC 9(31) VALUE 12345678901234567890123456.
01 B PIC V9(9) VALUE 0.004999999.
01 W PIC 9(25)V9(6).
PROCEDURE DIVISION.
    DISPLAY "M1 " FUNCTION MEDIAN (A B)
    DISPLAY "D1 " FUNCTION MIDRANGE (A B)
    COMPUTE W = FUNCTION MEDIAN (A B)
    DISPLAY "W1 " W
    STOP RUN.
END PROGRAM PB4827STD.
