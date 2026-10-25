*>--- conformance-case
*> id: pb3755-invoke-content-low-value-numeric-edited-2023
*> rules: [GR-14.8.2.3.3-L2.2, SR-14.9.25.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3755, the INVOKE lane of pb3755-call-content-high-value-numeric-edited-2023: 14.9.23.3 SR5 c) applies
      *> 14.8.2 to a method's arguments, so LOW-VALUE BY CONTENT into the PIC ZZ9 formal of a factory method conforms as
      *> MOVE LOW-VALUE TO <a numeric-edited item> does - valid through 2014, removed at 2023 (14.9.25.3 SR5; Annex E.2
      *> item 1 bullet 1) - and is COBOLNET0902 at 2023.
      *>   cite.py --check 14.9.23.3 "The rules for conformance as specified in 14.8.2, Parameters and 14.8.3,
      *>     Returning items apply" -> OK 14.9.23.3 5) c)
       IDENTIFICATION DIVISION.
       CLASS-ID. K3755I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3755I.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION USING BY REFERENCE E.
           DISPLAY "ENTERED"
           GOBACK.
       END METHOD MN.
       END FACTORY.
       END CLASS K3755I.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3755I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3755I.
       PROCEDURE DIVISION.
           INVOKE K3755I "MN" USING BY CONTENT LOW-VALUE
           STOP RUN.
       END PROGRAM P3755I.
