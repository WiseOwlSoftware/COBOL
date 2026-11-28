// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Symbols;
using Antlr4.Runtime;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;

using Core = CobolParserCore;

/// <summary>The SET Formats 8 and 9 with an address sender: ENTRY, ADDRESS OF FUNCTION and ADDRESS OF PROGRAM (ISO §2023:8.4.3.12, §2023:8.4.3.13; kb/Work PB452, PB549): one binder of the SET format families <see cref="SetBinder"/> dispatches to (kb/Work
/// PB2314; DESIGN-architecture-review §8.3, R0-0019). <see cref="SetFormatSelection"/> chose the format before this binder runs.</summary>
internal sealed class SetAddressBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary><c>SET function-pointer… TO ADDRESS OF FUNCTION {function-prototype-name-1 | identifier-1}</c> —
    /// ISO §2023:14.9.39.2 Format 8 with the §2023:8.4.3.12 function-address-identifier as its sender. Without this the
    /// category has no non-NULL value it can ever hold (kb/Work PB452).
    /// <para>Both braced arms arrive as ONE <c>dataReference</c> and are told apart HERE, where the facts exist:
    /// §2023:8.4.3.12.3 SR2 makes the prototype arm a REPOSITORY function-specifier (plus §2023:8.4.6.6's containing-
    /// function spelling), and SR1 makes the identifier arm "of category alphanumeric or national". The
    /// prototype arm resolves at COMPILE time, so §2023:8.4.3.12.4 GR3 ("the function-address-identifier has the
    /// characteristics of a function-pointer restricted to function-prototype-name-1") puts it straight into
    /// SR20's same-signature compare; the identifier arm names the function at RUN time (GR1 a), which is
    /// exactly the case §2023:14.9.39.4 GR14's EC-FUNCTION-PTR-INVALID screen exists for.</para></summary>
    internal BoundStatement BindSetFunctionAddress(Core.SetFunctionAddressStatementContext sfa)
    {
        // ⚠ `drefs` IS the receiving-operand list and nothing else: the function-address-identifier is its own
        // grammar RULE (§2023:8.4.3.12.2 is an identifier format), so its operand lives under `fai`, exactly as the
        // program-address-identifier's does under BindSetProgramAddress.
        var drefs = SetFormatSelection.Receivers(sfa.setReceivingIdentifier());
        var fai = sfa.functionAddressIdentifier();
        if (drefs.Length < 1)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SET TO ADDRESS OF FUNCTION with no receiving operand (ISO §2023:14.9.39.2)");
        var targets = new List<Place>(drefs.Length);
        foreach (var dref in drefs)
        {
            if (host.Expr.ResolveReceiving(dref) is not { } tp || tp.Item.Pic?.Category is not PicCategory.FunctionPointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET '{WrittenSource.Of(dref)}': the receiving operand of SET … TO ADDRESS OF FUNCTION shall be "
                    + "USAGE FUNCTION-POINTER (ISO §2023:14.9.39.3 SR20 / §2023:8.4.3.12.4 GR1)");
            }
            targets.Add(tp);
        }
        // ⛔ SR20 IS ASKED OF EVERY RECEIVER, NEVER OF A REPRESENTATIVE ONE (kb/Work PB3224) — see the program twin.
        // SR20 applies to the receivers among themselves too — identifier-13 is ONE sender, so two receivers
        // restricted to differently-signed prototypes cannot both conform to it.
        if (ReceiversDisagreeing(targets, ctx.Symbols.FunctionSignatureOf) is { } clash)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                $"SET '{clash.A.Item.CobolName}' '{clash.B.Item.CobolName}' TO ADDRESS OF FUNCTION: the receiving "
                + $"function-pointers are restricted to function-prototypes '{clash.A.Item.Pic?.RestrictedPrototypeName}' "
                + $"and '{clash.B.Item.Pic?.RestrictedPrototypeName}', which do not have the same signature, so one "
                + "sender cannot satisfy both (ISO §2023:14.9.39.3 SR20)");
        }
        // §2023:14.9.39.4 GR14's run-time screen compares the stored address with ONE signature. The receivers agree
        // (the pair test above), so any RESTRICTED receiver states it; with none (every receiver unrestricted), the screen
        // is suppressed (ExpectedFormalsOf).
        int expectedFormals = ExpectedFormalsOf(targets.Select(t => ctx.Symbols.FunctionSignatureOf(t.Item)).FirstOrDefault(s => s is not null));

        // The receiving operands AS WRITTEN — every echo of the statement below names them (kb/Work PB388).
        string written = SetFormatSelection.Written(drefs);
        // ⛔ identifier-1 AS A FUNCTION-IDENTIFIER OR AN INLINE METHOD INVOCATION (§2023:8.4.3.1.3 SR1 — "that other
        // identifier may be any of the formats for an identifier"; kb/Work PB1452's sibling sweep). Neither can be a
        // function-prototype-name, so the prototype arm below is not asked; SR1's category screen is, through the ONE
        // reader the data-reference arm uses, over the data item the identifier references (§2023:8.4.3.2.1, §2023:8.4.3.4.4 GR1).
        if (((ParserRuleContext?)fai.functionCall() ?? fai.inlineMethodInvocation()) is { } idSyntax)
        {
            if (AddressNameOfIdentifier(idSyntax, $"SET {written} TO ADDRESS OF FUNCTION {WrittenSource.Of(idSyntax)}",
                    DiagnosticCatalog.FunctionAddressOperand, "8.4.3.12.3 SR1") is not { } idName)
                return BoundRejected.Reported(ctx.Edition);
            return new BoundSetFunctionAddress(targets, idName.Literal, idName.Place, expectedFormals);
        }
        var operand = fai.dataReference();
        string word = operand.GetText();
        string wordWritten = WrittenSource.Of(operand);
        // The PROTOTYPE arm (§2023:8.4.3.12.3 SR2 + §2023:8.4.6.6): a REPOSITORY function-specifier, or the containing
        // function definition's own user-function-name. The SAME two legs the USAGE clause's TO phrase resolves, through the
        // symbol table's one lookup (a function-prototype-name is one word: a qualified or subscripted operand is no such name).
        if (ctx.Symbols.Identify(operand, NameClass.FunctionPrototype, report: true)?.Symbol is FunctionPrototypeReference prototype)
        {
            // GR3 makes the sender a function-pointer restricted to this prototype, so SR20's compare is the
            // same one the pointer-to-pointer arm runs — one rule, one helper, asked of EVERY receiver.
            foreach (var t in targets)
            {
                string? receiverProto = t.Item.Pic?.RestrictedPrototypeName;
                if (!StatementBinder.SignaturesDiffer(ctx.Symbols.FunctionSignatureOf(t.Item), prototype.CalleeSignature)) continue;
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                    $"SET '{t.Item.CobolName}' TO ADDRESS OF FUNCTION {wordWritten}: the receiving function-pointer "
                    + $"is restricted to function-prototype '{receiverProto}' and the function-address-identifier has "
                    + $"the characteristics of a function-pointer restricted to '{wordWritten}' (ISO §2023:8.4.3.12.4 GR3), and "
                    + "the two do not have the same signature (ISO §2023:14.9.39.3 SR20)");
            }
            // §2023:8.4.3.12.4 GR2 names the EXTERNALIZED function-name as the address's identity, and that is what
            // the run-unit registry holds a unit under (§2023:11.5.4 GR1's AS literal-1 when one is written; the
            // word otherwise) — so the bound node carries the externalized spelling, never the source word
            // (kb/Work PB303, the same drift the CALL/ENTRY paths were fixed for).
            string externalized = prototype.Signature?.Externalized ?? word;
            return new BoundSetFunctionAddress(targets, externalized, null, expectedFormals);
        }
        // The IDENTIFIER arm (§2023:8.4.3.12.3 SR1): "Identifier-1 shall be of category alphanumeric or national."
        // A DEFERRED shape is the resolver's to announce; only a word that identifies nothing earns SR2's reading.
        if (host.Expr.ResolveSending(operand) is var nameAnswer && nameAnswer.Place is not { } namePlace)
        {
            if (nameAnswer.Outcome == RefOutcome.Deferred) return nameAnswer.Refusal(ctx.Edition);   // kb/Work PB1030
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.FunctionAddressOperand,
                $"SET {written} TO ADDRESS OF FUNCTION {wordWritten}: '{wordWritten}' is neither a function-prototype-name declared in "
                + "the REPOSITORY paragraph (ISO §2023:8.4.3.12.3 SR2 / §2023:8.4.6.6) nor a resolvable identifier "
                + "(§2023:8.4.3.12.3 SR1)");
        }
        if (!ScreenAddressName(namePlace, $"SET {written} TO ADDRESS OF FUNCTION {wordWritten}", wordWritten,
                DiagnosticCatalog.FunctionAddressOperand, "8.4.3.12.3 SR1"))
            return BoundRejected.Reported(ctx.Edition);
        return new BoundSetFunctionAddress(targets, null, namePlace, expectedFormals);
    }

    /// <summary>⛔ THE ONE §2023:8.4.3.12.3 SR1 / §2023:8.4.3.13.3 SR1 SCREEN — "Identifier-1 shall be of category alphanumeric
    /// or national" — over the data item the name operand of an address-identifier references (kb/Work PB1417 +
    /// PB1452). The function-address-identifier and the program-address-identifier print the same sentence, so one
    /// reader answers both: <see cref="ItemCategory.IsAlphanumericOrNational(Place, EditionInfo)"/>, the compiler's ONE category
    /// predicate for this rule. Both sites used to test the raw <c>Item.Pic?.Category</c>, which is null for a GROUP
    /// (an alphanumeric or national group item has class and category alphanumeric / national, §2023:8.5.2.1, and was
    /// refused as "category (none)") and which reads PIC A — category alphabetic, outside SR1's list — as
    /// alphanumeric, accepting it. A reference-modified operand takes §2023:8.4.3.3.4 GR6's category through the same
    /// predicate. Returns false having reported.</summary>
    /// <param name="prefix">The statement as the message opens it, ending before the colon.</param>
    private bool ScreenAddressName(Place name, string prefix, string word, DiagnosticDescriptor code, string rule)
    {
        if (ItemCategory.IsAlphanumericOrNational(name, ctx.Edition.Edition)) return true;
        ctx.Edition.Error(code,
            $"{prefix}: identifier-1 shall be of category alphanumeric or national (ISO §2023:{rule}) — '{word}' is {ItemCategory.Face(name)}");
        return false;
    }

    /// <summary>⛔ identifier-1 OF AN ADDRESS-IDENTIFIER WHEN IT IS A FUNCTION-IDENTIFIER OR AN INLINE METHOD
    /// INVOCATION (§2023:8.4.3.1.3 SR1; kb/Work PB1452) — bound ONCE (its activation registers a statement pre-op), then
    /// screened by <see cref="ScreenAddressName"/>'s rule over the data item it references: a user function's or an
    /// invocation's returned temporary is that item already (§2023:8.4.3.2.1, §2023:8.4.3.4.4 GR1); an intrinsic's character
    /// result is materialized into the §2023:15.4 returned-value temporary (<see cref="SendingValueTemp"/>); an intrinsic
    /// folded to its literal value is the compile-time name itself. A numeric (or other non-character) result is SR1's
    /// refusal. Null having reported; <c>Literal</c> or <c>Place</c> is set, never both.</summary>
    private (string? Literal, Place? Place)? AddressNameOfIdentifier(
        ParserRuleContext syntax, string prefix, DiagnosticDescriptor code, string rule)
    {
        var bound = syntax switch
        {
            Core.FunctionCallContext fc => host.Intrinsic.BindIntrinsic(fc),
            Core.InlineMethodInvocationContext imi => host.Oo.OoBindInlineInvocation(imi),
            _ => throw new InvalidOperationException(
                $"AddressNameOfIdentifier serves ISO §2023:8.4.3.1.2 Formats 1 and 4 only, not {syntax.GetType().Name}"),
        };
        if (bound is BoundExprError) return null;   // the identifier's own binder reported
        string text = WrittenSource.Of(syntax);
        var operand = IntrinsicBinder.OperandOf(bound, functionValue: syntax is Core.FunctionCallContext);
        if (operand is BoundStringLiteral { Category: PicCategory.Alphanumeric or PicCategory.National } folded)
            return (folded.Value, null);
        Place? place = operand is BoundFieldOperand f ? f.Place
            : IntrinsicResultType.OperandCategory(operand) is PicCategory.Alphanumeric or PicCategory.National
                ? host.SendingValue.Materialize(operand, "addrname")
                : null;
        if (place is not null && ScreenAddressName(place, prefix, text, code, rule)) return (null, place);
        if (place is null)
            ctx.Edition.Error(code,
                $"{prefix}: identifier-1 shall be of category alphanumeric or national (ISO §2023:{rule}) — '{text}' is of "
                + $"category {(IntrinsicResultType.OperandCategory(operand) is { } category ? ItemCategory.CategoryName(category) : "(none)")}");
        return null;
    }

    /// <summary>The run-time signature granularity of §2023:14.9.39.4 GR14 for a receiver restricted to a prototype of
    /// <paramref name="signature"/> (the symbol table's <see cref="SymbolTable.FunctionSignatureOf"/>, the ONE reader every
    /// function-pointer signature comparison asks, §2023:14.9.39.3 SR20) — the prototype's formal count, or −1 when no receiver is
    /// restricted or the name has no table entry (refused where it was declared; null either way). ⛔ −1 SUPPRESSES the emitted GR14 screen rather
    /// than defaulting it to zero: with no prototype there is nothing for "the same signature as the function referenced
    /// in the definition of identifier-13" to name. GR4's not-found screen still runs, because locating the function
    /// needs no signature.</summary>
    private static int ExpectedFormalsOf(CalleeSignature? signature) => signature is { } s ? s.Formals.Count : -1;

    /// <summary><c>SET program-pointer… TO ENTRY {literal | identifier}</c> — ⛔ THE MICRO FOCUS / IBM VENDOR
    /// SPELLING, NOT AN ISO FORMAT. This doc comment used to call it "ISO §2023:14.9.39 Format 9 with the §2023:8.4.3.13
    /// program-address-identifier sender" and it is neither (kb/Work PB549): the words <c>TO ENTRY</c> occur
    /// nowhere in ISO/IEC 1989:2023. The ISO surface is <see cref="BindSetProgramAddress"/>
    /// (<c>SET pp … TO ADDRESS OF PROGRAM …</c>). What IS shared is the SEMANTICS, and they are the
    /// standard's: every target shall be category program-pointer (§2023:14.9.39.3 SR21), and the operand names the
    /// program (§2023:8.4.3.13.4 GR1 — a literal, or an identifier whose VALUE names it per §2023:8.3.2.2). So the body
    /// is factored into <see cref="BindProgramAddressTargets"/> + <see cref="BoundSetEntry"/> and both
    /// surfaces reach it, rather than each carrying its own copy of SR21.</summary>
    internal BoundStatement BindSetEntry(Core.SetEntryStatementContext se)
    {
        // The LAST dataReference is the ENTRY identifier operand when no literal is present — the grammar
        // shape is `SET dataReference+ TO ENTRY (nonNumericLiteral | dataReference)`.
        var drefs = SetFormatSelection.Receivers(se.setReceivingIdentifier());
        // ⛔ The words `TO ENTRY` occur nowhere in ISO/IEC 1989 (the grammar's own note; kb/Work PB549) — a Micro Focus /
        // IBM spelling this compiler accepts for its program-pointer semantics, so the §2023:4.2.10 warning names it.
        ctx.Edition.Extension(ExtensionIds.SetProgramPointerToEntry, $"SET {SetFormatSelection.Written(drefs)} TO ENTRY");
        // The ENTRY operand's literal, as written or spelled by a constant-name (§2023:13.10.3 SR2, kb/Work PB780).
        ProgramNameOperand? entryLiteral = se.nonNumericLiteral() is { } nn ? ProgramNameLiteral.Of(host.Expr, nn, nn.GetText())
            : se.dataReference() is { } entryRef ? ProgramNameLiteral.Of(host.Expr, entryRef)
            : null;
        int targetCount = drefs.Length;
        if (targetCount < 1)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SET TO ENTRY with no receiving operand (ISO §2023:14.9.39.2)");
        if (BindProgramAddressTargets(drefs, targetCount, "SET … TO ENTRY") is not { } targets)
            return BoundRejected.Reported(ctx.Edition);
        // The receiving operands AS WRITTEN — a statement ECHO names them (kb/Work PB388); only the FORM
        // reference passed to BindProgramAddressTargets above keeps the general format's own `…`.
        string written = SetFormatSelection.Written(drefs[..targetCount]);
        if (entryLiteral is { } lit)
        {
            // The ONE program-name reader the ISO surface uses (§2023:8.4.3.13.3 SR2): an alphanumeric or national literal
            // whose length is not zero, its content FORMED as the run-time locate forms it.
            if (lit.Form(ctx.Edition.Edition.Year).Name is not { } entryName)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {written} TO ENTRY {lit.Spelling}: the ENTRY literal shall be an alphanumeric or national "
                    + "literal, not zero-length, naming a program (ISO §2023:8.4.3.13.3 SR2 / §2023:8.3.2.2)");
            }
            return RestrictedReceiverRefusesEntry(targets, written, lit.Spelling)
                ? BoundRejected.Reported(ctx.Edition)
                : new BoundSetEntry(targets, entryName, null);
        }
        // The resolver reported (or defers) the reference; "the ENTRY identifier is unresolvable" on top named
        // no rule the program broke (kb/Work PB1030).
        if (host.Expr.ResolveSending(se.dataReference()) is var entryAnswer && entryAnswer.Place is not { } namePlace)
            return entryAnswer.Refusal(ctx.Edition);
        return RestrictedReceiverRefusesEntry(targets, written, WrittenSource.Of(se.dataReference()))
            ? BoundRejected.Reported(ctx.Edition)
            : new BoundSetEntry(targets, null, namePlace);
    }

    /// <summary>⛔ THE VENDOR <c>TO ENTRY</c> SPELLING OBEYS THE CONTENT RULE OF A RESTRICTED PROGRAM-POINTER (kb/Work
    /// PB935, decided by the ISO text 2026-09-30). §2023:13.18.60.4 GR25 is an obligation on the ITEM, whichever statement
    /// loads it: "A restricted program-pointer shall contain only the predefined address NULL or the address of a
    /// program with the same signature as that identified by the specified program-prototype-name." The ENTRY operand
    /// names a program by VALUE (a literal, or an identifier whose content is the name), so it is an UNRESTRICTED
    /// sender — associated with no program-prototype, exactly the literal-1 and identifier-1 arms of the ISO
    /// <c>ADDRESS OF PROGRAM</c> form — and it is refused for a restricted receiver by the same helper
    /// (<see cref="RestrictedReceiverNeedsPrototype"/>). The conforming load is <c>SET pp TO ADDRESS OF PROGRAM
    /// program-prototype-name-1</c> (§2023:8.4.3.13.4 GR3). Returns true having reported.</summary>
    private bool RestrictedReceiverRefusesEntry(IReadOnlyList<Place> receivers, string written, string operandSpelling) =>
        RestrictedReceiverNeedsPrototype(receivers, $"SET {written} TO ENTRY {operandSpelling}",
            "the vendor TO ENTRY form names a program by value, so its sender is an unrestricted program-address and is "
            + "associated with no program-prototype; a restricted program-pointer shall contain only the predefined "
            + "address NULL or the address of a program with the same signature as its program-prototype "
            + "(ISO §2023:13.18.60.4 GR25; §2023:14.9.39.3 SR22), which SET … TO ADDRESS OF PROGRAM program-prototype-name-1 "
            + "establishes (ISO §2023:8.4.3.13.4 GR3)");

    /// <summary>§2023:14.9.39.3 SR21's receiving half — "Identifier-7 shall reference a data item of category
    /// program-pointer" — over the first <paramref name="count"/> of <paramref name="drefs"/>. ONE screen for
    /// the two surfaces that assign a program address (the ISO <c>ADDRESS OF PROGRAM</c> form and the vendor
    /// <c>TO ENTRY</c> form), so the rule cannot be enforced in one and forgotten in the other. Returns null
    /// having reported.</summary>
    private List<Place>? BindProgramAddressTargets(
        Core.DataReferenceContext[] drefs, int count, string where)
    {
        var targets = new List<Place>(count);
        for (int i = 0; i < count; i++)
        {
            if (host.Expr.ResolveReceiving(drefs[i]) is not { } tp || tp.Item.Pic?.Category is not PicCategory.ProgramPointer)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET '{WrittenSource.Of(drefs[i])}': the receiving operand of {where} shall be USAGE "
                    + "PROGRAM-POINTER (ISO §2023:14.9.39.2 Format 9 / §2023:14.9.39.3 SR21)");
                return null;
            }
            targets.Add(tp);
        }
        return targets;
    }

    /// <summary><c>SET { identifier-7 } … TO ADDRESS OF PROGRAM { identifier-1 | literal-1 |
    /// program-prototype-name-1 }</c> — ISO §2023:14.9.39.2 Format 9 with the §2023:8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER
    /// as its sender, the ONLY standard syntax that puts a program's address into a program-pointer. It had no
    /// grammar at any edition, so the conforming spelling was a bare parse error and the vendor
    /// <c>TO ENTRY</c> extension was the only route in (kb/Work PB549).
    /// <para>Three braced operand arms, three syntax rules, all on
    /// <see cref="DiagnosticCatalog.ProgramAddressOperand"/>: §2023:8.4.3.13.3 SR1 "Identifier-1 shall be of
    /// category alphanumeric or national", SR2 "Literal-1 shall be an alphanumeric or national literal whose
    /// length is not zero", SR3 "Program-prototype-name-1 shall be a program prototype specified in the
    /// REPOSITORY paragraph". The receiving operand's own category is §2023:14.9.39.3 SR21's and reports through
    /// <see cref="DiagnosticCatalog.PointerOperandShape"/>, as the vendor spelling's does.</para>
    /// <para>§2023:8.4.3.13.4 GR2 fixes what the address IS — "For a COBOL program, the address is that of the
    /// outermost program identified by the EXTERNALIZED program-name in its PROGRAM-ID paragraph" — so the
    /// prototype arm resolves through the §2023:12.3.8.2 program-specifier's externalized name (its
    /// <c>AS literal-3</c> when one is written), never the source word; that is the same drift kb/Work PB303
    /// fixed on the CALL and ENTRY paths. GR3 makes the prototype arm a program-pointer RESTRICTED to
    /// program-prototype-name-1, which is what §2023:14.9.39.3 SR22 then compares against a restricted
    /// receiver — the same compare the pointer-to-pointer arm runs, so it is the same helper.</para></summary>
    internal BoundStatement BindSetProgramAddress(Core.SetProgramAddressStatementContext spa)
    {
        var drefs = SetFormatSelection.Receivers(spa.setReceivingIdentifier());
        var pai = spa.programAddressIdentifier();
        var operandRef = pai.dataReference();
        // ⚠ `drefs` IS the receiving-operand list and nothing else. The program-address-identifier is its own
        // grammar RULE (§2023:8.4.3.13.2 is an identifier format, not a statement phrase), so its operand lives
        // under `pai`, not flattened into this context's dataReference list — unlike the FUNCTION twin, whose
        // sender phrase is written inline in setFunctionAddressStatement and therefore has to drop the last
        // element. Getting that wrong is silent: the last receiver is simply never screened or stored.
        if (drefs.Length < 1)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SET TO ADDRESS OF PROGRAM with no receiving operand (ISO §2023:14.9.39.2)");
        if (BindProgramAddressTargets(drefs, drefs.Length, "SET … TO ADDRESS OF PROGRAM") is not { } targets)
            return BoundRejected.Reported(ctx.Edition);
        // The receiving operands AS WRITTEN — every echo of the statement below names them, and the two
        // helpers this arm delegates to take it as a parameter rather than re-eliding it (kb/Work PB388).
        string written = SetFormatSelection.Written(drefs);

        // ⛔ SR22 IS ASKED OF EVERY RECEIVER, NEVER OF A REPRESENTATIVE ONE (kb/Work PB3224). Its condition is the
        // RECEIVER being restricted, and identifier-7 is a `{ } ...` list: each restricted receiver is compared with
        // the sender on its own, so the verdict cannot depend on which receiver was written first. (The first
        // receiver used to stand for the list; an unrestricted one made every compare vacuous, and
        // `SET UPP RP TO ADDRESS OF PROGRAM OTHER` compiled while `SET RP UPP TO ...` drew COBOL1959.)
        // SR22 also binds the receivers to ONE sender, so two receivers restricted to differently-signed
        // prototypes cannot both conform to it — the SR20 argument on the function twin, verbatim.
        if (ReceiversDisagreeing(targets, ctx.Symbols.ProgramSignatureOf) is { } clash)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                $"SET '{clash.A.Item.CobolName}' '{clash.B.Item.CobolName}' TO ADDRESS OF PROGRAM: the receiving "
                + $"program-pointers are restricted to program-prototypes '{clash.A.Item.Pic?.RestrictedPrototypeName}' "
                + $"and '{clash.B.Item.Pic?.RestrictedPrototypeName}', which do not have the same signature, so one "
                + "sender cannot satisfy both (ISO §2023:14.9.39.3 SR22)");
        }

        if (BindProgramAddressOperand(pai, $"SET {written} TO") is not { } operand) return BoundRejected.Reported(ctx.Edition);
        if (operand.Prototype is { } word)
        {
            // GR3: this identifier is a program-pointer RESTRICTED to `word`. SR22 then requires each restricted
            // receiver's prototype to have the same signature — an unrestricted receiver meets no condition.
            foreach (var t in targets)
            {
                if (t.Item.Pic?.RestrictedPrototypeName is not { } receiverProto
                    || !StatementBinder.SignaturesDiffer(ctx.Symbols.ProgramSignatureOf(t.Item), ctx.Symbols.ProgramSignatureOf(operand)))
                    continue;
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                    $"SET '{t.Item.CobolName}' TO ADDRESS OF PROGRAM {word}: the receiving "
                    + $"program-pointer is restricted to program-prototype '{receiverProto}' and the "
                    + $"program-address-identifier has the characteristics of a program-pointer restricted to "
                    + $"'{word}' (ISO §2023:8.4.3.13.4 GR3), and the two do not have the same signature "
                    + "(ISO §2023:14.9.39.3 SR22)");
            }
            return new BoundSetEntry(targets, operand.NameLiteral, null);
        }
        string senderWhat = operand.NamePlace is null
            ? $"literal \"{operand.NameLiteral}\""
            : $"identifier '{WrittenSource.Of(operandRef ?? (ParserRuleContext?)pai.functionCall() ?? pai.inlineMethodInvocation()!)}'";
        return RestrictedReceiverNeedsPrototype(targets, $"SET {written} TO ADDRESS OF PROGRAM {senderWhat}",
                "only the program-prototype-name-1 arm of a program-address-identifier is itself restricted "
                + "(ISO §2023:8.4.3.13.4 GR3); an unrestricted sender is associated with no program-prototype, so the two "
                + "cannot have the same signature (ISO §2023:14.9.39.3 SR22)")
            ? BoundRejected.Reported(ctx.Edition)
            : new BoundSetEntry(targets, operand.NameLiteral, operand.NamePlace);
    }

    /// <summary>⛔ THE ONE BINDER FOR A §2023:8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER OPERAND (kb/Work PB239) — the SET
    /// Format-9 sender and the CALL argument (§2023:14.9.4.3 SR3/SR4, an address-identifier being §2023:8.4.3.1.2
    /// identifier Format 9) both bind through it, so the three braced arms and their three syntax rules are
    /// stated once. <paramref name="site"/> is the statement text that precedes the identifier in a message
    /// (<c>SET P TO</c>, <c>CALL … USING</c>). Null having reported.
    /// <para>Three braced operand arms, three syntax rules, all on
    /// <see cref="DiagnosticCatalog.ProgramAddressOperand"/>: SR1 identifier-1, SR2 literal-1, SR3
    /// program-prototype-name-1 — asked FIRST, like the function twin: the name is a user-defined word in a slot
    /// where the standard lists the prototype meaning, and §2023:8.3.2.2 keeps the two name types apart by
    /// context.</para></summary>
    internal BoundProgramAddress? BindProgramAddressOperand(Core.ProgramAddressIdentifierContext pai, string site)
    {
        // ── ARM 2: literal-1 (§2023:8.4.3.13.3 SR2 / §2023:8.4.3.13.4 GR1b) — as written, or spelled by a constant-name, which
        //    "may be used anywhere that a format specifies a literal" (§2023:13.10.3 SR2, kb/Work PB780). The alias is asked
        //    before ARM 3, so a word the element declares as a constant is never an inherited prototype's name. ──
        ProgramNameOperand? literal1 = pai.nonNumericLiteral() is { } nn ? ProgramNameLiteral.Of(host.Expr, nn, nn.GetText())
            : pai.dataReference() is { } aliasRef ? ProgramNameLiteral.Of(host.Expr, aliasRef)
            : null;
        if (literal1 is { } lit)
            return ProgramAddressLiteral(lit, site) is { } value ? new BoundProgramAddress(value, null, null) : null;

        // ── ARM 1b: identifier-1 written as a function-identifier or an inline method invocation (§2023:8.4.3.1.3 SR1;
        //    kb/Work PB1452) — a reference to a data item like any other, so SR1's category screen is the same one. ──
        if (((ParserRuleContext?)pai.functionCall() ?? pai.inlineMethodInvocation()) is { } idSyntax)
            return AddressNameOfIdentifier(idSyntax, $"{site} ADDRESS OF PROGRAM {WrittenSource.Of(idSyntax)}",
                    DiagnosticCatalog.ProgramAddressOperand, "8.4.3.13.3 SR1") is { } idName
                ? new BoundProgramAddress(idName.Literal, idName.Place, null)
                : null;

        var operandRef = pai.dataReference();
        string word = operandRef.GetText();
        string wordWritten = WrittenSource.Of(operandRef);
        // ── ARM 3: program-prototype-name-1 (§2023:8.4.3.13.3 SR3) — GR2's EXTERNALIZED program-name, never the
        //    source word (kb/Work PB303). ──
        if (ctx.Symbols.Identify(operandRef, NameClass.ProgramPrototype, report: true)?.Symbol is ProgramPrototype proto)
            return new BoundProgramAddress(proto.ExternalizedName, null, word);

        // ── A word a PROGRAM specifier declares that names no data item has no prototype: the sentence names the route
        //    (the one CALL and CANCEL give, kb/Work PB4708), and the resolver, which would add its own COBOL1639 "no
        //    declaration gives the name" to a word that IS declared, is not asked. ──
        if (ctx.Refs.Probe(operandRef) is null && ctx.Symbols.DeclaresProgramSpecifier(word))
        {
            if (ctx.Symbols.IsRefusedProgramReference(word, ctx.Edition)) return null;   // reported at the specifier (kb/Work PB4936)
            ctx.Edition.Error(DiagnosticCatalog.ProgramAddressOperand,
                $"{site} ADDRESS OF PROGRAM {wordWritten}: '{wordWritten}' is no program-prototype-name here — "
                + $"{ctx.Symbols.WhyNoPrototype(word)} (ISO §2023:8.4.3.13.3 SR3) — and no resolvable identifier (SR1)");
            return null;
        }
        // ── ARM 1: identifier-1 (§2023:8.4.3.13.3 SR1 / §2023:8.4.3.13.4 GR1a) ───────────────────────────────────────
        // A DEFERRED shape is the resolver's to announce; only a word that identifies nothing earns SR3's reading.
        if (host.Expr.ResolveSending(operandRef) is var nameAnswer && nameAnswer.Place is not { } namePlace)
        {
            if (nameAnswer.Outcome == RefOutcome.Deferred) { nameAnswer.PlaceOrReported(ctx.Edition); return null; }   // 0899, reported (kb/Work PB1030)
            ctx.Edition.Error(DiagnosticCatalog.ProgramAddressOperand,
                $"{site} ADDRESS OF PROGRAM {wordWritten}: '{wordWritten}' is neither a program-prototype-name declared in "
                + "the REPOSITORY paragraph (ISO §2023:8.4.3.13.3 SR3) nor a resolvable identifier (SR1)");
            return null;
        }
        if (!ScreenAddressName(namePlace, $"{site} ADDRESS OF PROGRAM {wordWritten}", wordWritten,
                DiagnosticCatalog.ProgramAddressOperand, "8.4.3.13.3 SR1"))
            return null;
        return new BoundProgramAddress(null, namePlace, null);
    }

    /// <summary>§2023:8.4.3.13.3 SR2 over the literal arm of a program-address-identifier: "Literal-1 shall be an
    /// alphanumeric or national literal whose length is not zero." The figurative constants and the boolean
    /// literal are excluded by the same sentence — none of them is an alphanumeric or national literal — and a
    /// §2023:8.8.3 concatenation expression has folded, because §2023:8.8.3.3 GR3 makes it "equivalent to a literal of
    /// the same class and value". The name is FORMED by the ONE program-name reader CALL and CANCEL use
    /// (<see cref="ProgramNameLiteral"/>): GR1 sends literal-1 through 8.3.2.2 exactly as §2023:14.9.4.4 GR3b does.
    /// Returns null having reported.</summary>
    private string? ProgramAddressLiteral(ProgramNameOperand literal1, string site)
    {
        var (name, fault) = literal1.Form(ctx.Edition.Edition.Year);
        if (fault != ProgramNameFault.None)
        {
            ctx.Edition.Error(DiagnosticCatalog.ProgramAddressOperand,
                $"{site} ADDRESS OF PROGRAM {literal1.Spelling}: literal-1 shall be an alphanumeric or national "
                + "literal whose length is not zero (ISO §2023:8.4.3.13.3 SR2)");
            return null;
        }
        return name;
    }

    /// <summary>§2023:14.9.39.3 SR22 against an UNRESTRICTED program-address sender, one helper for every spelling that
    /// loads one. Only §2023:8.4.3.13.4 GR3's prototype arm gives a program-address-identifier a restriction, so an
    /// identifier-1 or literal-1 sender is an UNRESTRICTED program-pointer: SR22 requires "the program-prototypes
    /// associated with identifier-7 and identifier-8" to have the same signature, and an unrestricted pointer is
    /// associated with none — the same reading <see cref="BindSetProgramPointer"/> enforces.
    /// Returns true having reported.
    /// <para>⚠ BOTH SPELLINGS REACH IT (kb/Work PB935). The ISO literal-1 / identifier-1 arms of
    /// <see cref="BindSetProgramAddress"/> pass the SR22 / GR3 wording; the vendor <c>SET pp TO ENTRY</c> form
    /// (<see cref="BindSetEntry"/>) passes §2023:13.18.60.4 GR25's, because its words are in no ISO general format and so
    /// SR22 has no identifier-8 to name there, but GR25 binds the receiving ITEM whatever loads it. The
    /// load of a restricted program-pointer that conforms is <c>ADDRESS OF PROGRAM program-prototype-name-1</c>.</para>
    /// </summary>
    /// <param name="statement">The statement as written up to and including its sender, e.g. <c>SET PP TO ENTRY "X"</c>.</param>
    /// <param name="why">The rule sentence that follows the receiver's restriction in the message, citations included.</param>
    private bool RestrictedReceiverNeedsPrototype(IReadOnlyList<Place> receivers, string statement, string why)
    {
        // The FIRST restricted receiver is named; one is enough to refuse, and which one is named is the only thing
        // the order of the list decides (kb/Work PB3224).
        if (receivers.Select(r => r.Item.Pic?.RestrictedPrototypeName).FirstOrDefault(n => n is not null) is not { } receiverProto)
            return false;
        ctx.Edition.Error(DiagnosticCatalog.PrototypePointerSignature,
            $"{statement}: the receiving program-pointer is restricted to program-prototype '{receiverProto}', and {why}");
        return true;
    }

    /// <summary>⛔ SR20 / SR22 AMONG THE RECEIVERS OF ONE ADDRESS SENDER, ORDER-FREE (kb/Work PB3224). The standard
    /// binds every receiver to the ONE sender's signature, so two receivers restricted to prototypes of different
    /// signatures cannot both conform to it, whatever the sender. Every PAIR is compared (not each receiver against
    /// the first): <see cref="StatementBinder.SignaturesDiffer"/> raises nothing for an unrestricted receiver (or one
    /// whose prototype name was refused where it was declared), so a representative of that kind would hide a clash
    /// between the rest. Returns the first disagreeing pair in source order, or null.</summary>
    /// <param name="signatureOf">The restricted pointer → compile-time signature of the prototype its USAGE TO phrase names
    /// (program or function), null for an unrestricted pointer or a name with none.</param>
    private static (Place A, Place B)? ReceiversDisagreeing(IReadOnlyList<Place> receivers,
                                                            Func<DataItem, CalleeSignature?> signatureOf)
    {
        for (int i = 0; i < receivers.Count; i++)
            for (int j = i + 1; j < receivers.Count; j++)
                if (StatementBinder.SignaturesDiffer(signatureOf(receivers[i].Item), signatureOf(receivers[j].Item)))
                    return (receivers[i], receivers[j]);
        return null;
    }
}
