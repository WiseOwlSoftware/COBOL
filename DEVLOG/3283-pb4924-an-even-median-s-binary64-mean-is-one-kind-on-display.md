## Entry 3283 — 2026-11-07 16:36 PST — PB4924 - an even MEDIAN's binary64 mean is one kind on DISPLAY and COMPUTE

COMPUTE returned the binary64 mean as a float item's content (`CobolExact.FromBinary64`, ordered by exact value) while DISPLAY's
`FunctionValueField.Median` returned `OfFloat`, ordered by the shortest image, so a MAX or MIN over a nested even MEDIAN beside a
value equal to that image tied on DISPLAY and selected differently (`MAX (MEDIAN (F M) V)`, V = 0.30: DISPLAY 0.3, COMPUTE selected V).
Fix: `FunctionValueField.Median` takes the intermediate mode and returns `OfBinary64`; `RuntimeApi.FunctionFieldMedian` and the
MedianScaled arm of `FunctionDeviceText` pass `num.IntermediateMode`; unit tests pass `CobolRounding.NearestEven`. Golden
`pb4924_even_median_mean_is_one_binary64_kind_on_display_and_compute` (2002/2014/2023) and its `_below_2002` refusal. No inventory row closed.

