using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Npgsql;
using Npgsql.BackendMessages;
using Npgsql.PostgresTypes;
using Npgsql.TypeHandling;
using Npgsql.TypeMapping;
using NpgsqlTypes;

namespace Meta.Common.Extensions
{
    public static class PgsqlTypeMappingExtensions
    {
        internal static string Sql = @"

SELECT ns.nspname, a.typname, a.oid, a.typbasetype, a.typnotnull,
CASE WHEN pg_proc.proname='array_recv' THEN 'a' ELSE a.typtype END AS typtype,
CASE
  WHEN pg_proc.proname='array_recv' THEN a.typelem
  ELSE 0
END AS typelem,
CASE
  WHEN pg_proc.proname IN ('array_recv','oidvectorrecv') THEN 3    /* Arrays before */
  WHEN a.typtype='r' THEN 2                                        /* Ranges before */
  WHEN a.typtype='d' THEN 1                                        /* Domains before */
  ELSE 0                                                           /* Base types first */
END AS ord
FROM pg_type AS a
JOIN pg_namespace AS ns ON (ns.oid = a.typnamespace)
JOIN pg_proc ON pg_proc.oid = a.typreceive
LEFT OUTER JOIN pg_class AS cls ON (cls.oid = a.typrelid)
LEFT OUTER JOIN pg_type AS b ON (b.oid = a.typelem)
LEFT OUTER JOIN pg_class AS elemcls ON (elemcls.oid = b.typrelid)
WHERE
  a.typname = 'xml' and a.typtype = 'b'
ORDER BY typname;
";
        //       a.typtype IN('b', 'r', 'e', 'd') OR         /* Base, range, enum, domain */
        //(a.typtype = 'c' AND cls.relkind= 'c') OR
        // /* User-defined free-standing composites (not table composites) by default */
        // (pg_proc.proname= 'array_recv' AND (
        //   b.typtype IN ('b', 'r', 'e', 'd') OR       /* Array of base, range, enum, domain */
        //   (b.typtype = 'p' AND b.typname IN ('record', 'void')) OR /* Arrays of special supported pseudo-types */
        //   (b.typtype = 'c' AND elemcls.relkind= 'c')  /* Array of user-defined free-standing composites (not table composites) */
        // )) OR
        // (a.typtype = 'p' AND a.typname IN ('record', 'void'))  /* Some special supported pseudo-types */
        public static void UseCustomXml(this INpgsqlTypeMapper map)
        {
            map.RemoveMapping("xml");
            map.AddMapping(new NpgsqlTypeMappingBuilder
            {
                PgTypeName = "xml",
                NpgsqlDbType = NpgsqlDbType.Xml,
                ClrTypes = new[] { typeof(XmlDocument) },
                TypeHandlerFactory = new XmlHandlerFactory()
            }.Build());
        }
    }
    public class XmlHandlerFactory : NpgsqlTypeHandlerFactory<XmlDocument>
    {
        private static PostgresTypeModel _xmlType = null;

        private PostgresTypeModel GetXmlTypeModel(NpgsqlConnection conn)
        {
            var info = new PostgresTypeModel();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            using var cmd = new NpgsqlCommand(PgsqlTypeMappingExtensions.Sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                info.Namespace = reader.GetString(0);
                info.InternalName = reader.GetString(1);
                info.Oid = Convert.ToUInt32(reader.GetValue(2));
                break;
            }
            return info;
        }
        public override NpgsqlTypeHandler<XmlDocument> Create(PostgresType pgType, NpgsqlConnection conn)
        {
            if (_xmlType == null)
                _xmlType = GetXmlTypeModel(conn);

            return new XmlHandler(new PostgresXmlType(_xmlType.Namespace, _xmlType.InternalName, _xmlType.Oid));
        }
        public class PostgresTypeModel
        {
            public string Namespace { get; set; }
            public string InternalName { get; set; }
            public uint Oid { get; set; }
        }
    }
    internal class PostgresXmlType : PostgresBaseType
    {
        protected internal PostgresXmlType(string ns, string internalName, uint oid) : base(ns, internalName, oid)
        {
        }
    }
    internal class XmlHandler : NpgsqlTypeHandler<XmlDocument>, INpgsqlTypeHandler<XmlDocument>
    {
        public XmlHandler(PostgresType postgresType) : base(postgresType)
        {
        }

        public override ValueTask<XmlDocument> Read(NpgsqlReadBuffer buf, int len, bool async, FieldDescription fieldDescription = null)
        {
            var xmlStr = buf.ReadString(len);
            var xml = new XmlDocument();
            if (string.IsNullOrEmpty(xmlStr))
                xml.LoadXml(xmlStr);
            return new ValueTask<XmlDocument>(xml);
        }

        public override int ValidateAndGetLength(XmlDocument value, ref NpgsqlLengthCache lengthCache, NpgsqlParameter parameter)
        {
            return value.InnerXml.Length;
        }

        public override Task Write(XmlDocument value, NpgsqlWriteBuffer buf, NpgsqlLengthCache lengthCache, NpgsqlParameter parameter, bool async)
        {
            var xmlStr = value.InnerXml;
            var charLen = parameter == null || parameter.Size <= 0 || parameter.Size >= xmlStr.Length ? xmlStr.Length : parameter.Size;
            return buf.WriteString(xmlStr, charLen, async);
        }
    }/*
    public class TextHandler : NpgsqlTypeHandler<string>, INpgsqlTypeHandler<char[]>, INpgsqlTypeHandler<ArraySegment<char>>,
      INpgsqlTypeHandler<char>, INpgsqlTypeHandler<byte[]>, ITextReaderHandler
    {
        // Text types are handled a bit more efficiently when sent as text than as binary
        // see https://github.com/npgsql/npgsql/issues/1210#issuecomment-235641670
        internal override bool PreferTextWrite => true;

        readonly Encoding _encoding;

        #region State

        readonly char[] _singleCharArray = new char[1];

        #endregion

        /// <inheritdoc />
        protected internal TextHandler(PostgresType postgresType, NpgsqlConnection connection)
            : this(postgresType, connection.Connector!.TextEncoding) { }

        /// <inheritdoc />
        protected internal TextHandler(PostgresType postgresType, Encoding encoding)
            : base(postgresType) => _encoding = encoding;

        #region Read

        /// <inheritdoc />
        public override ValueTask<string> Read(NpgsqlReadBuffer buf, int byteLen, bool async, FieldDescription? fieldDescription = null)
        {
            return buf.ReadBytesLeft >= byteLen
                ? new ValueTask<string>(buf.ReadString(byteLen))
                : ReadLong();

            async ValueTask<string> ReadLong()
            {
                if (byteLen <= buf.Size)
                {
                    // The string's byte representation can fit in our read buffer, read it.
                    while (buf.ReadBytesLeft < byteLen)
                        await buf.ReadMore(async);
                    return buf.ReadString(byteLen);
                }

                // Bad case: the string's byte representation doesn't fit in our buffer.
                // This is rare - will only happen in CommandBehavior.Sequential mode (otherwise the
                // entire row is in memory). Tweaking the buffer length via the connection string can
                // help avoid this.

                // Allocate a temporary byte buffer to hold the entire string and read it in chunks.
                var tempBuf = new byte[byteLen];
                var pos = 0;
                while (true)
                {
                    var len = Math.Min(buf.ReadBytesLeft, byteLen - pos);
                    buf.ReadBytes(tempBuf, pos, len);
                    pos += len;
                    if (pos < byteLen)
                    {
                        await buf.ReadMore(async);
                        continue;
                    }
                    break;
                }
                return buf.TextEncoding.GetString(tempBuf);
            }
        }

        async ValueTask<char[]> INpgsqlTypeHandler<char[]>.Read(NpgsqlReadBuffer buf, int byteLen, bool async, FieldDescription? fieldDescription)
        {
            if (byteLen <= buf.Size)
            {
                // The string's byte representation can fit in our read buffer, read it.
                while (buf.ReadBytesLeft < byteLen)
                    await buf.ReadMore(async);
                return buf.ReadChars(byteLen);
            }

            // TODO: The following can be optimized with Decoder - no need to allocate a byte[]
            var tempBuf = new byte[byteLen];
            var pos = 0;
            while (true)
            {
                var len = Math.Min(buf.ReadBytesLeft, byteLen - pos);
                buf.ReadBytes(tempBuf, pos, len);
                pos += len;
                if (pos < byteLen)
                {
                    await buf.ReadMore(async);
                    continue;
                }
                break;
            }
            return buf.TextEncoding.GetChars(tempBuf);
        }

        async ValueTask<char> INpgsqlTypeHandler<char>.Read(NpgsqlReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
        {
            // Make sure we have enough bytes in the buffer for a single character
            var maxBytes = Math.Min(buf.TextEncoding.GetMaxByteCount(1), len);
            while (buf.ReadBytesLeft < maxBytes)
                await buf.ReadMore(async);

            var decoder = buf.TextEncoding.GetDecoder();
            decoder.Convert(buf.Buffer, buf.ReadPosition, maxBytes, _singleCharArray, 0, 1, true, out var bytesUsed, out var charsUsed, out _);
            buf.Skip(len - bytesUsed);

            if (charsUsed < 1)
                throw new NpgsqlSafeReadException(new NpgsqlException("Could not read char - string was empty"));

            return _singleCharArray[0];
        }

        ValueTask<ArraySegment<char>> INpgsqlTypeHandler<ArraySegment<char>>.Read(NpgsqlReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
        {
            buf.Skip(len);
            throw new NpgsqlSafeReadException(new NotSupportedException("Only writing ArraySegment<char> to PostgreSQL text is supported, no reading."));
        }

        ValueTask<byte[]> INpgsqlTypeHandler<byte[]>.Read(NpgsqlReadBuffer buf, int byteLen, bool async, FieldDescription? fieldDescription)
        {
            var bytes = new byte[byteLen];
            if (buf.ReadBytesLeft >= byteLen)
            {
                buf.ReadBytes(bytes, 0, byteLen);
                return new ValueTask<byte[]>(bytes);
            }
            return ReadLong();

            async ValueTask<byte[]> ReadLong()
            {
                if (byteLen <= buf.Size)
                {
                    // The bytes can fit in our read buffer, read it.
                    while (buf.ReadBytesLeft < byteLen)
                        await buf.ReadMore(async);
                    buf.ReadBytes(bytes, 0, byteLen);
                    return bytes;
                }

                // Bad case: the bytes don't fit in our buffer.
                // This is rare - will only happen in CommandBehavior.Sequential mode (otherwise the
                // entire row is in memory). Tweaking the buffer length via the connection string can
                // help avoid this.

                var pos = 0;
                while (true)
                {
                    var len = Math.Min(buf.ReadBytesLeft, byteLen - pos);
                    buf.ReadBytes(bytes, pos, len);
                    pos += len;
                    if (pos < byteLen)
                    {
                        await buf.ReadMore(async);
                        continue;
                    }
                    break;
                }
                return bytes;
            }
        }

        #endregion

        #region Write

        /// <inheritdoc />
        public override unsafe int ValidateAndGetLength(string value, ref NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter)
        {
            if (lengthCache == null)
                lengthCache = new NpgsqlLengthCache(1);
            if (lengthCache.IsPopulated)
                return lengthCache.Get();

            if (parameter == null || parameter.Size <= 0 || parameter.Size >= value.Length)
                return lengthCache.Set(_encoding.GetByteCount(value));
            fixed (char* p = value)
                return lengthCache.Set(_encoding.GetByteCount(p, parameter.Size));
        }

        /// <inheritdoc />
        public virtual int ValidateAndGetLength(char[] value, ref NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter)
        {
            if (lengthCache == null)
                lengthCache = new NpgsqlLengthCache(1);
            if (lengthCache.IsPopulated)
                return lengthCache.Get();

            return lengthCache.Set(
                parameter == null || parameter.Size <= 0 || parameter.Size >= value.Length
                    ? _encoding.GetByteCount(value)
                    : _encoding.GetByteCount(value, 0, parameter.Size)
            );
        }

        /// <inheritdoc />
        public virtual int ValidateAndGetLength(ArraySegment<char> value, ref NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter)
        {
            if (lengthCache == null)
                lengthCache = new NpgsqlLengthCache(1);
            if (lengthCache.IsPopulated)
                return lengthCache.Get();

            if (parameter?.Size > 0)
                throw new ArgumentException($"Parameter {parameter.ParameterName} is of type ArraySegment<char> and should not have its Size set", parameter.ParameterName);

            return lengthCache.Set(value.Array is null ? 0 : _encoding.GetByteCount(value.Array, value.Offset, value.Count));
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(char value, ref NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter)
        {
            _singleCharArray[0] = value;
            return _encoding.GetByteCount(_singleCharArray);
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(byte[] value, ref NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter)
            => value.Length;

        /// <inheritdoc />
        public override Task Write(string value, NpgsqlWriteBuffer buf, NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter, bool async)
            => WriteString(value, buf, lengthCache!, parameter, async);

        /// <inheritdoc />
        public virtual Task Write(char[] value, NpgsqlWriteBuffer buf, NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter, bool async)
        {
            var charLen = parameter == null || parameter.Size <= 0 || parameter.Size >= value.Length
                ? value.Length
                : parameter.Size;
            return buf.WriteChars(value, 0, charLen, lengthCache!.GetLast(), async);
        }

        /// <inheritdoc />
        public virtual Task Write(ArraySegment<char> value, NpgsqlWriteBuffer buf, NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter, bool async)
            => value.Array is null ? Task.CompletedTask : buf.WriteChars(value.Array, value.Offset, value.Count, lengthCache!.GetLast(), async);

        Task WriteString(string str, NpgsqlWriteBuffer buf, NpgsqlLengthCache lengthCache, NpgsqlParameter? parameter, bool async)
        {
            var charLen = parameter == null || parameter.Size <= 0 || parameter.Size >= str.Length
                ? str.Length
                : parameter.Size;
            return buf.WriteString(str, charLen, lengthCache!.GetLast(), async);
        }

        /// <inheritdoc />
        public Task Write(char value, NpgsqlWriteBuffer buf, NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter, bool async)
        {
            _singleCharArray[0] = value;
            var len = _encoding.GetByteCount(_singleCharArray);
            return buf.WriteChars(_singleCharArray, 0, 1, len, async);
        }

        /// <inheritdoc />
        public Task Write(byte[] value, NpgsqlWriteBuffer buf, NpgsqlLengthCache? lengthCache, NpgsqlParameter? parameter, bool async)
            => buf.WriteBytesRaw(value, async);

        #endregion

        /// <inheritdoc />
        public virtual TextReader GetTextReader(Stream stream) => new StreamReader(stream);
    }*/
}
