// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>The VALIDATE statement emitter (ISO/IEC 1989:2002 §14.8.46; §2023:14.9.50; docs/rearchitecture/DESIGN-validate-facility.md). It lays out
/// the five stages of §2023:14.9.50.4 GR4 as passes over the ONE element list the binder expanded (<see cref="BoundValidate.Steps"/>): "Each stage is
/// executed for all the elements of the operand before each next stage", so a stage is a pass over every element and never a pass per element.
/// Each stage's TEST is a bound condition the one condition renderer renders; the emitter owns only the internal indicator (GR5) and the order.
/// <para>Stage one (format validation) and stage five (error indication, EC-VALIDATE-FORMAT) are built. The internal indicator of an element is one C#
/// local assigned the initial valid value at the start of the statement and set to <see cref="InvalidOnFormat"/> only from valid (GR5: "An indicator is
/// set to indicate an error only if it is in its initial valid state"); an element inside a table's loop keeps ONE indicator for its occurrences, which
/// is all stage five needs ("If any of these internal indicators is set to invalid on format…"). Stages three and four read a PER-OCCURRENCE indicator
/// ("An elementary item that is invalid on format does not undergo a content check"), which they will introduce with their tests. The statement
/// changes no content (GR5: "the content of the invalid data item does not change"), so with the condition's checking off it has no effect and
/// emits nothing.</para></summary>
internal sealed class ValidateEmitter(EmitContext ctx, ConditionRenderer cond, EcEmitter ec)
{
    /// <summary>The initial valid value of an element's internal indicator (§2023:14.9.50.4 GR5).</summary>
    private const int Valid = 0;

    /// <summary>"invalid on format" (§2023:14.9.50.4 GR5).</summary>
    private const int InvalidOnFormat = 1;

    private const string FormatEc = "EC-VALIDATE-FORMAT";

    public void Emit(BoundValidate v)
    {
        var w = ctx.Writer;
        if (!ec.EnabledHere(FormatEc))
        {
            w.Line("// VALIDATE: no exception-name of the facility is enabled at this statement, so no stage has an effect (ISO §2023:14.9.50.4 GR5, GR6 e)");
            return;
        }

        int id = ctx.Names.NextEc();
        var indicators = new Dictionary<ValidateElement, string>(ReferenceEqualityComparer.Instance);
        foreach (var element in v.Elements)
        {
            string indicator = $"__vi{id}_{indicators.Count}";
            indicators[element] = indicator;
            w.Line($"int {indicator} = {Valid};   // internal indicator, initially valid (ISO §2023:14.9.50.4 GR5)");
        }

        // Stage one — format validation (GR6 a): every element with a PICTURE, SIGN or USAGE check, in definition order.
        w.Line("// VALIDATE — stage one, format validation (ISO §2023:14.9.50.4 GR6 a; §2002:14.8.46.3 GR6 a)");
        EmitFormat(v.Steps, indicators);

        // Stage five — error indication (GR6 e): "If any of these internal indicators is set to invalid on format, an EC-VALIDATE-FORMAT exception
        // condition is set to exist". EC-VALIDATE-FORMAT is nonfatal, so execution continues as if it had not been set (GR6).
        w.Line("// VALIDATE — stage five, error indication (ISO §2023:14.9.50.4 GR6 e)");
        using (w.Block($"if ({string.Join(" || ", indicators.Values.Select(i => $"{i} == {InvalidOnFormat}"))})"))
            ec.EmitConditionRaise(FormatEc, "VALIDATE: an element is invalid on format (ISO §2023:14.9.50.4 GR6 e)");
    }

    private void EmitFormat(IReadOnlyList<ValidateStep> steps, Dictionary<ValidateElement, string> indicators)
    {
        var w = ctx.Writer;
        foreach (var step in steps)
            switch (step)
            {
                case ValidateElement e:
                    // "An indicator is set to indicate an error only if it is in its initial valid state" (GR5 NOTE).
                    w.Line($"if (!({cond.Render(e.FormatValid)}) && {indicators[e]} == {Valid}) {indicators[e]} = {InvalidOnFormat};   // invalid on format; the content is unchanged (GR5)");
                    break;
                case ValidateLoop l:
                    string i = LocalNames.Of(l.Var);
                    using (w.Block($"for (long {i} = 1; {i} <= {PlaceRenderer.OccurrenceCount(l.Count)}; {i}++)"))
                        EmitFormat(l.Body, indicators);
                    break;
            }
    }
}
