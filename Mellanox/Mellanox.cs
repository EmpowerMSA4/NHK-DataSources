// Ignore Spelling: Mellanox Multicast Ip Lldp

namespace Mellanox
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	internal class Mellanox
	{
		private readonly IDmsElement element;
		private IEnumerable<MellanoxMulticastRoute> _multicastRoutes;
		private string _ipAddress;
		private string _hostname;
		private Dictionary<string, MellanoxLldpConnection> _lldpConnectionsByLocalIf;
		private Dictionary<string, MellanoxVlanDetails> _vlanById;
		private Dictionary<string, MellanoxInterface> _interfacesByName;

		public Mellanox(IDmsElement element)
		{
			this.element = element;
		}

		public IDmsElement DmsElement => element;

		public IEnumerable<MellanoxMulticastRoute> MulticastRoutes
		{
			get
			{
				if (_multicastRoutes == null)
				{
					_multicastRoutes = MellanoxMulticastRoute.GetFromTable(DmsElement);
				}

				return _multicastRoutes;
			}
		}

		public Dictionary<string, MellanoxInterface> InterfacesByName
		{
			get
			{
				if (_interfacesByName == null)
				{
					_interfacesByName = MellanoxInterface.GetFromTable(DmsElement);
				}

				return _interfacesByName;
			}
		}

		public Dictionary<string, MellanoxLldpConnection> LldpConnectionsByLocalIf
		{
			get
			{
				if (_lldpConnectionsByLocalIf == null)
				{
					_lldpConnectionsByLocalIf = MellanoxLldpConnection.GetConnections(DmsElement);
				}

				return _lldpConnectionsByLocalIf;
			}
		}

		public Dictionary<string, MellanoxVlanDetails> VlanById
		{
			get
			{
				if (_vlanById == null)
				{
					_vlanById = MellanoxVlanDetails.GetFromTable(DmsElement);
				}

				return _vlanById;
			}
		}

		/// <summary>
		/// Get the port from the interface table. The lookup is not case-sensitive.
		/// </summary>
		/// <param name="interfaceName">The name of the interface.</param>
		/// <returns>The port from the interface table. If the entry is not found it will return <see langword="null"/>.</returns>
		public MellanoxInterface GetPortForInterface(string interfaceName)
		{
			string portName = interfaceName;
			if (interfaceName.StartsWith("vlan"))
			{
				string vlan = interfaceName.Substring(4);
				if (VlanById.ContainsKey(vlan))
				{
					portName = VlanById[vlan].Interface;
				}
			}

			if (!InterfacesByName.ContainsKey(portName))
			{
				return null;
			}

			return InterfacesByName[portName];
		}

		public string IpAddress
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_ipAddress))
				{
					_ipAddress = DmsElement.GetStandaloneParameter<string>(2).GetValue();
				}

				return _ipAddress;
			}
		}

		public string HostName
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_hostname))
				{
					_hostname = DmsElement.GetStandaloneParameter<string>(1413).GetValue();
				}

				return _hostname;
			}
		}

		public bool HasMultiCastGroup(string multicastGroup)
		{
			return MulticastRoutes.Any(m => m.MulticastGroup.Equals(multicastGroup));
		}
	}
}