// Ignore Spelling: Muliticast Mellanox

namespace Mellanox
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	internal class MellanoxMuliticastOutgoing
	{
		public MellanoxMuliticastOutgoing(object[] row)
		{
			Key = Convert.ToString(row[0]);
			Route = Convert.ToString(row[1]);
			OutgoingIf = Convert.ToString(row[2]);
		}

		public string Key { get; private set; }

		public string Route { get; private set; }

		public string OutgoingIf { get; private set; }

		public static IEnumerable<MellanoxMuliticastOutgoing> GetFromTable(IDmsElement element)
		{
			List<MellanoxMuliticastOutgoing> requests = new List<MellanoxMuliticastOutgoing>();
			var table = element.GetTable(11100);
			var rows = table.GetData();
			foreach (var row in rows.Values)
			{
				var request = new MellanoxMuliticastOutgoing(row);
				requests.Add(request);
			}

			return requests;
		}
	}
}