// Ignore Spelling: Multicast Mellanox Rpf Ip

namespace Mellanox
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	internal class MellanoxMulticastRoute
	{
		private readonly List<string> _outgoingIfs = new List<string>();

		private MellanoxMulticastRoute(object[] row)
		{
			Key = Convert.ToString(row[0]);
			SourceIp = Convert.ToString(row[1]);
			MulticastGroup = Convert.ToString(row[2]);
			RpfNeighbor = Convert.ToString(row[3]);
			InterfaceIn = Convert.ToString(row[4]);
		}

		public string Key { get; private set; }

		public string SourceIp { get; private set; }

		public string MulticastGroup { get; private set; }

		public string RpfNeighbor { get; private set; }

		public string InterfaceIn { get; private set; }

		public IEnumerable<string> OutgoingInterfaces => _outgoingIfs;

		public static IEnumerable<MellanoxMulticastRoute> GetFromTable(IDmsElement element)
		{
			Dictionary<string, MellanoxMulticastRoute> routes = new Dictionary<string, MellanoxMulticastRoute>();
			var table = element.GetTable(11000);
			var rows = table.GetData();
			foreach (var row in rows.Values)
			{
				var request = new MellanoxMulticastRoute(row);
				routes[request.Key] = request;
			}

			// Find outgoing IFs for multicasts
			var outgoing = MellanoxMuliticastOutgoing.GetFromTable(element);
			foreach (var outgoingIf in outgoing.Where(o => routes.ContainsKey(o.Route)))
			{
				routes[outgoingIf.Route]._outgoingIfs.Add(outgoingIf.OutgoingIf);
			}

			return routes.Values;
		}
	}
}