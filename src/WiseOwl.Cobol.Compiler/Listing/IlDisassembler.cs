// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ THE ONE DECODER OF IL (DESIGN-listing.md section 6.9): the instructions of a stretch of a method body, each with its offset, its name
/// and its operand resolved through the assembly's metadata (ECMA-335 partition III). It reads only the produced assembly, so it is the
/// true object code under every backend.
/// </summary>
/// <remarks>The instruction set is the runtime's own table (<see cref="OpCodes"/>: each instruction's value, name and operand type), read
/// once, so no list of opcodes is written here.</remarks>
internal static class IlDisassembler
{
    private static readonly IReadOnlyDictionary<ushort, OpCode> s_opCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => unchecked((ushort)o.Value));

    /// <summary>The instructions of <paramref name="il"/> (a method body's IL) from offset <paramref name="start"/> up to
    /// <paramref name="end"/>, their operands named through <paramref name="metadata"/>.</summary>
    public static IReadOnlyList<ListingInstruction> Decode(MetadataReader metadata, ImmutableArray<byte> il, int start, int end)
    {
        var instructions = new List<ListingInstruction>();
        int at = 0;
        while (at < end && at < il.Length)
        {
            int offset = at;
            ushort value = il[at++];
            if (value == 0xFE && at < il.Length) value = (ushort)(0xFE00 | il[at++]);
            if (!s_opCodes.TryGetValue(value, out var opCode))
                throw new BadImageFormatException(string.Create(CultureInfo.InvariantCulture, $"IL byte 0x{value:X2} at offset {offset} is no instruction"));
            string? operand = Operand(metadata, il, ref at, opCode.OperandType);
            if (offset >= start) instructions.Add(new ListingInstruction(offset, opCode.Name!, operand));
        }
        return instructions;
    }

    /// <summary>The operand of an instruction of <paramref name="type"/> at <paramref name="at"/>, which it moves past the operand.</summary>
    private static string? Operand(MetadataReader metadata, ImmutableArray<byte> il, ref int at, OperandType type)
    {
        switch (type)
        {
            case OperandType.InlineNone:
                return null;
            case OperandType.ShortInlineBrTarget:
            {
                int delta = unchecked((sbyte)il[at++]);
                return Target(at + delta);
            }
            case OperandType.InlineBrTarget:
            {
                int delta = ReadInt32(il, ref at);
                return Target(at + delta);
            }
            case OperandType.ShortInlineI:
                return unchecked((sbyte)il[at++]).ToString(CultureInfo.InvariantCulture);
            case OperandType.ShortInlineVar:
                return il[at++].ToString(CultureInfo.InvariantCulture);
            case OperandType.InlineVar:
            {
                int index = il[at] | (il[at + 1] << 8);
                at += 2;
                return index.ToString(CultureInfo.InvariantCulture);
            }
            case OperandType.InlineI:
                return ReadInt32(il, ref at).ToString(CultureInfo.InvariantCulture);
            case OperandType.InlineI8:
            {
                long low = (uint)ReadInt32(il, ref at), high = ReadInt32(il, ref at);
                return (low | (high << 32)).ToString(CultureInfo.InvariantCulture);
            }
            case OperandType.ShortInlineR:
            {
                float value = BitConverter.Int32BitsToSingle(ReadInt32(il, ref at));
                return value.ToString("R", CultureInfo.InvariantCulture);
            }
            case OperandType.InlineR:
            {
                long low = (uint)ReadInt32(il, ref at), high = ReadInt32(il, ref at);
                return BitConverter.Int64BitsToDouble(low | (high << 32)).ToString("R", CultureInfo.InvariantCulture);
            }
            case OperandType.InlineString:
                return Quoted(metadata.GetUserString(MetadataTokens.UserStringHandle(ReadInt32(il, ref at) & 0x00FFFFFF)));
            case OperandType.InlineSwitch:
            {
                int count = ReadInt32(il, ref at);
                int origin = at + 4 * count;
                var targets = new string[count];
                for (int i = 0; i < count; i++) targets[i] = Target(origin + ReadInt32(il, ref at));
                return "(" + string.Join(", ", targets) + ")";
            }
            case OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineType or OperandType.InlineTok
                or OperandType.InlineSig:
                return MetadataNames.Of(metadata, MetadataTokens.EntityHandle(ReadInt32(il, ref at)));
            default:
                throw new BadImageFormatException($"an IL operand of type {type}");
        }
    }

    private static int ReadInt32(ImmutableArray<byte> il, ref int at)
    {
        int value = il[at] | (il[at + 1] << 8) | (il[at + 2] << 16) | (il[at + 3] << 24);
        at += 4;
        return value;
    }

    private static string Target(int offset) => string.Create(CultureInfo.InvariantCulture, $"IL_{offset:X4}");

    /// <summary>A string operand as ILDASM writes one: in quotation marks, a quotation mark, a backslash and a control character
    /// escaped.</summary>
    private static string Quoted(string value)
    {
        var text = new StringBuilder("\"");
        foreach (char c in value)
            text.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when char.IsControl(c) => string.Create(CultureInfo.InvariantCulture, $"\\u{(int)c:X4}"),
                _ => c.ToString(),
            });
        return text.Append('"').ToString();
    }
}
