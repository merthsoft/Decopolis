using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Merthsoft.DynamicConfig {
    /// <summary>
    /// A custom object for safe configuration access.
    /// </summary>
    class ConfigObject : DynamicObject, IDictionary<string, object> {
        /// <summary>
        /// Gets the ConfigOptions that define how access is handled.
        /// </summary>
        public ConfigOptions Options { get; }

        /// <summary>
        /// Creates a new ConfigObject with the default options.
        /// </summary>
        public ConfigObject() { Options = ConfigOptions.Default; }

        /// <summary>
        /// Creates a new ConfigObject with the specified options.
        /// </summary>
        /// <param name="options">The ConfigOptions that define access.</param>
        public ConfigObject(ConfigOptions options) { Options = options; }

        /// <summary>
        /// Backing store for data.
        /// </summary>
        readonly Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public object this[string key] {
            get {
                return dictionary[key];
            }

            set {
                dictionary[key] = value;
            }
        }

        public int Count {
            get {
                return dictionary.Count;
            }
        }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        public ICollection<string> Keys {
            get {
                return dictionary.Keys;
            }
        }

        public ICollection<object> Values {
            get {
                return dictionary.Values;
            }
        }

        public void Add(KeyValuePair<string, object> item) {
            Add(item.Key, item.Value);
        }

        public void Add(string key, object value) {
            dictionary.Add(key, value);
        }

        public void Clear() {
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item) {
            return dictionary.Contains(item);
        }

        public bool ContainsKey(string key) {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
            foreach (var item in dictionary) {
                array[arrayIndex++] = item;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            return dictionary.GetEnumerator();
        }

        public bool Remove(KeyValuePair<string, object> item) {
            if (dictionary.ContainsKey(item.Key) && dictionary[item.Key] == item.Value) {
                return Remove(item.Key);
            } else {
                return false;
            }
        }

        public bool Remove(string key) {
            return dictionary.Remove(key);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            string name = binder.Name;
            if (!Options.CaseSensitive) { name = name.ToLowerInvariant(); }

            foreach (var val in dictionary) {
                var checkedKey = val.Key;
                if (!Options.CaseSensitive) { checkedKey = checkedKey.ToLowerInvariant(); }
                if (checkedKey == name) {
                    result = val.Value;
                    return true;
                }
            }
            
            result = null;
            return Options.ReturnNullWhenNotFound;
            
        }

        public bool TryGetValue(string key, out object value) {
            if (ContainsKey(key)) {
                value = dictionary[key];
                return true;
            } else {
                value = null;
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return (dictionary as IEnumerable).GetEnumerator();
        }
    }
}
