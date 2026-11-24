## A national dynamic-capacity table has one layout code in both lanes

PB6138. The declared-group lane (`GroupImageCodec.ContiguousLayout`) passed layout code 0 for an `OCCURS DYNAMIC` table; the cell lane
(`FileModel.LayoutCodeOf`) passes the bare unit code of its elements (32 for a national element). Re-probe on this build: the declared lane emitted
`[0]` for `PIC N OCCURS DYNAMIC`. The wrong answer is unreachable: the code only picks the padding space in `ToFixedForm` / `Decompose`, which act on a
FILE record, and a dynamic-capacity table may not be defined in the file section (§2023:8.5.1.9.1 SR3). The fix is the consistency lead: the table arm now
calls `FileModel.LayoutCodeOf`, so the two lanes hand `CobolContiguousLayout.Of` the same inputs. `DynamicTableLayoutCodeTests` pins the emitted code
(32 national, 0 alphanumeric). No inventory row closes.
