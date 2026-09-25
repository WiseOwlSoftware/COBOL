      *> reject-at: 2002 2014 2023
      *> ISO §8.3.3.5.4 2) — a national literal (format 1) is class
      *>   national
      *> "National literals are of the class and category national"
      *> OK  §8.3.3.5.4 2)  (General rules)
      *> 15.66.3 1) NATIONAL-OF: "Argument-1 shall be of class
      *>   alphabetic or
      *> class alphanumeric."  OK  §15.66.3 1)  (Argument rules)
      *> NATIONAL-OF("AB") is valid (alphanumeric literal).
      *>   NATIONAL-OF(N"AB")
      *> names a class-national argument, so the source shall be
      *>   rejected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WN PIC N(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION NATIONAL-OF("AB") TO WN
           MOVE FUNCTION NATIONAL-OF(N"AB") TO WN
           STOP RUN.
