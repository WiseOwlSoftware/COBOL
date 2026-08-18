      *> reject-at: 2002 2014 2023
      *> ISO 12.3.6.2 CHARACTER CLASSIFICATION is Annex A.4.9 item 7 - an optional locale-module element ("OBJECT-COMPUTER
      *> paragraph, CHARACTER CLASSIFICATION clause"). COBOL.NET's documented non-support of the locale module is
      *> conformant (4.2.7 / A.4.1) only because it is DIAGNOSED: COBOLNET1518, the SPECIAL-NAMES LOCALE clause's
      *> disposition. Before kb/Work PB78 the clause was swallowed silently by the attribute sink after a
      *> computer-name (and a parse error without one).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB78CC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX
           CHARACTER CLASSIFICATION IS SYSTEM-DEFAULT.
       PROCEDURE DIVISION.
           STOP RUN.
