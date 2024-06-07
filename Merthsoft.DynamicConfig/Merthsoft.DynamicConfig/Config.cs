using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merthsoft.DynamicConfig {

	/// <summary>
	/// Contains the static config methods.
	/// </summary>
	public static class Config {
        /// <summary>
        /// Reads an INI file from a stream.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="options">The ConfigOptions that define how this configuration will work.</param>
        /// <returns>A dynamic object with properties defined by the INI file.</returns>
        public static dynamic ReadIni(StreamReader reader, ConfigOptions options) {
			bool lineIsSection = false;
			string sectionName;
			int lineNumber = -1;

            IDictionary<string, object> config = new ConfigObject(options);
			IDictionary<string, object> currentSection = null;

			while (!reader.EndOfStream) {
				string line = reader.ReadLine().TrimStart();
				lineNumber++;
				if (string.IsNullOrWhiteSpace(line)) {
					continue;
				}

				// Might be nice to somehow preserve comments at some point in time...
				if (line[0] == ';' || line[0] == '#') {
					continue;
				}

				lineIsSection = line[0] == '[' && line[line.Length - 1] == ']';

				if (lineIsSection) {
					sectionName = line.Substring(1, line.Length - 2);
					currentSection = new ConfigObject(options);
					config.Add(sectionName, currentSection);
				} else {
					if (currentSection == null) { throw new IniException("Current section is null, but trying to read a key.", lineNumber); }

					int equalsLocation = line.IndexOf('=');
					if (equalsLocation == -1) { equalsLocation = line.IndexOf(':'); }
					if (equalsLocation <= 0 || equalsLocation == line.Length) { throw new IniException("Key-value pair not in format 'key=value' or 'key:value'.", lineNumber); }

					string key = line.Substring(0, equalsLocation).Trim();
					string value = line.Substring(equalsLocation + 1).Trim();
					if (value.Length > 0 && value[0] == '"' && value[value.Length - 1] == '"') { value = value.Substring(1, value.Length - 2); }

					currentSection.Add(key, value);
				}
			}

			return config;
		}

        /// <summary>
        /// Reads an INI file from a stream.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <returns>A dynamic object with properties defined by the INI file.</returns>
        public static dynamic ReadIni(StreamReader reader) {
            return ReadIni(reader, ConfigOptions.Default);
        }
        /// <summary>
        /// Reads an INI file from disk.
        /// </summary>
        /// <param name="path">The location of the INI file.</param>
        /// <param name="options">The ConfigOptions that define how this configuration will work.</param>
        /// <returns>A dynamic object with properties defined by the INI file.</returns>
        public static dynamic ReadIni(string path, ConfigOptions options) {
			using (StreamReader sr = new StreamReader(path)) {
				return ReadIni(sr, options);
			}
		}

        /// <summary>
		/// Reads an INI file from disk.
		/// </summary>
		/// <param name="path">The location of the INI file.</param>
		/// <returns>A dynamic object with properties defined by the INI file.</returns>
        public static dynamic ReadIni(string path) {
            return ReadIni(path, ConfigOptions.Default);
        }

        /// <summary>
        /// Writes an INI file to disk.
        /// </summary>
        /// <param name="config">The config data to write.</param>
        /// <param name="path">The path to write to.</param>
        public static void WriteIni(this IDictionary<string, object> config, string path) {
			using (StreamWriter sw = new StreamWriter(path)) {
				WriteIni(config, sw);
			}
		}

		/// <summary>
		/// Writes an INI file to a stream.
		/// </summary>
		/// <param name="config">The config data to write.</param>
		/// <param name="sw">The stream to write to.</param>
		private static void WriteIni(this IDictionary<string, object> config, StreamWriter sw) {
			foreach (var prop in config) {
				sw.WriteLine("[{0}]", prop.Key);
				foreach (var item in (IDictionary<string, object>)prop.Value) {
					sw.WriteLine("{0}={1}", item.Key, item.Value);
				}
				sw.WriteLine();
			}
		}

		/// <summary>
		/// Writes an INI file to disk.
		/// </summary>
		/// <param name="config">The config data to write.</param>
		/// <param name="path">The path to write to.</param>
		public static void WriteIni(dynamic config, string path) {
			((IDictionary<string, object>)config).WriteIni(path);
		}

		/// <summary>
		/// Writes an INI file to a stream.
		/// </summary>
		/// <param name="config">The config data to write.</param>
		/// <param name="sw">The stream to write to.</param>
		private static void WriteIni(dynamic config, StreamWriter sw) {
			((IDictionary<string, object>)config).WriteIni(sw);
		}

		/// <summary>
		/// Reloads an INI file into an object.
		/// </summary>
		/// <param name="config">The existing config object to load.</param>
		/// <param name="reader">The reader stream to read from.</param>
		public static void ReloadIni(this IDictionary<string, object> config, StreamReader reader) {
			IDictionary<string, object> newConfig = ReadIni(reader);

			pruneDictionary(config, newConfig);

			foreach (var prop in newConfig) {
				if (config.ContainsKey(prop.Key)) {
					pruneDictionary((IDictionary<string, object>)config[prop.Key], (IDictionary<string, object>)prop.Value);
					foreach (var item in (IDictionary<string, object>)prop.Value) {
						((IDictionary<string, object>)config[prop.Key])[item.Key] = item.Value;
					}
				} else {
					config.Add(prop);
				}
			}
		}

		/// <summary>
		/// Reloads an INI file into an object.
		/// </summary>
		/// <param name="config">The existing config object to load.</param>
		/// <param name="path">The location of the INI file.</param>
		public static void ReloadIni(this IDictionary<string, object> config, string path) {
			using (StreamReader sr = new StreamReader(path)) {
				ReloadIni(config, sr);
			}
		}

		/// <summary>
		/// Reloads an INI file into an object.
		/// </summary>
		/// <param name="config">The existing config object to load.</param>
		/// <param name="path">The location of the INI file.</param>
		public static void ReloadIni(dynamic config, string path) {
			((IDictionary<string, object>)config).ReloadIni(path);
		}
		
		/// <summary>
		/// Reloads an INI file into an object.
		/// </summary>
		/// <param name="config">The existing config object to load.</param>
		/// <param name="reader">The reader stream to read from.</param>
		public static void ReloadIni(dynamic config, StreamReader reader) {
			((IDictionary<string, object>)config).ReloadIni(reader);
		}

        /// <summary>
        /// Prunes non-existing values out of an existing dictionary.
        /// </summary>
        /// <param name="config">The dictionary to prune.</param>
        /// <param name="newConfig">The dictionary to match values from.</param>
        private static void pruneDictionary(IDictionary<string, object> config, IDictionary<string, object> newConfig) {
            ICollection<string> configKeys = config.Keys;
            int configKeysCount = configKeys.Count;
            var existingKeys = config.Where(k => newConfig.ContainsKey(k.Key)).Select(k => k.Key).ToList();
            foreach (var key in existingKeys) {
                config.Remove(key);
            }
        }
    }
}
