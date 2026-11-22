*>--- conformance-case
*> id: pb725-flag-85-removed-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> Annex E.2 item 21 ("Obsolete elements"): "The following features that were
      *> classified as obsolete in the previous COBOL standard, have been removed from this
      *> Working Draft International Standard: - FLAG-85 - FLAG-NATIVE-ARITHMETIC -
      *> Standard Arithmetic - Move of the figurative constant, QUOTE, to numeric or
      *> numeric-edited items". So >>FLAG-85 and >>FLAG-NATIVE-ARITHMETIC are REMOVED at
      *> 2023 (COBOL0902) and, by the same sentence's "classified as obsolete in the
      *> previous COBOL standard", OBSOLETE at 2014 (the COBOL0903 warning). Both were
      *> silently consumed at EVERY edition until kb/Work PB725.
      *>
      *> The introduction edge is quoted from the 2002 text (kb/Work PB3965): §2002:F.2
      *> item 56 "A directive, FLAG-85, has been added", and §2002:7.2.12 introduces
      *> FLAG-NATIVE-ARITHMETIC (§2002:F.2 item 29). Their operand syntax is the 2002 and
      *> 2014 texts' (§2002:7.2.11.1, §2002:7.2.12.1), checked by FlagDirectiveProcessor
      *> (kb/Work PB5600); this case gates the directive WORD, which the removal record
      *> supports.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB725NF8.
       PROCEDURE DIVISION.
       MAIN.
       >>FLAG-85 ALL ON
           DISPLAY "A".
       >>FLAG-NATIVE-ARITHMETIC STANDARD-BINARY
           STOP RUN.
