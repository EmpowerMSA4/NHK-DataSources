// Ignore Spelling: Lldp Mellanox

namespace Mellanox
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	internal class MellanoxLldpConnection
	{
		private MellanoxLldpConnection(string localIf, string remoteIf, string remoteName)
		{
			LocalIf = localIf;
			RemoteIf = remoteIf;
			RemoteName = remoteName;
		}

		public string LocalIf { get; private set; }

		public string RemoteIf { get; private set; }

		public string RemoteName { get; private set; }

		public static Dictionary<string, MellanoxLldpConnection> GetConnections(IDmsElement mellanox)
		{
			// Get local IF info
			Dictionary<string, string> localMapping = new Dictionary<string, string>();
			var localTable = mellanox.GetTable(10100);
			var localRows = localTable.GetData();
			foreach (var row in localRows.Values)
			{
				string key = Convert.ToString(row[0]);
				string localIf = Convert.ToString(row[2]);
				if (!string.IsNullOrWhiteSpace(key))
				{
					localMapping[key] = localIf;
				}
			}

			// Get remote IF info
			var comparer = StringComparer.OrdinalIgnoreCase;
			Dictionary<string, MellanoxLldpConnection> connections = new Dictionary<string, MellanoxLldpConnection>(comparer);
			var remoteTable = mellanox.GetTable(10300);
			var remoteRows = remoteTable.GetData();
			foreach (var row in remoteRows.Values)
			{
				var keyParts = Convert.ToString(row[0]).Split('.');
				var remoteIf = Convert.ToString(row[4]);
				var remoteName = Convert.ToString(row[6]);
				if (keyParts.Length < 2)
				{
					continue;
				}

				var localIfKey = keyParts[1];
				if (localMapping.ContainsKey(localIfKey))
				{
					var localIf = localMapping[localIfKey];
					var connection = new MellanoxLldpConnection(localIf, remoteIf, remoteName);
					connections[localIf] = connection;
				}
			}

			return connections;
		}
	}
}