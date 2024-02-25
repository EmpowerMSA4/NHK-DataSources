/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

21/02/2024	1.0.0.1		MSA, Skyline	Initial version
****************************************************************************
*/

// Ignore Spelling: Gqi Multicast
namespace GetMulticastIfConnections_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Mellanox;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	[GQIMetaData(Name = "GetMulticastIfConnections")]
	public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
	{
		private readonly GQIStringArgument _multiCast = new GQIStringArgument("Multicast") { IsRequired = true };
		private readonly int limit = 30;
		private Dictionary<string, Mellanox> _switches;
		private int leftOff;
		private string multiCastGroup;
		private IDms thisDms;
		private List<GQIRow> retrievedConnections;

		private Dictionary<string, Mellanox> Switches
		{
			get
			{
				if (_switches == null)
				{
					RetrieveSwitches();
				}

				return _switches;
			}
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Source IF ID"),
				new GQIStringColumn("Destination IF ID"),
				new GQIStringColumn("Source IP"),
				new GQIStringColumn("Multicast Group"),
				new GQIStringColumn("RPF Neighbor"),
			};
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _multiCast };
		}

		public GQIPage GetNextPageOld(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();
			for (int i = leftOff; i < Switches.Count; i++)
			{
				var mellanox = Switches.ElementAt(i).Value;
				leftOff++;
				foreach (var multicastRoute in mellanox.MulticastRoutes.Where(m => multiCastGroup.Equals("All", StringComparison.InvariantCultureIgnoreCase) || m.MulticastGroup == multiCastGroup))
				{
					string parsedIfIn = multicastRoute.InterfaceIn;
					if (multicastRoute.InterfaceIn.StartsWith("vlan"))
					{
						string vlan = multicastRoute.InterfaceIn.Substring(4);
						if (mellanox.VlanById.ContainsKey(vlan))
						{
							parsedIfIn = mellanox.VlanById[vlan].Interface;
						}
					}

					foreach (var outgoingIf in multicastRoute.OutgoingInterfaces)
					{

						string parsedIfOut = outgoingIf;
						if (outgoingIf.StartsWith("vlan"))
						{
							string vlan = outgoingIf.Substring(4);
							if (mellanox.VlanById.ContainsKey(vlan))
							{
								parsedIfOut = mellanox.VlanById[vlan].Interface;
							}
						}

						// Internal connection
						var internalConnection = new GQIRow(
							new[]
							{
								new GQICell { Value = $"{mellanox.DmsElement.Name} {parsedIfIn}"},
								new GQICell { Value = $"{mellanox.DmsElement.Name} {parsedIfOut}"},
								new GQICell { Value = $"{multicastRoute.SourceIp}"},
								new GQICell { Value = $"{multicastRoute.MulticastGroup}"},
								new GQICell { Value = $"{multicastRoute.RpfNeighbor}"},
							});
						rows.Add(internalConnection);
						if (!mellanox.LldpConnectionsByLocalIf.ContainsKey(parsedIfOut))
						{
							continue;
						}

						var lldpConnection = mellanox.LldpConnectionsByLocalIf[parsedIfOut];
						if (!Switches.ContainsKey(lldpConnection.RemoteName))
						{
							continue;
						}

						var remoteSwitch = Switches[lldpConnection.RemoteName];
						var newRow = new GQIRow(
							new[]
							{
								new GQICell { Value = $"{mellanox.DmsElement.Name} {parsedIfOut}" },
								new GQICell { Value = $"{remoteSwitch.DmsElement.Name} {lldpConnection.RemoteIf}" },
								new GQICell { Value = $"{multicastRoute.SourceIp}"},
								new GQICell { Value = $"{multicastRoute.MulticastGroup}"},
								new GQICell { Value = $"{multicastRoute.RpfNeighbor}"},
							});
						rows.Add(newRow);
					}
				}

				if (rows.Count > limit)
				{
					break;
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = leftOff < Switches.Count,
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (retrievedConnections == null)
			{
				RetrieveConnections();
			}

			List<GQIRow> rows = new List<GQIRow>(limit);
			for (int i = leftOff; i < retrievedConnections.Count; i++)
			{
				var row = retrievedConnections[i];
				rows.Add(row);
				leftOff++;

				if (rows.Count >= limit)
				{
					break;
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = leftOff < retrievedConnections.Count,
			};
		}

		private void RetrieveConnections()
		{
			retrievedConnections = new List<GQIRow>();
			foreach (var mellanox in Switches.Values)
			{
				RetrieveConnectionsFromSwitch(mellanox);
			}
		}

		private void RetrieveConnectionsFromSwitch(Mellanox mellanox)
		{
			foreach (var multicastRoute in mellanox.MulticastRoutes.Where(m => multiCastGroup.Equals("All", StringComparison.InvariantCultureIgnoreCase) || m.MulticastGroup == multiCastGroup))
			{
				// Local IF In
				var portIn = mellanox.GetPortForInterface(multicastRoute.InterfaceIn);

				foreach (var outgoingIf in multicastRoute.OutgoingInterfaces)
				{
					// Local IF out
					var portOut = mellanox.GetPortForInterface(outgoingIf);
					if (portOut == null)
					{
						continue;
					}

					// Add internal connection
					if (portIn != null)
					{
						AddConnectionRow(mellanox, mellanox, portIn, portOut, multicastRoute);
					}

					// Add external connection.
					AddRemoteConnection(mellanox, portOut, multicastRoute);
				}
			}
		}

		private void AddRemoteConnection(Mellanox mellanox, MellanoxInterface localPortOut, MellanoxMulticastRoute routeInfo)
		{
			if (!mellanox.LldpConnectionsByLocalIf.ContainsKey(localPortOut.Name))
			{
				return;
			}

			var lldpConnection = mellanox.LldpConnectionsByLocalIf[localPortOut.Name];
			if (!Switches.ContainsKey(lldpConnection.RemoteName))
			{
				return;
			}

			var remoteSwitch = Switches[lldpConnection.RemoteName];
			var remotePort = remoteSwitch.GetPortForInterface(lldpConnection.RemoteIf);
			if (remotePort != null)
			{
				AddConnectionRow(mellanox, remoteSwitch, localPortOut, remotePort, routeInfo);
			}
		}

		private void AddConnectionRow(Mellanox srcMellanox, Mellanox dstMellanox, MellanoxInterface srcMellanoxIf, MellanoxInterface dstMellanoxIf, MellanoxMulticastRoute routeInfo)
		{
			var newRow = new GQIRow(
				new[]
				{
					new GQICell { Value = $"{srcMellanox.DmsElement.Name} {srcMellanoxIf.Name}" },
					new GQICell { Value = $"{dstMellanox.DmsElement.Name} {dstMellanoxIf.Name}" },
					new GQICell { Value = $"{routeInfo.SourceIp}"},
					new GQICell { Value = $"{routeInfo.MulticastGroup}"},
					new GQICell { Value = $"{routeInfo.RpfNeighbor}"},
				});
			retrievedConnections.Add(newRow);
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			multiCastGroup = args.GetArgumentValue(_multiCast);
			return default;
		}

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			thisDms = DmsFactory.CreateDms(new GqiConnection(args.DMS));
			return new OnInitOutputArgs();
		}

		private void RetrieveSwitches()
		{
			_switches = new Dictionary<string, Mellanox>();
			var elements = thisDms.GetElements().Where(e => e.Protocol.Name == "Mellanox Technologies MLNX-OS Manager").Select(e => new Mellanox(e));
			foreach (var element in elements)
			{
				if (_switches.ContainsKey(element.HostName))
				{
					throw new ArgumentException($"Found two Mellonox with system name {element.HostName}");
				}

				_switches[element.HostName] = element;
			}
		}

		public class GqiConnection : ICommunication
		{
			private readonly GQIDMS _gqiDms;

			public GqiConnection(GQIDMS gqiDms)
			{
				_gqiDms = gqiDms ?? throw new ArgumentNullException(nameof(gqiDms));
			}

			public static void AddSubscriptions(NewMessageEventHandler handler, string handleGuid, SubscriptionFilter[] subscriptions)
			{
				throw new NotSupportedException();
			}

			public static void ClearSubscriptions(NewMessageEventHandler handler, string handleGuid, bool replaceWithEmpty = false)
			{
				throw new NotSupportedException();
			}

			public void AddSubscriptionHandler(NewMessageEventHandler handler)
			{
				throw new NotSupportedException();
			}

			public void AddSubscriptions(NewMessageEventHandler handler, string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions)
			{
				throw new NotSupportedException();
			}

			public void ClearSubscriptionHandler(NewMessageEventHandler handler)
			{
				throw new NotSupportedException();
			}

			public void ClearSubscriptions(string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions, bool force = false)
			{
				throw new NotSupportedException();
			}

			public DMSMessage[] SendMessage(DMSMessage message)
			{
				return _gqiDms.SendMessages(message);
			}

			public DMSMessage SendSingleRawResponseMessage(DMSMessage message)
			{
				return _gqiDms.SendMessage(message);
			}

			public DMSMessage SendSingleResponseMessage(DMSMessage message)
			{
				return _gqiDms.SendMessage(message);
			}
		}
	}
}