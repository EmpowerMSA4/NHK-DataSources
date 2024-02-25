// Ignore Spelling: mellanox Bitrate

namespace Mellanox
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	internal class MellanoxInterface
	{
		private MellanoxInterface(object[] row)
		{
			Key = Convert.ToString(row[0]);
			Name = Convert.ToString(row[18]);
			Description = Convert.ToString(row[12]);
			Type = Convert.ToString(row[12]);
			OperationalStatus = Convert.ToString(row[15]);
			CustomDescription = Convert.ToString(row[17]);
			Bitrate = Convert.ToDouble(row[22]);
			Utilization = Convert.ToDouble(row[21]);
			ErrorsInput = Convert.ToDouble(row[8]);
			ErrorRateInput = Convert.ToDouble(row[10]);
			ErrorsOutput = Convert.ToDouble(row[9]);
			ErrorRateOutput = Convert.ToDouble(row[11]);
		}

		public string Key { get; private set; }

		public string Name { get; private set; }

		public string Description { get; private set; }

		public string OperationalStatus { get; private set; }

		public string AdministrativeStatus { get; private set; }

		public string CustomDescription { get; private set; }

		/// <summary>
		/// Gets the bitrate used by the interface in Mbps.
		/// </summary>
		public double Bitrate { get; private set; }

		public double Utilization { get; private set; }

		/// <summary>
		/// Gets the type of the interface (eth, lag, vlan or loopback).
		/// </summary>
		public string Type { get; private set; }

		/// <summary>
		/// Gets the number of inbound packets that contained errors delivering to a higher-layer protocol.
		/// </summary>
		public double ErrorsInput { get; private set; }

		/// <summary>
		/// Gets the rate of error packets per second.
		/// </summary>
		public double ErrorRateInput { get; private set; }

		/// <summary>
		/// Gets the number of outbound packets that could not be transmitted.
		/// </summary>
		public double ErrorsOutput { get; private set; }

		/// <summary>
		/// Gets the rate of error packets per second.
		/// </summary>
		public double ErrorRateOutput { get; private set; }

		public static Dictionary<string, MellanoxInterface> GetFromTable(IDmsElement mellanox)
		{
			// Get local IF info
			var comparer = StringComparer.OrdinalIgnoreCase;
			Dictionary<string, MellanoxInterface> interfaces = new Dictionary<string, MellanoxInterface>(comparer);
			var localTable = mellanox.GetTable(1100);
			var localRows = localTable.GetData();
			foreach (var row in localRows.Values)
			{
				var newInterface = new MellanoxInterface(row);
				interfaces[newInterface.Name] = newInterface;
			}

			return interfaces;
		}
	}
}