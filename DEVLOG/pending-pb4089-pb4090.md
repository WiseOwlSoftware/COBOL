## PB4089 and PB4090: two compiler defects from the evening waves, adjudicated against the spec

At the owner's request ("These may be compiler bugs which should be fixed if possible before the fuzz"), two leads of
wave 20261760 were checked against ISO/IEC 1989:2023 and filed as defects.

- **PB4089 (wrong answer).** STRING into a numeric DISPLAY receiver re-reads the receiver as a number. ISO §14.9.43.4 GR3
  a), GR6 and GR7 move characters one at a time into the receiver's character positions and change only those
  positions, so `STRING "AB" INTO N` with N = 123456 must leave AB3456, as GnuCOBOL does. WiseOwl leaves 003456.
- **PB4090 (accepts illegal source).** A Format 1 SEND that names a message server compiles without the RETURNING
  phrase that §14.9.38.2 prints unbracketed. PB2024's screen covers only a literal operand.
