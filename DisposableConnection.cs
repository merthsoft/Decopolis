using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySqlConnector;

namespace Merthsoft.DecBot3 {
	// I'm totally cheating with this. You can dispose of it multiple times. Do something like
	// using (Connection.Open()) { ... }
	// Makes it so I don't have to wrap it in a try...finally just to close the connection.
	class DisposableConnection : IDisposable {
		public MySqlConnection Connection { get; private set; }

		public static implicit operator MySqlConnection(DisposableConnection conn) {
			return conn.Connection;
		}

		public DisposableConnection(MySqlConnection connection) {
			Connection = connection;
		}

		public DisposableConnection Open() {
			Connection.Open();
			return this;
		}

		public void Dispose() {
			Connection.Close();
		}
	}
}
