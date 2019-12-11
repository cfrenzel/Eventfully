using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Eventfully
{
    [DebuggerDisplay("{DisplayName} - {Value}")]
    public abstract class Enumeration<TEnumeration> : Enumeration<TEnumeration, int>
        where TEnumeration : Enumeration<TEnumeration>
    {
        protected Enumeration(int value, string displayName)
            : base(value, displayName)
        {
        }

        public static TEnumeration FromInt32(int value)
        {
            return FromValue(value);
        }

        public static bool TryFromInt32(int listItemValue, out TEnumeration result)
        {
            return TryParse(listItemValue, out result);
        }
    }

    [DebuggerDisplay("{DisplayName} - {Value}")]
    //[DataContract(Namespace = "http://github.com/HeadspringLabs/Enumeration/5/13")]
    public abstract class Enumeration<TEnumeration, TValue> : IComparable<TEnumeration>, IEquatable<TEnumeration>
        where TEnumeration : Enumeration<TEnumeration, TValue>
        where TValue : IComparable
    {
        private static readonly Lazy<TEnumeration[]> Enumerations = new Lazy<TEnumeration[]>(GetEnumerations);

        //[DataMember(Order = 1)]
        readonly string _displayName;

        //[DataMember(Order = 0)]
        readonly TValue _value;

        protected Enumeration(TValue value, string displayName)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            _value = value;
            _displayName = displayName;
        }

        public TValue Value
        {
            get { return _value; }
            set {; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

       
        public int CompareTo(TEnumeration other)
        {
            return Value.CompareTo(other == default(TEnumeration) ? default(TValue) : other.Value);
        }

        public override sealed string ToString()
        {
            return DisplayName;
        }

        public static TEnumeration[] GetAll()
        {
            return Enumerations.Value;
        }

        public static IEnumerable<KeyValuePair<TValue, string>> ToKeyValuePairs(Func<TEnumeration, string> formatter = null) //where T : Enumeration<T, int>
        {
            return Enumerations.Value.Select(x =>
            {
                return new KeyValuePair<TValue, string>(x.Value, formatter != null ? formatter(x) : x.DisplayName);
            });

        }
        //public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs<T>(this Enumeration<T, string> e, Func<Enumeration<T, string>, string> formatter = null) where T : Enumeration<T, string>
        //{
        //    return Enumeration<T, string>.GetAll().Select(x =>
        //    {
        //        return new KeyValuePair<string, string>(x.Value, formatter != null ? formatter(x) : x.DisplayName);
        //    });
        //}
        //public static IEnumerable<KeyValuePair<int, string>> ToKeyValuePairs<T>(this Enumeration<T, int> e, Func<Enumeration<T, int>, string> formatter = null) where T : Enumeration<T, int>
        //{
        //    return Enumeration<T, int>.GetAll().Select(x =>
        //    {
        //        return new KeyValuePair<int, string>(x.Value, formatter != null ? formatter(x) : x.DisplayName);
        //    });

        //}
        //public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs<T>(this Enumeration<T, string> e, Func<Enumeration<T, string>, string> formatter = null) where T : Enumeration<T, string>
        //{
        //    return Enumeration<T, string>.GetAll().Select(x =>
        //    {
        //        return new KeyValuePair<string, string>(x.Value, formatter != null ? formatter(x) : x.DisplayName);
        //    });
        //}


        private static TEnumeration[] GetEnumerations()
        {
            Type enumerationType = typeof(TEnumeration);
            return enumerationType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(info => enumerationType.IsAssignableFrom(info.FieldType))
                .Select(info => info.GetValue(null))
                .Cast<TEnumeration>()
                .ToArray();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TEnumeration);
        }

        public bool Equals(TEnumeration other)
        {
            return other != null && ValueEquals(other.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(Enumeration<TEnumeration, TValue> left, Enumeration<TEnumeration, TValue> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Enumeration<TEnumeration, TValue> left, Enumeration<TEnumeration, TValue> right)
        {
            return !Equals(left, right);
        }

        public static TEnumeration FromValue(TValue value)
        {
            return Parse(value, "value", item => item.Value.Equals(value));
        }

        public static TEnumeration Parse(string displayName)
        {
            return Parse(displayName, "display name", item => item.DisplayName == displayName);
        }

        static bool TryParse(Func<TEnumeration, bool> predicate, out TEnumeration result)
        {
            result = GetAll().FirstOrDefault(predicate);
            return result != null;
        }

        private static TEnumeration Parse(object value, string description, Func<TEnumeration, bool> predicate)
        {
            TEnumeration result;

            if (!TryParse(predicate, out result))
            {
                string message = string.Format("'{0}' is not a valid {1} in {2}", value, description, typeof(TEnumeration));
                throw new ArgumentException(message, "value");
            }

            return result;
        }

        public static bool TryParse(TValue value, out TEnumeration result)
        {
            return TryParse(e => e.ValueEquals(value), out result);
        }

        public static bool TryParseValue(TValue value, out TEnumeration result)
        {
            return TryParse(e => e.ValueEquals(value), out result);
        }

        public static bool TryParse(string displayName, out TEnumeration result)
        {
            return TryParse(e => e.DisplayName == displayName, out result);
        }

        protected virtual bool ValueEquals(TValue value)
        {
            return Value.Equals(value);
        }
    }
}
