using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Merthsoft.DynamicConfig;

namespace Merthsoft.DynamicConfigTest {
	/// <summary>
	/// Example dynamic config reading.
	/// </summary>
	class Program {
		static void Main(string[] args) {
			// Read in the INI file as a dynamic type.
			dynamic config = Config.ReadIni("Config.ini", new ConfigOptions() { CaseSensitive = false, ReturnNullWhenNotFound = true });

			// Access the properties directly as if it were an object.
			Console.WriteLine("Accessing properties directly:");
			Console.WriteLine("Cats say {0}.", config.Cat.sound);
			Console.WriteLine("Dogs say {0}.", config.dog.Sound);
			Console.WriteLine("Cows say {0}.", config.cow.sound);
            Console.Write("Fish says {0}.", config.fish?.sound ?? "nothing");

            Console.WriteLine();
			
			// Because we're using ExpandoObjects, which implement IDictionary<string, object>,
			// we can also iterate over the collection. Each item in the config is another ExpandoObject
			// meaning we can iterate over that as well.
			Console.WriteLine("Iterating over properties:");
			foreach (var prop in ((IDictionary<string, object>)config)) {
				Console.WriteLine("A {0}:", prop.Key);
				foreach (var key in ((IDictionary<string, object>)prop.Value)) {
					Console.WriteLine("\t{0}={1}", key.Key, key.Value);
				}
			}

			Console.WriteLine();

			Console.WriteLine("Reading in config 2.");
			Config.ReloadIni(config, "Config2.ini");
			foreach (var prop in ((IDictionary<string, object>)config)) {
				Console.WriteLine("A {0}:", prop.Key);
				foreach (var key in ((IDictionary<string, object>)prop.Value)) {
					Console.WriteLine("\t{0}={1}", key.Key, key.Value);
				}
			}

			Console.WriteLine();

			Console.WriteLine("Writing to config 3.");
			Config.WriteIni(config, "Config3.ini");
            Console.WriteLine("Data written.");

            Console.ReadKey();
		}
	}
}
