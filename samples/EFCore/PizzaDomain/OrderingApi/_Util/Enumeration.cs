

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace OrderingApi
{

    [DebuggerDisplay("{DisplayName} - {Value}")]
    public abstract class Enumeration<TEnumeration, TValue> : IComparable<TEnumeration>, IEquatable<TEnumeration>
        where TEnumeration : Enumeration<TEnumeration, TValue>
        where TValue : IComparable
    {
        private static readonly Lazy<TEnumeration[]> Enumerations = new Lazy<TEnumeration[]>(GetEnumerations);
        readonly string _displayName;
        readonly TValue _value;

        protected Enumeration(TValue value, string displayName)
        {
            if (value == null)
                throw new ArgumentNullException();
            
            _value = value;
            _displayName = displayName;
        }

        public TValue Value
        {
            get { return _value; }
            set { ; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }
        
        public int CompareTo(TEnumeration other) => Value.CompareTo(other == default(TEnumeration) ? default(TValue) : other.Value);
        public sealed override string ToString() => DisplayName;
        public static TEnumeration[] GetAll() =>  Enumerations.Value;
        
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

        public override bool Equals(object obj) => Equals(obj as TEnumeration);
        public bool Equals(TEnumeration other) => other != null && ValueEquals(other.Value);
        public override int GetHashCode()=> Value.GetHashCode();
        public static bool operator ==(Enumeration<TEnumeration, TValue> left, Enumeration<TEnumeration, TValue> right) => Equals(left, right);
        public static bool operator !=(Enumeration<TEnumeration, TValue> left, Enumeration<TEnumeration, TValue> right) => !Equals(left, right);
     
        public static TEnumeration FromValue(TValue value) => Parse(value, "value", item => item.Value.Equals(value));
        public static TEnumeration Parse(string displayName) => Parse(displayName, "display name", item => item.DisplayName == displayName);
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

        public static bool TryParse(TValue value, out TEnumeration result) => TryParse(e => e.ValueEquals(value), out result);
        public static bool TryParseValue(TValue value, out TEnumeration result) => TryParse(e => e.ValueEquals(value), out result);
        public static bool TryParse(string displayName, out TEnumeration result) => TryParse(e => e.DisplayName == displayName, out result);
        protected virtual bool ValueEquals(TValue value) => Value.Equals(value);
    }
}
