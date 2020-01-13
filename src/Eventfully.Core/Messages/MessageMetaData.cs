using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eventfully
{
    public interface IMessageMetaData : IEnumerable<KeyValuePair<string, string>>
    {
        MessageMetaData With(string key, string value);
    }

    public class MessageMetaData : Dictionary<string, string>, IMessageMetaData
    {

        [JsonIgnore]
        public string MessageType
        {
            get
            {
                if (this.ContainsKey(HeaderType.MessageType.Value))
                    return this[HeaderType.MessageType.Value];
                return null;
            }
            set { this[HeaderType.MessageType.Value] = value; }
        }


        
        [JsonIgnore]
        public DateTime? CreatedAtUtc
        {
            get
            {
                if (this.ContainsKey(HeaderType.CreatedAtUtc.Value))
                    return GetDateTimeUtc(HeaderType.CreatedAtUtc.Value);
                return null;
            }
            set
            {
                this[HeaderType.CreatedAtUtc.Value] = value.HasValue ? value.Value.ToString("o") : null;
            }
        }

        [JsonIgnore]
        public string ContentType
        {
            get
            {
                if (this.ContainsKey(HeaderType.ContentType.Value))
                    return this[HeaderType.ContentType.Value];
                return null;
            }
            set { this[HeaderType.ContentType.Value] = value; }
        }

        [JsonIgnore]
        public string MessageId
        {
            get
            {
                if (this.ContainsKey(HeaderType.MessageId.Value))
                    return this[HeaderType.MessageId.Value];
                return null;
            }
            set { this[HeaderType.MessageId.Value] = value; }
        }

        [JsonIgnore]
        public string CorrelationId
        {
            get
            {
                if (this.ContainsKey(HeaderType.CorrelationId.Value))
                    return this[HeaderType.CorrelationId.Value];
                return null;
            }
            set { this[HeaderType.CorrelationId.Value] = value; }
        }

        [JsonIgnore]
        public string SessionId
        {
            get
            {
                if (this.ContainsKey(HeaderType.SessionId.Value))
                    return this[HeaderType.SessionId.Value];
                return null;
            }
            set { this[HeaderType.SessionId.Value] = value; }
        }

        [JsonIgnore]
        public string ReplyTo
        {
            get
            {
                if (this.ContainsKey(HeaderType.ReplyTo.Value))
                    return this[HeaderType.ReplyTo.Value];
                return null;
            }
            set { this[HeaderType.ReplyTo.Value] = value; }
        }


        [JsonIgnore]
        public string ReplyToEndpointName
        {
            get
            {
                if (this.ContainsKey(HeaderType.ReplyToEndpointName.Value))
                    return this[HeaderType.ReplyToEndpointName.Value];
                return null;
            }
            set { this[HeaderType.ReplyToEndpointName.Value] = value; }
        }

        [JsonIgnore]
        public bool SkipTransientDispatch
        {
            get
            {
                if (this.ContainsKey(HeaderType.SkipTransientDispatch.Value))
                    return this[HeaderType.SkipTransientDispatch.Value] == "True" ? true : false;
                return false;
            }
            set { this[HeaderType.SkipTransientDispatch.Value] = value ? "True" : "False"; }
        }

        [JsonIgnore]
        public DateTime? ExpiresAtUtc
        {
            get
            {
                if (this.ContainsKey(HeaderType.ExpiresAtUtc.Value) && this[HeaderType.ExpiresAtUtc.Value] != null)
                    return GetDateTimeUtc(HeaderType.ExpiresAtUtc.Value);
                    //return DateTime.Parse(this[HeaderType.ExpiresAtUtc.Value]).ToUniversalTime();
                return null;
            }
            set {
                this[HeaderType.ExpiresAtUtc.Value] = value.HasValue ? value.Value.ToString("o") : null;
                if (value.HasValue && ! TimeToLive.HasValue)
                    this.TimeToLive = value.Value - DateTime.UtcNow;
            }
        }

        [JsonIgnore]
        public TimeSpan? TimeToLive
        {
            get
            {
                if (this.ContainsKey(HeaderType.TimeToLive.Value) && this[HeaderType.TimeToLive.Value] != null)
                    return TimeSpan.Parse(this[HeaderType.TimeToLive.Value]);
                return null;
            }
            set {
                this[HeaderType.TimeToLive.Value] = value.HasValue ? value.ToString() : null;
                if (value.HasValue && !ExpiresAtUtc.HasValue)
                    ExpiresAtUtc = DateTime.UtcNow.Add(value.Value);
                
            }
        }

        [JsonIgnore]
        public TimeSpan? DispatchDelay
        {
            get
            {
                if (this.ContainsKey(HeaderType.DispatchDelay.Value) && this[HeaderType.DispatchDelay.Value] != null)
                    return TimeSpan.Parse(this[HeaderType.DispatchDelay.Value]);
                return null;
            }
            set
            {
                this[HeaderType.DispatchDelay.Value] = value.HasValue ? value.ToString() : null;
            }
        }

        [JsonIgnore]
        public bool Encrypted
        {
            get
            {
                if (this.ContainsKey(HeaderType.Encrypted.Value))
                    return this[HeaderType.Encrypted.Value] == "true" ? true : false;
                return false;
            }
            set { this[HeaderType.Encrypted.Value] = value ? "true" : "false"; }
        }

        [JsonIgnore]
        public string EncryptionMethod
        {
            get
            {
                if (this.ContainsKey(HeaderType.EncryptionMethod.Value))
                    return this[HeaderType.EncryptionMethod.Value];
                return null;
            }
            set { this[HeaderType.EncryptionMethod.Value] = value; }
        }

        [JsonIgnore]
        public string EncryptionKeyName
        {
            get
            {
                if (this.ContainsKey(HeaderType.EncryptionKeyName.Value))
                    return this[HeaderType.EncryptionKeyName.Value];
                return null;
            }
            set { this[HeaderType.EncryptionKeyName.Value] = value; }
        }

        public MessageMetaData(TimeSpan? delay = null, string correlationId = null, string messageId = null, bool skipTransient = false, DateTime? expiresAtUtc = null) : this()
        {
            this.DispatchDelay = delay;
            this.SkipTransientDispatch = skipTransient;
            this.CorrelationId = correlationId;
            this.MessageId = messageId;
            this.ExpiresAtUtc = expiresAtUtc;
        }

        public MessageMetaData() : this(DateTime.UtcNow){ }
        public MessageMetaData(DateTime createdAtUtc)
        {
            this.CreatedAtUtc = createdAtUtc;
        }

        public MessageMetaData With(string key, string value)
        {
            this.Add(key, key);
            return this;
        }

        public bool IsExpired(DateTime? utcNow = null)
        {
            utcNow = utcNow ?? DateTime.UtcNow;
            if (this.ExpiresAtUtc.HasValue && this.ExpiresAtUtc.Value <= utcNow.Value)
                return true;
            return false;
        }

       

        public void PopulateForReplyTo(MessageMetaData commandMetaData)
        {
            if (commandMetaData == null)
                return;

            if(this.SessionId == null)
                this.SessionId = commandMetaData.SessionId;
            if (this.CorrelationId == null)
                this.CorrelationId = commandMetaData.CorrelationId;
        }

        public IDictionary<string, string> GetHeadersByClass(HeaderClass hclass)
        {
            var types = HeaderType.GetAll().Where(x => x.Class == hclass).Select(x => x.Value).ToArray();
            if (types == null || types.Length < 1)
                return null;

            return this.Where(x => types.Contains(x.Key)).ToDictionary(x => x.Key, y => y.Value);
        }

        public string Add(string key, byte[] value)
        {
            string val =  Convert.ToBase64String(value);
            this[key] = val;
            return val;
        }
        public byte[] GetBytes(string key)
        {
            return Convert.FromBase64String(this[key]);
        }

        public string Add(string key, DateTime? value)
        {
            if (value.HasValue)
            {
                string val = value.Value.ToString("o");
                this[key] = val;
                return val;
            }
            this[key] = null;
            return null;
        }

        public DateTime GetDateTimeUtc(string key)
        {
            return DateTime.Parse(this[key]).ToUniversalTime();
        }



    }

    public class HeaderType : Enumeration<HeaderType, string>
    {
        public static readonly HeaderType MessageType = new HeaderType("MessageType", HeaderClass.Transport);
        public static readonly HeaderType MessageId = new HeaderType("MessageId", HeaderClass.Transport);
        public static readonly HeaderType CorrelationId = new HeaderType("CorrelationId", HeaderClass.Transport);
        public static readonly HeaderType ReplyTo = new HeaderType("ReplyTo", HeaderClass.Transport);
        public static readonly HeaderType SessionId = new HeaderType("SessionId", HeaderClass.Transport);


        public static readonly HeaderType ExpiresAtUtc = new HeaderType("ExpiresAtUtc", HeaderClass.Transport);
        public static readonly HeaderType TimeToLive = new HeaderType("TimeToLive", HeaderClass.Transport);
        public static readonly HeaderType DispatchDelay = new HeaderType("DispatchDelay", HeaderClass.Transport);

        public static readonly HeaderType Encrypted = new HeaderType("Encrypted", HeaderClass.Transport);
        public static readonly HeaderType EncryptionMethod = new HeaderType("Encryption-Method", HeaderClass.Transport);
        public static readonly HeaderType EncryptionKeyName = new HeaderType("Encryption-KeyName", HeaderClass.Transport);

        public static readonly HeaderType ContentType = new HeaderType("ContentType", HeaderClass.Application);
        public static readonly HeaderType CreatedAtUtc = new HeaderType("CreatedAtUtc", HeaderClass.Internal);
        public static readonly HeaderType SkipTransientDispatch = new HeaderType("SkipTransientDispatch", HeaderClass.Internal);
        public static readonly HeaderType ReplyToEndpointName = new HeaderType("ReplyToEndpointName", HeaderClass.Internal);

        public override bool Equals(object obj)
        {
            if (obj is string)
                return this.Value.Equals((string)obj, StringComparison.OrdinalIgnoreCase);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Value.ToLower().GetHashCode(); 
        }

        public HeaderClass Class { get; }
        private HeaderType(string value, HeaderClass hclass) : base(value, value)
        {
            this.Class = hclass;
        }
    }

    public enum HeaderClass
    {
        Internal = 0,
        Transport = 1,
        Application = 2,
    }
}
