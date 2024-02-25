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

namespace GetMulticastIfs_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Mellanox;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	[GQIMetaData(Name = "GetMulticastIfs")]
	public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
	{
		private readonly GQIStringArgument _multiCast = new GQIStringArgument("Multicast") { IsRequired = true };
		private readonly int limit = 100;
		private Dictionary<string, Mellanox> _switches;
		private int leftOff;
		private string multiCastGroup;
		private IDms thisDms;
		private Dictionary<string, GQIRow> retrievedIfs;

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
				new GQIStringColumn("IF ID"),
				new GQIStringColumn("IF Name"),
				new GQIStringColumn("Hostname"),
				new GQIStringColumn("Operational"),
				new GQIDoubleColumn("Bitrate"),
				new GQIDoubleColumn("Utilization"),
				new GQIDoubleColumn("Errors Output"),
				new GQIDoubleColumn("Rate Errors Output"),
				new GQIDoubleColumn("Errors Input"),
				new GQIDoubleColumn("Rate Errors Input"),
				new GQIStringColumn("Custom Description"),
			};
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _multiCast };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (retrievedIfs == null)
			{
				RetrieveIfs();
			}

			List<GQIRow> rows = new List<GQIRow>(limit);
			for (int i = leftOff; i < retrievedIfs.Count; i++)
			{
				var row = retrievedIfs.ElementAt(i).Value;
				rows.Add(row);
				leftOff++;

				if (rows.Count >= limit)
				{
					break;
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = leftOff < retrievedIfs.Count,
			};
		}

		private void RetrieveIfs()
		{
			retrievedIfs = new Dictionary<string, GQIRow>();
			foreach (var mellanox in Switches.Values)
			{
				RetrieveIfsFromSwitch(mellanox);
			}
		}

		private void RetrieveIfsFromSwitch(Mellanox mellanox)
		{
			foreach (var multicastRoute in mellanox.MulticastRoutes.Where(m => multiCastGroup.Equals("All", StringComparison.InvariantCultureIgnoreCase) || m.MulticastGroup == multiCastGroup))
			{
				var portIn = mellanox.GetPortForInterface(multicastRoute.InterfaceIn);
				if (portIn == null)
				{
					continue;
				}

				bool outgoingFound = false;
				foreach (var outgoingIf in multicastRoute.OutgoingInterfaces)
				{
					// Local IF out
					var portOut = mellanox.GetPortForInterface(outgoingIf);
					if (portOut == null)
					{
						continue;
					}

					AddInterfaceRow(mellanox, portOut);
					outgoingFound = true;

					// Find the port on the remote switch from the outgoing IF.
					AddRemotePort(mellanox, portOut.Name);
				}

				if (outgoingFound)
				{
					// Local IF In
					AddInterfaceRow(mellanox, portIn);
				}
			}
		}

		private void AddRemotePort(Mellanox mellanox, string localPortOut)
		{
			if (!mellanox.LldpConnectionsByLocalIf.ContainsKey(localPortOut))
			{
				return;
			}

			var lldpConnection = mellanox.LldpConnectionsByLocalIf[localPortOut];
			if (!Switches.ContainsKey(lldpConnection.RemoteName))
			{
				return;
			}

			var remoteSwitch = Switches[lldpConnection.RemoteName];
			var remotePort = remoteSwitch.GetPortForInterface(lldpConnection.RemoteIf);
			if (remotePort != null)
			{
				AddInterfaceRow(remoteSwitch, remotePort);
			}
		}

		private void AddInterfaceRow(Mellanox mellanox, MellanoxInterface mellanoxIfIn)
		{
			var indexIf = $"{mellanox.DmsElement.Name} {mellanoxIfIn.Name}";
			var newRow = new GQIRow(
				new[]
				{
					new GQICell { Value = indexIf },
					new GQICell { Value = mellanoxIfIn.Name },
					new GQICell { Value = mellanox.HostName },
					new GQICell { Value = mellanoxIfIn.OperationalStatus },
					new GQICell { Value = mellanoxIfIn.Bitrate, DisplayValue = $"{mellanoxIfIn.Bitrate} Mbps" },
					new GQICell { Value = mellanoxIfIn.Utilization, DisplayValue = mellanoxIfIn.Utilization < 0.0 ? "N/A" : $"{mellanoxIfIn.Utilization} %" },
					new GQICell { Value = mellanoxIfIn.ErrorsOutput, DisplayValue = $"{mellanoxIfIn.ErrorsOutput} packets" },
					new GQICell { Value = mellanoxIfIn.ErrorRateOutput, DisplayValue = $"{mellanoxIfIn.ErrorRateOutput} packets/s" },
					new GQICell { Value = mellanoxIfIn.ErrorsInput, DisplayValue = $"{mellanoxIfIn.ErrorsInput} packets" },
					new GQICell { Value = mellanoxIfIn.ErrorRateInput, DisplayValue = $"{mellanoxIfIn.ErrorRateInput} packets/s" },
					new GQICell { Value = $"{mellanoxIfIn.CustomDescription} ({mellanox.HostName})"},
				});
			retrievedIfs[indexIf] = newRow;
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