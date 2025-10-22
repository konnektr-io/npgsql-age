using System;
using System.Buffers;
using System.Text;
using Npgsql.Age.Types;
using Npgsql.Internal;

namespace Npgsql.Age.Internal
{
#pragma warning disable NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    internal class AgtypeConverter : PgBufferedConverter<Agtype>
    {
        public override bool CanConvert(
            DataFormat format,
            out BufferRequirements bufferRequirements
        )
        {
            bufferRequirements = BufferRequirements.None;
            return format is DataFormat.Text || format is DataFormat.Binary;
        }

        public override Size GetSize(SizeContext context, Agtype value, ref object? writeState)
        {
            var str = value.GetString();
            // Add 1 byte for the version number prefix
            return Encoding.UTF8.GetByteCount(str) + 1;
        }

        /// <summary>
        /// Read agtype from its binary representation.
        /// Apache AGE expects: version byte (1) + text content
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected override Agtype ReadCore(PgReader reader)
        {
            // Read the version byte (should be 1)
            byte version = reader.ReadByte();
            if (version != 1)
            {
                throw new NotSupportedException($"Unsupported agtype version number {version}");
            }

            // Read the remaining text content
            ReadOnlySequence<byte> textBytes = reader.ReadBytes(reader.CurrentRemaining);
            string text = Encoding.UTF8.GetString(textBytes.ToArray());

            return new(text);
        }

        /// <summary>
        /// Write agtype to its binary representation.
        /// Apache AGE format: version byte (1) + text content
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        protected override void WriteCore(PgWriter writer, Agtype value)
        {
            // Write version number as first byte (version 1)
            writer.WriteByte(1);

            // Write the text content
            byte[] bytes = Encoding.UTF8.GetBytes(value.GetString());
            writer.WriteBytes(bytes);
        }
    }
#pragma warning restore NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
