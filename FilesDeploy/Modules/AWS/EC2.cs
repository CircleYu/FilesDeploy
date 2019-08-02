using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace EC2_Manager.Modules.AWS
{
	public class EC2
	{
		enum InstanceState
		{
			pending = 0,
			running = 16,
			shutting_down = 32,
			terminated = 48,
			stopping = 64,
			stopped = 80,
		}

		private Action<string, string, Action> _showMsgBox;
		private Action<string, Action> _showErrorBox;

		private IAmazonEC2 _ec2Client;
		private readonly string _updateTag;

		private Dictionary<string, string> _modifySGInstances = null;

		public List<string> EC2Instances
		{
			get
			{
				return CheckEC2ListLifeCycle(FindEC2InstancesWithSpecifiedTag(_updateTag));
			}
		}

		public EC2(RegionEndpoint regionEndpoint, string updateTag, string fireWallTag, Action<string, string, Action> showMsgBox, Action<string, Action> showErrorBox)
		{
			_ec2Client = new AmazonEC2Client(regionEndpoint);
			_updateTag = updateTag;
			FindSecurityGroupWithSpecifiedTag(fireWallTag);
			_showMsgBox = showMsgBox;
			_showErrorBox = showErrorBox;
		}
		public void CloseFireWall()
		{
			foreach (KeyValuePair<string, string> it in _modifySGInstances)
			{
				if (CheckEC2LifeCycle(it.Key))
				{
					var networkInterface = GetNetworkInterface(it.Key);
					List<string> newGroups = new List<string>();
					foreach (GroupIdentifier sg in networkInterface.Groups)
					{
						newGroups.Add(sg.GroupId);
					}
					if (newGroups.Contains(it.Value))
					{
						newGroups.Remove(it.Value);
					}
					ModifyNetworkInterface(networkInterface.NetworkInterfaceId, newGroups);
				}
			}
			_showMsgBox("CloseFireWall OK", "CloseFireWall", null);
		}

		public void OpenFireWall()
		{
			foreach (KeyValuePair<string, string> it in _modifySGInstances)
			{
				if (CheckEC2LifeCycle(it.Key))
				{
					var networkInterface = GetNetworkInterface(it.Key);
					if (networkInterface == null)
					{
						return;
					}

					List<string> newGroups = new List<string>();
					foreach (GroupIdentifier sg in networkInterface.Groups)
					{
						newGroups.Add(sg.GroupId);
					}
					if (!newGroups.Contains(it.Value))
					{
						newGroups.Add(it.Value);
					}

					ModifyNetworkInterface(networkInterface.NetworkInterfaceId, newGroups);
				}
			}

			_showMsgBox("OpenFireWall OK", "OpenFireWall", null);
		}

		private InstanceNetworkInterface GetNetworkInterface(string instanceId)
		{
			try
			{
				var response = _ec2Client.DescribeInstances(new DescribeInstancesRequest
				{
					InstanceIds = new List<string> { instanceId }
				});

				var instancesList = response.Reservations;
				var ec2Instances = instancesList[0].Instances;
				var ec2Instance = ec2Instances[0];
				var networkInterface = ec2Instance.NetworkInterfaces[0];

				return networkInterface;
			}
			catch (AmazonEC2Exception ec2Exception)
			{
				if (ec2Exception.ErrorCode != null &&
						(ec2Exception.ErrorCode.Equals("InvalidAccessKeyId") || ec2Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Permission denied DescribeInstances : {ec2Exception.ErrorCode}", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"AWS error occurred when DescribeInstances: {ec2Exception.Message}", null);
					}
				}
				return null;
			}
			catch (Exception exception)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox($"Exception occurred: {exception.Message}", null);
				}
				return null;
			}
		}

		private void ModifyNetworkInterface(string networkInterfacesID, List<string> newGroups)
		{
			try
			{
				var response = _ec2Client.ModifyNetworkInterfaceAttribute(new ModifyNetworkInterfaceAttributeRequest
				{
					Groups = newGroups,
					NetworkInterfaceId = networkInterfacesID
				});
			}
			catch (AmazonEC2Exception ec2Exception)
			{
				if (ec2Exception.ErrorCode != null &&
						(ec2Exception.ErrorCode.Equals("InvalidAccessKeyId") || ec2Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Permission denied ModifyNetworkInterfaceAttribute : {ec2Exception.ErrorCode}", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"AWS error occurred when ModifyNetworkInterfaceAttribute: {ec2Exception.Message}", null);
					}
				}
			}
			catch (Exception exception)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox($"Exception occurred: {exception.Message}", null);
				}
			}
		}

		private List<string> FindEC2InstancesWithSpecifiedTag(string tagName)
		{
			List<string> instanceIDs = new List<string>();

			Console.WriteLine("Looking for tags matching: {0}", tagName);
			var describeTagsResponse = _ec2Client.DescribeTags();

			var itemsWithSpecifiedTag =
				describeTagsResponse.Tags.Where(
					x => x.Key.Equals(tagName, StringComparison.OrdinalIgnoreCase) &&
						x.ResourceType == ResourceType.Instance);

			if (!itemsWithSpecifiedTag.Any())
			{
				Console.WriteLine("No items found matching the specified tag. tag = {0}", tagName);
				if (_showErrorBox != null)
				{
					_showErrorBox($"No items found matching the specified tag. tag = {tagName}", null);
				}
				return null;
			}

			foreach (var item in itemsWithSpecifiedTag)
			{
				Console.WriteLine($"Found item: key: {item.Key}, value: {item.Value}, resouceId: {item.ResourceId}, resourceType: {item.ResourceType}");
				instanceIDs.Add(item.ResourceId);
			}

			return instanceIDs;
		}

		private void FindSecurityGroupWithSpecifiedTag(string tagName)
		{
			_modifySGInstances = new Dictionary<string, string>();

			Console.WriteLine("Looking for tags matching: {0}", tagName);
			var describeTagsResponse = _ec2Client.DescribeTags();

			var itemsWithSpecifiedTag =
				describeTagsResponse.Tags.Where(
					x =>
						x.Key.Equals(tagName, StringComparison.OrdinalIgnoreCase) &&
						x.ResourceType == ResourceType.Instance);

			if (!itemsWithSpecifiedTag.Any())
			{
				Console.WriteLine("No items found matching the specified tag. tag = {0}", tagName);
				if (_showErrorBox != null)
				{
					_showErrorBox($"No items found matching the specified tag. tag = {tagName}", null);
				}
				return;
			}

			foreach (var item in itemsWithSpecifiedTag)
			{
				Console.WriteLine($"Found item: key: {item.Key}, value: {item.Value}, resouceId: {item.ResourceId}, resourceType: {item.ResourceType}");
				var sgWithSpecifiedTag =
				describeTagsResponse.Tags.Where(
					x => x.Value != null &&
						x.Value.Equals(item.Value, StringComparison.OrdinalIgnoreCase) &&
						x.ResourceType == ResourceType.SecurityGroup);

				if (!sgWithSpecifiedTag.Any())
				{
					Console.WriteLine("No items found matching the specified tag. tag = {0}", item.Value);
					if (_showErrorBox != null)
					{
						_showErrorBox($"No items found matching the specified tag. tag = {item.Value}", null);
					}
					return;
				}

				foreach (var sg in sgWithSpecifiedTag)
				{
					_modifySGInstances[item.ResourceId] = sg.ResourceId;
				}
			}
		}

		private List<string> CheckEC2ListLifeCycle(List<string> instanceIDs)
		{
			if (instanceIDs == null)
			{
				return null;
			}

			try
			{
				List<string> runningInstances = new List<string>();

				var response = _ec2Client.DescribeInstanceStatus(new DescribeInstanceStatusRequest
				{
					InstanceIds = instanceIDs
				});

				List<InstanceStatus> instanceStatuses = response.InstanceStatuses;

				foreach (InstanceStatus iStatus in instanceStatuses)
				{

					if ((InstanceState)iStatus.InstanceState.Code == InstanceState.running)
					{
						runningInstances.Add(iStatus.InstanceId);
					}
				}

				return runningInstances;
			}
			catch (AmazonEC2Exception ec2Exception)
			{
				if (ec2Exception.ErrorCode != null &&
						(ec2Exception.ErrorCode.Equals("InvalidAccessKeyId") || ec2Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Permission denied ModifyNetworkInterfaceAttribute : {ec2Exception.ErrorCode}", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"AWS error occurred when ModifyNetworkInterfaceAttribute: {ec2Exception.Message}", null);
					}
				}
				return null;
			}
			catch (Exception exception)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox($"Exception occurred: {exception.Message}", null);
				}
				return null;
			}
		}

		private bool CheckEC2LifeCycle(string instanceID)
		{
			if (instanceID == null)
			{
				return false;
			}

			try
			{
				var response = _ec2Client.DescribeInstanceStatus(new DescribeInstanceStatusRequest
				{
					InstanceIds = new List<string>() { instanceID }
				});

				List<InstanceStatus> instanceStatuses = response.InstanceStatuses;

				foreach (InstanceStatus iStatus in instanceStatuses)
				{
					return (InstanceState)iStatus.InstanceState.Code == InstanceState.running;
				}

				return false;
			}
			catch (AmazonEC2Exception ec2Exception)
			{
				if (ec2Exception.ErrorCode != null &&
						(ec2Exception.ErrorCode.Equals("InvalidAccessKeyId") || ec2Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Permission denied ModifyNetworkInterfaceAttribute : {ec2Exception.ErrorCode}", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"AWS error occurred when ModifyNetworkInterfaceAttribute: {ec2Exception.Message}", null);
					}
				}
				return false;
			}
			catch (Exception exception)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox($"Exception occurred: {exception.Message}", null);
				}
				return false;
			}
		}
	}
}
