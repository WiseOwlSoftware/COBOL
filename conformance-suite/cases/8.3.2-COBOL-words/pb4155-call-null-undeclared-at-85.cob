      *>--- conformance-case
      *> id: pb4155-call-null-undeclared-at-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4155, the CALL arm of PB1673. At COBOL-85 §8.9
      *> does not reserve NULL (reserved-words.json r85 false), so
      *> it is a user-defined word, and this program declares none:
      *> the NULL operand of each CALL argument below names nothing
      *> declared - the §8.4.2.1 undefined reference ("a statement
      *> shall contain a reference that uniquely identifies that
      *> resource"), COBOL1639. It is never the Format 2 argument
      *> diagnostic of a program-prototype CALL (COBOL0899), nor the
      *> BY REFERENCE address screen of §14.9.4.3 SR3 (COBOL1677):
      *> both describe a predefined NULL the edition does not have.
      *> The 2002 half is pb1630_null_argument_crossing; the sibling
      *> positions (VALUE, DISPLAY, a relation, SET) are
      *> pb1673-null-undeclared-at-85 and
      *> pb1673-set-null-undeclared-at-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4155C.
       PROCEDURE DIVISION.
           CALL "X" USING NULL.
           CALL "X" USING BY REFERENCE NULL.
           CALL "X" USING BY CONTENT NULL.
           STOP RUN.
