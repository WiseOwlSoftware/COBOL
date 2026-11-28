## The image-seed lane reads a numeric-edited VALUE, an INDEX member and a figurative numeric VALUE as its record-struct twin does

Three wrong initial values under a REDEFINES (Tier-B group, EXTERNAL, BASED, OO cell), one root: the character-image seed
lane (`ImageInitializer.CarrierInitOfOne`, `InitialStateBackground.Seed`) wrote its own answer where the record-struct lane
has one shared composer (kb/Work PB6259, PB6261).

**PB6261.** `01 G. 05 E PIC ZZ9.99 VALUE ZERO. 01 R REDEFINES G PIC X(6).` at --std 2023 displayed `E` as `000000`; the
unaliased item holds `  0.00` (ISO 1989:2023 13.18.63.3 SR6: the figurative ZERO is treated as the literal zero and converted by
the MOVE rules). The image lane had its own EditCompose / LocaleEditCompose arms, behind its figurative arm. Both lanes now
ask `ValueInitializer.NumericEditedValueSeed` (LOCALE arm plus `EditedImageOfNumericValue`), the image lane before its figurative
arm; the private arms are deleted and `TheNumericEditedValueImage_HasExactlyThreeReaders` still sees three readers.

**PB6259.** Under `OPTIONS. INITIALIZE WORKING-STORAGE SECTION TO X"5A".` a USAGE INDEX member of a REDEFINES group was seeded
with the C# `long` expression the native IndexCell takes; the image lane spliced it into a string, so the backing held the
decimal digits (`R` read `651061555`, `A` read `5`). `Seed`'s INDEX arm now answers the native axis only (`item.Class is null`);
a class member takes the byte-form image arm, eight fill bytes (`ZZZZZZZZZ`, `Z`).

**Sibling found by the sweep.** A numeric member with a figurative VALUE (`PIC 9(4) VALUE ZERO`, also COMP-3) in the same
group took the OPTIONS INITIALIZE background instead of zero, because the image lane asked `NoValueSeed` for an item that has a
VALUE (the float arm already guarded this). It now asks only when `!(useValues && effRaw is not null)`.

Goldens: `pb6261_redefines_numeric_edited_value_seed` (2023; six edited items incl. BLANK WHEN ZERO, currency and a
floating-point edited item), refusal `pb6261-redefines-numeric-edited-value-below-2023` (COBOL0900 at 85/2002/2014),
`pb6259_options_initialize_index_in_redefined_group`, `pb6261_options_initialize_redefined_figurative_value`. Expected images
computed from 13.18.40.5 editing rule 7 a and 14.6.2.3.2 actions 1 and 2.
