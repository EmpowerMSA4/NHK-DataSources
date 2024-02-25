// Ignore Spelling: Mellanox

namespace Mellanox
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	internal class MellanoxVlanDetails
	{
		private MellanoxVlanDetails(object[] row)
		{
			Key = Convert.ToString(row[0]);
			Name = Convert.ToString(row[1]);
			Interface = Convert.ToString(row[2]);
		}

		public string Key { get; private set; }

		public string Name { get; private set; }

		public string Interface { get; private set; }

		public static Dictionary<string, MellanoxVlanDetails> GetFromTable(IDmsElement element)
		{
			Dictionary<string, MellanoxVlanDetails> vlans = new Dictionary<string, MellanoxVlanDetails>();
			var table = element.GetTable(1570);
			var rows = table.GetData();
			foreach (var row in rows.Values)
			{
				var request = new MellanoxVlanDetails(row);
				vlans[request.Key] = request;
			}

			return vlans;
		}
	}
}