## kb/Work PB4731: the native-arithmetic authority is §8.8.1.3

PB4731 cited §8.8.1.5.1 as the authority for converting a float item to decimal. That clause states the standard-decimal
mode's conversion; under native arithmetic §8.8.1.3 alone leaves the whole evaluation to the implementor, and the compiler
adopts the §8.8.1.5.1 lift (`CobolDec.FromBinary64`) as its one conversion in both modes. The note's text and spec_refs
now say so. The code comments that name §8.8.1.5.1 for that lift stay correct: they name the conversion the compiler adopts.
