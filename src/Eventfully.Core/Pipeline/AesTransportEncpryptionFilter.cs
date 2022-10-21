using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Eventfully.Filters
{
    /// <summary>
    /// Encrypted / Decrypt TransportMessages - these are messages
    /// that are already in the format ready to go on the bus 
    /// </summary>
    public class AesEncryptionTransportFilter : ITransportMessageFilter
    {
        private readonly IEncryptionKeyProvider _keyProvider;
        private readonly string _keyName;

        public AesEncryptionTransportFilter(IEncryptionKeyProvider keyProvider):this(null, keyProvider)
        {
        }
        public AesEncryptionTransportFilter(string keyName, IEncryptionKeyProvider keyProvider)
        {
            _keyName = keyName;
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));

        }
        public TransportMessageFilterContext OnIncoming(TransportMessageFilterContext context)
        {
            return _decrypt(context);
        }

        public TransportMessageFilterContext OnOutgoing(TransportMessageFilterContext context)
        {
            return _encrypt(context);
        }
        // public TransportMessageFilterContext Process(TransportMessageFilterContext context)
        // {
        //     if (context.Direction == FilterDirection.Outbound)
        //         return _encrypt(context);
        //     else if (context.Direction == FilterDirection.Inbound)
        //         return _decrypt(context);
        //
        //     return context;
        // }

        private TransportMessageFilterContext _encrypt(TransportMessageFilterContext context)
        {
            using (Aes aes = Aes.Create())
            {
                var key = Convert.FromBase64String(_keyProvider.GetKey(_keyName));
                aes.GenerateIV();
                var aesIV = aes.IV;
                ICryptoTransform encryptor = aes.CreateEncryptor(key, aesIV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(context.TransportMessage.Data, 0, context.TransportMessage.Data.Length);
                    }
                    context.TransportMessage.Data = msEncrypt.ToArray();
                }

                var meta = context.TransportMessage.MetaData = context.TransportMessage.MetaData ?? new MessageMetaData();
                meta.Encrypted = true;
                meta.EncryptionKeyName = _keyName;
                meta.EncryptionMethod = "AES";
                meta.Add("EncryptionVector", aesIV);
                
                return context;
            }
        }

        private TransportMessageFilterContext _decrypt(TransportMessageFilterContext context)
        {
            var meta = context.TransportMessage.MetaData = context.TransportMessage.MetaData ?? new MessageMetaData();
            if (!meta.ContainsKey("EncryptionVector"))
                throw new ApplicationException($"Unable to decrypt message.  MetaData field EncryptionVector was null");
            var aesIV = meta.GetBytes("EncryptionVector");

            var metaKeyName = meta.EncryptionKeyName;
            var keyName = _keyName ?? metaKeyName;

            //if (string.IsNullOrEmpty(keyName))
            //    throw new ApplicationException($"Unable find AES keyName through configuration or metadata. AesEncryptionTransportFilter._keyName and  MetaData.EncryptionKeyName was null.  MessageType: {context.TransportMessage.MessageTypeIdentifier}");
            //if (!"AES".Equals(meta.EncryptionMethod, StringComparison.OrdinalIgnoreCase))
            //    throw new ApplicationException($"Unable to decrypt message.  Expected meta data EncryptionMethod=AES, but found {context.TransportMessage.MetaData.EncryptionMethod}");

            using (Aes aes = Aes.Create())
            {
                var key = Convert.FromBase64String(_keyProvider.GetKey(keyName));
                ICryptoTransform decryptor = aes.CreateDecryptor(key, aesIV);
                using (MemoryStream stream = new MemoryStream(context.TransportMessage.Data))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
                    {
                        context.TransportMessage.Data = ReadFully(csDecrypt);
                    }
                }
                return context;
            }
        }

        /// <summary>
        /// Taken from Jon Skeet
        /// https://jonskeet.uk/csharp/readbinary.html
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="initialLength"></param>
        /// <returns></returns>
        public static byte[] ReadFully(Stream stream, int initialLength = -1)
        {
            if (initialLength < 1)
                initialLength = 32768;
            
            byte[] buffer = new byte[initialLength];
            int read = 0;
            int chunk;

            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;
                // If we've reached the end of our buffer, check to see if there's any more information
                if (read == buffer.Length)
                {
                    int nextByte = stream.ReadByte();
                    if (nextByte == -1)
                        return buffer;

                    //Resize the buffer, put in the byte we've just read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);
            return ret;
        }
    }
}
