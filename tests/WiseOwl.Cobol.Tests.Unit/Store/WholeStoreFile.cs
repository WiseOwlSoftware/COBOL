// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;

namespace WiseOwl.Cobol.Tests.Unit.Store;

/// <summary>A file of the DELETED whole-store format (versions 1 to 5; kb/Work PB3335): the magic <c>CBNFSTR</c> at offset 0,
/// the version byte, the organization, the record type, the minimum and maximum record sizes, a key count of 0, a
/// generation and a release mint (36 bytes), then one record's frame. No code of the runtime writes or reads this format
/// any longer — its codec was deleted with the whole-store model — so a case writes the bytes itself, to show that such a
/// file is FOREIGN to the paged store's reader (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.3 decision 1: no reader
/// of the old format is kept).</summary>
internal static class WholeStoreFile
{
    public static byte[] Bytes(char organization, byte version, string record)
    {
        var head = new byte[36];
        "CBNFSTR"u8.CopyTo(head);
        head[7] = version;
        head[8] = (byte)organization;
        head[9] = (byte)'F';
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(10), record.Length);
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(14), record.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(head.AsSpan(20), 1);   // the generation
        var frame = new byte[4 + record.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, (uint)record.Length);
        Encoding.Latin1.GetBytes(record).CopyTo(frame, 4);
        return [.. head, .. frame];
    }
}
