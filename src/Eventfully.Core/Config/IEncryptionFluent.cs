using System;
using System.Collections.Generic;
using System.Text;
using Eventfully.Filters;
using Eventfully.Transports;

namespace Eventfully
{

    public interface ISupportFiltersFluent<T>
    {
        T WithFilter(params ITransportMessageFilter[] filters);
        T WithFilter(params IMessageFilter[] filters);
    }


    public static class ConfigureEncryptionFluentExtensions
    {
        public static T UseAesEncryption<T>(this T config, string key, bool isBase64Encoded = false)  where T: ISupportFiltersFluent<T>
        {
            if (String.IsNullOrEmpty(key))
                throw new InvalidOperationException("UseEncryption requires a non empty value for key");

            return UseAesEncryption(config,null, new StringKeyProvider(key, isBase64Encoded));
        }

        public static T UseAesEncryption<T>(this T config, IEncryptionKeyProvider keyProvider)  where T: ISupportFiltersFluent<T>
        {
            return UseAesEncryption(config,null, keyProvider);
        }

        public static  T UseAesEncryption<T>(this T config, string keyName, IEncryptionKeyProvider keyProvider) where T: ISupportFiltersFluent<T>
        {
            throw new ApplicationException();
            // config.WithFilter(new MessageTypeTransportFilter<T>(
            //     new AesEncryptionTransportFilter(keyName, keyProvider)
            // ));
            // return config;
        }
    }

}
