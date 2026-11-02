      *>--- conformance-case
      *> id: pb1965_annex_c_word_keys
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1965 - a COBOL word is keyed by the ONE Annex C fold
      *> (ISO 8.1.3.2 GR3 and GR4 b)), never by the host's upper-casing.
      *> 1. The capital U with diaeresis U+00DC folds to U+00FC
      *>    (Annex C.2), so KÜP and KüP are one word, the EQUATE
      *>    synonym of UPPER-CASE (7.3.10.4 GR2). (The Kelvin sign,
      *>    which this case once folded to k, pairs with a BASIC
      *>    letter and is not folded, kb/Work PB4420.)
      *> 2. FINAL SIGMA and SIGMA are two words at 2023 (Annex E.2 item
      *>    14 deleted (03C2,03C3)): Xς and Xσ are two EXTERNAL items
      *>    and two methods. The host's upper-casing wrote both as
      *>    capital sigma: one shared item (MAIN S), and a C# class with
      *>    two members of one name (CS0111).
       >>COBOL-WORDS EQUATE "UPPER-CASE" WITH "KÜP"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1965M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1965CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Xς PIC X EXTERNAL.
       01 W PIC X(3) VALUE "abc".
       01 O USAGE OBJECT REFERENCE PB1965CL.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "SYNONYM " FUNCTION KüP (W).
           MOVE "F" TO Xς.
           CALL "PB1965S".
           DISPLAY "FINAL " Xς.
           INVOKE PB1965CL "NEW" RETURNING O.
           INVOKE O "Xς".
           INVOKE O "Xσ".
           STOP RUN.
       END PROGRAM PB1965M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1965S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Xσ PIC X EXTERNAL.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "S" TO Xσ.
           DISPLAY "SIGMA " Xσ.
           GOBACK.
       END PROGRAM PB1965S.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1965CL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. Xς.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "METHOD FINAL-SIGMA".
       END METHOD Xς.
       METHOD-ID. Xσ.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "METHOD SIGMA".
       END METHOD Xσ.
       END OBJECT.
       END CLASS PB1965CL.
