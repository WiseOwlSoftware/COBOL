## Entry 2779 — 2026-10-30 09:32 PDT — PB4075: MEAN and the binary64 family take a wide function argument as its exact value, not its 34-digit image

PB3953 gave the exact intrinsic family (ABS, MOD, MAX, SUM and the rest) the exact wide intermediate of an argument expression
of 39 to 77 digits, and left two consumers on the round-to-odd image `RawArg` lowers it to. Both reproduced on the landed tree
before the fix: `FUNCTION MEAN (BIG * BIG * K3, 1)` (BIG `9(18)` all nines, K3 = 999) showed
`499499999999999999001000000000000100000`, and `FUNCTION SIN (BIG * BIG * K3)` showed the sine of the image,
about 999 radians from the product (-0.929..., against the oracle's 0.93255971139006498686).

The root is one: a consumer that reads `RawArg` reads the image. The fix gives each consumer the wide form at the intake it
already has (`RawWideArg`):

- **MEAN.** `RenderWide` gained a `MeanScaled` arm. `CobolIntrinsics.MeanWide` sums the aligned arguments on `CobolWide`, and
  the new `CobolWide.QuotientDec` rounds the exact quotient to the SDIDI's 34 digits once: the dividend is scaled until the
  integer quotient has 36 digits, the true remainder is the sticky bit, `CobolDec.Round34Wide` (now internal) applies the
  intermediate rounding mode, and an exact quotient keeps the preferred exponent (PB3954). The arm returns the SDIDI, not a
  `NumX.Wide`, because MEAN's value is the SDIDI quotient on every other lane (PB3860).
- **The binary64 family.** `ExactIntake` has a third member, `Wide`, for the bodies that are ill-conditioned where the image
  cannot name the argument: SIN, COS, TAN (every multiple of pi/2), LOG, LOG10 (1), ACOS, ASIN (plus and minus 1). `FloatBody`
  names their `...Wide` overload (new `CobolIntrinsics.FloatWide.cs`) for a wide argument, through the new
  `CobolIntrinsics.DomainWideAdmitted` for a domain row (the SDIDI screen's own `Admits` predicate). The quarter-turn reduction
  became `ReduceQuarterTurns(ReadOnlySpan<long> limbs, int exp)`, reading three limbs of 1/(2 pi) per significand limb (nine for an
  SDIDI as before, fifteen for the five limbs of a 77-digit magnitude, from the new `CobolWide.MagnitudeLimbs`), and the quadrant
  rules of SIN, COS and TAN moved into `SinReduced`, `CosReduced` and `TanReduced` so the SDIDI and wide entries share them.
  LOG, LOG10, ACOS and ASIN form x - 1 and 1 - |x| on the exact wide value (`AcosOfGap` is the shared half-angle step).
  EXP, EXP10 and SQRT keep the image on purpose: condition numbers of at most 709, 709 and 1/2 cannot carry an image error of
  10^-34 relative to binary64's last bit. `WholeRangeBodiesDriftTests.EveryWideIntakeBody_HasItsWideOverload_AndNoOtherMemberDoes`
  holds the set closed, so the next ill-conditioned body decides its wide form instead of quietly reading the image.

Goldens and tests: `conformance-suite/cases/15.4.1-Numeric-and-integer-functions/pb4075_wide_argument_consumers` (accepted at
85 and every later edition, since MEAN, SIN, LOG and ACOS are 85 catalog rows and its 18-digit items fit COBOL-85's limit; seven
legs: MEAN with two and three arguments, SIN, COS, TAN, and LOG and ACOS of a 68-digit value 2*10^-34 from 1, each value derived
from a 400-digit Python `decimal` oracle). Before the fix the pre-change build showed legs 1 and 3 to 7 wrong. There is no
refused-below program: the rule's introducing edition is the first one. `WideArgumentBodiesTests` checks the wide bodies and
`MeanWide` (including the 34th-digit tie under two rounding modes and the preferred exponent) against the same oracle,
including 77-digit angles that exercise all five limbs.

The self-review found one more mechanism in the same intake, left open because its value needs a determination first:
VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE read the image through `DecArgList` (`VARIANCE (P, P - 1)` does not answer 0.25;
`PRESENT-VALUE (0, P, 1 - P)` answers 0, not 1). The mean they deviate from is itself a 34-digit SDIDI value, and no clause says
which precision a native evaluation owes it. PB4075's note records the repros, and DOC-A.1-123 now states which consumers take the
exact value and which the image.

