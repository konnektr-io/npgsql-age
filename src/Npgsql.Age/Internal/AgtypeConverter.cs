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
            return Encoding.UTF8.GetByteCount(str);
        }

        /// <summary>
        /// Read agtype from its binary representation.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected override Agtype ReadCore(PgReader reader)
        {
            ReadOnlySequence<byte> textBytes = reader.ReadBytes(reader.CurrentRemaining);
            string text = Encoding.UTF8.GetString(textBytes.ToArray());

            return new(text);
        }

        /// <summary>
        /// Write agtype to its binary representation.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        protected override void WriteCore(PgWriter writer, Agtype value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value.GetString());
            writer.WriteBytes(bytes);
        }
    }
#pragma warning restore NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
