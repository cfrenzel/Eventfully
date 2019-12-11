using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    /// <summary>
    /// Provide a base64 encoded key for encryption
    /// </summary>
    public interface IEncryptionKeyProvider
    {
        /// <summary>
        /// Get an encryption key
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns>A base64 encoded key</returns>
        string GetKey(string keyName = null);
    }

    public class StringKeyProvider : IEncryptionKeyProvider
    {
        private readonly string _key;
        private readonly bool _isBase64Encoded;
        public StringKeyProvider(string key, bool isBase64Encoded = false)
        {
            _key = key;
            _isBase64Encoded = isBase64Encoded;
        }

        public string GetKey(string keyName = null)
        {
            if (!_isBase64Encoded)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(_key);
                return System.Convert.ToBase64String(bytes);
            }
            return _key;
        }
    }

    
}
