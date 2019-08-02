using System;
using System.Collections.Generic;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using EC2_Manager.Modules.Reader;

namespace EC2_Manager.Modules.AWS
{
	public class SSM
	{
		private Action<string, string, Action> _showMsgBox;
		private Action<string, Action> _showErrorBox;

		private IAmazonSimpleSystemsManagement _ssmClient;

		public SSM(RegionEndpoint regionEndpoint, Action<string, string, Action> showMsgBox, Action<string, Action> showErrorBox)
		{
			_ssmClient = new AmazonSimpleSystemsManagementClient(regionEndpoint);
			_showMsgBox = showMsgBox;
			_showErrorBox = showErrorBox;
		}

		public void SendStarServiceCommand(List<string> instanceIds)
		{
			if (instanceIds == null)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox("instanceIds == null", null);
				}
				return;
			}

			try
			{
				Dictionary<string, List<string>> command = new Dictionary<string, List<string>>();

				command["commands"] = PowerShellReader.GetPowerShellScript("StarAllService.txt");

				SendCommandRequest req = new SendCommandRequest("AWS-RunPowerShellScript", instanceIds)
				{
					Parameters = command,
					TimeoutSeconds = 600
				};

				var sendCommandResponse = _ssmClient.SendCommand(req);
				if (sendCommandResponse != null)
				{
					Console.WriteLine("HttpStatusCode: {0}", sendCommandResponse.HttpStatusCode);
					if (_showMsgBox != null)
					{
						_showMsgBox("StarService ok !", "SSM", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox("StarService fail !", null);
					}
				}
			}
			catch (AmazonSimpleSystemsManagementException ssmException)
			{
				if (ssmException.ErrorCode != null &&
						(ssmException.ErrorCode.Equals("InvalidAccessKeyId") || ssmException.ErrorCode.Equals("InvalidSecurity")))
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Permission denied SendCommand : {ssmException.ErrorCode}", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"AWS error occurred when SendCommand: {ssmException.Message}", null);
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

		public void SendStopServiceCommand(List<string> instanceIds)
		{
			if (instanceIds == null)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox("instanceIds == null", null);
				}
				return;
			}

			try
			{
				Dictionary<string, List<string>> command = new Dictionary<string, List<string>>();

				command["commands"] = PowerShellReader.GetPowerShellScript("StopAllService.txt");

				SendCommandRequest req = new SendCommandRequest("AWS-RunPowerShellScript", instanceIds)
				{
					Parameters = command,
					TimeoutSeconds = 600
				};

				var sendCommandResponse = _ssmClient.SendCommand(req);
				if (sendCommandResponse != null)
				{
					Console.WriteLine("HttpStatusCode: {0}", sendCommandResponse.HttpStatusCode);
					if (_showMsgBox != null)
					{
						_showMsgBox("StopService ok !", "SSM", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox("StopService fail !", null);
					}
				}
			}
			catch (AmazonSimpleSystemsManagementException ssmException)
			{
				if (ssmException.ErrorCode != null &&
						(ssmException.ErrorCode.Equals("InvalidAccessKeyId") || ssmException.ErrorCode.Equals("InvalidSecurity")))
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Permission denied SendCommand : {ssmException.ErrorCode}", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"AWS error occurred when SendCommand: {ssmException.Message}", null);
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

		public void SendUpdateCommand(List<string> instanceIds, string S3BucketName, string S3Key)
		{
			if (instanceIds == null)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox("instanceIds == null", null);
				}
				return;
			}

			try
			{
				Dictionary<string, List<string>> command = new Dictionary<string, List<string>>();

				command["commands"] = PowerShellReader.GetPowerShellScript("UpdateData.txt");

				for (int i = 0; i < command["commands"].Count; i++)
				{
					if (command["commands"][i].StartsWith("Read-S3Object"))
					{
						command["commands"][i] = string.Format(command["commands"][i], S3BucketName, S3Key);
					}
				}

				SendCommandRequest req = new SendCommandRequest("AWS-RunPowerShellScript", instanceIds)
				{
					Parameters = command,
					TimeoutSeconds = 600
				};

				var sendCommandResponse = _ssmClient.SendCommand(req);
				if (sendCommandResponse != null)
				{
					Console.WriteLine("HttpStatusCode: {0}", sendCommandResponse.HttpStatusCode);
					if (_showMsgBox != null)
					{
						_showMsgBox("Update ok !", "SSM", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox("Update fail !", null);
					}
				}
			}
			catch (AmazonSimpleSystemsManagementException ssmException)
			{
				if (ssmException.ErrorCode != null &&
						(ssmException.ErrorCode.Equals("InvalidAccessKeyId") || ssmException.ErrorCode.Equals("InvalidSecurity")))
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Permission denied SendCommand : {ssmException.ErrorCode}", null);
					}
				}
				else
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"AWS error occurred when SendCommand: {ssmException.Message}", null);
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
	}
}
