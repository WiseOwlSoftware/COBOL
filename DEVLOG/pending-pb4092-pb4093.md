## PB4092 and PB4093: enabling one EC-SIZE name checks and stores as if the whole family were enabled

Two leads of the PB4031 wave, checked against ISO 14.6.13.1.1 and DOC-A.1-70 and filed so they can be fixed before
tonight's fuzz run (the owner moved its start to about 00:30):

- PB4092: with only EC-SIZE-TRUNCATION checking enabled, an overflowing comparison raises EC-SIZE-OVERFLOW, which is
  not enabled, and ends the run unit. NumericRenderer.CheckedFor checks the whole EC-SIZE family.
- PB4093: with only EC-SIZE-OVERFLOW checking enabled, a truncating COMPUTE leaves the receiver unchanged (007) where the
  not-enabled disposition of DOC-A.1-70 stores the low-order digits (000).
