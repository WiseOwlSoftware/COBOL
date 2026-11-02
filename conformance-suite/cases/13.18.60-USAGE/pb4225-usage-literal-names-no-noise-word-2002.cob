      *>--- conformance-case
      *> id: pb4225-usage-literal-names-no-noise-word-2002
      *> rules: [FMT-13.18.60.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4225 - the COBOL-2002 twin of
      *> pb4201-usage-literal-names-the-usages-85, pinning the list of
      *> usages without the noise word IS. ISO 13.18.60.2 prints
      *> "[ USAGE IS ] usage" (cite.py --check 13.18.60.2 "USAGE" ->
      *> OK) and 8.3.2.4.3 calls an uppercase word that is not
      *> underlined an optional word that may be specified "with no
      *> effect on the semantics of the format" (cite.py --check
      *> 8.3.2.4.3 "uppercase words that are not underlined are called
      *> optional words" -> OK), so IS is no usage and the sentence
      *> does not offer it as an alternative to BINARY and the rest.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4225U02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X USAGE 5.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
