namespace SqlStreamStore
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using SqlStreamStore.Imports.Ensure.That;

    internal struct StreamIdInfo // Love this name
    {
        public readonly SqlStreamId SqlStreamId;

        public readonly SqlStreamId MetadataSqlStreamId;

        public StreamIdInfo(string idOriginal)
        {
            Ensure.That(idOriginal, "streamId").IsNotNullOrWhiteSpace();

            var id = GetIdFromOriginal(idOriginal);

            SqlStreamId = new SqlStreamId(id, idOriginal);
            MetadataSqlStreamId = new SqlStreamId("$$" + id, "$$" + idOriginal);
        }

        private static string GetIdFromOriginal(string idOriginal)
        {
            if(Guid.TryParse(idOriginal, out var _))
            {
                return idOriginal; //If the ID is a GUID, don't bother hashing it.
            }
            using(var sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(idOriginal));
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }
    }

    internal struct SqlStreamId
    {
        internal readonly string Id;
        internal readonly string IdOriginal;

        public SqlStreamId(string id, string idOriginal)
        {
            Id = id;
            IdOriginal = idOriginal;
        }

        internal static readonly SqlStreamId Deleted = new StreamIdInfo(Streams.Deleted.DeletedStreamId).SqlStreamId;
    }
}