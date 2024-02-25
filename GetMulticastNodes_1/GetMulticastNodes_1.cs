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
namespace GetMulticastNodes_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Mellanox;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	[GQIMetaData(Name = "GetMulticastNodes")]
	public class MyDataSource : IGQIDataSource, IGQIOnInit
	{
		private readonly int limit = 30;
		private Mellanox[] _switches;
		private int leftOff;
		private IDms thisDms;

		private Mellanox[] Switches
		{
			get
			{
				if (_switches == null)
				{
					_switches = thisDms.GetElements().Where(e => e.Protocol.Name == "Mellanox Technologies MLNX-OS Manager").Select(e => new Mellanox(e)).ToArray();
				}

				return _switches;
			}
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Element ID"),
				new GQIStringColumn("Element Name"),
				new GQIStringColumn("IP address"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();
			for (int i = leftOff; i < Switches.Length; i++)
			{
				var mellanox = Switches[i];
				leftOff++;
				var newRow = new GQIRow(
					new[]
					{
						new GQICell { Value = mellanox.DmsElement.DmsElementId.Value },
						new GQICell { Value = mellanox.DmsElement.Name },
						new GQICell { Value = mellanox.IpAddress },
					});

				rows.Add(newRow);

				if (rows.Count > limit)
				{
					break;
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = leftOff < Switches.Length,
			};
		}

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			thisDms = DmsFactory.CreateDms(new GqiConnection(args.DMS));
			return new OnInitOutputArgs();
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