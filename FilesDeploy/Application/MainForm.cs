using System;
using System.Threading;
using System.Windows.Forms;
using Amazon;
using EC2_Manager.Application;
using EC2_Manager.Modules.AWS;
using EC2_Manager.Modules.Reader;

namespace EC2_Manager
{
	public partial class MainForm : Form
	{
		private EC2 _ce2Client;
		private SSM _ssmClient;
		private S3 _s3Clinet;

		public MainForm()
		{
			InitializeComponent();
			XmlReader.LoadLocalSettings(@"Control/LocalSettings.config");
			if (CheckLocalSettings())
			{
				AwsInitialize(RegionEndpoint.GetBySystemName(LocalSettings.Region));
			}
		}

		private void AwsInitialize(RegionEndpoint regionEndpoint)
		{
			_ce2Client = new EC2(regionEndpoint, LocalSettings.UpdateTag, LocalSettings.FireWallTag, ShowMessageBox, ShowErrorBox);
			_ssmClient = new SSM(regionEndpoint, ShowMessageBox, ShowErrorBox);
			_s3Clinet = new S3(regionEndpoint, ShowMessageBox, ShowErrorBox);
		}


		private void Update_Click(object sender, System.EventArgs e)
		{
			if (CheckLocalSettings())
			{
				_s3Clinet.PutObject(LocalSettings.S3BucketName, () =>
				{
					_ssmClient.SendUpdateCommand(_ce2Client.EC2Instances, LocalSettings.S3BucketName, _s3Clinet.S3KeyName);
				});
			}
		}

		private void ShowMessageBox(string text, string caption, Action action)
		{
			DialogResult result = MessageBox.Show(text, caption, MessageBoxButtons.OK);

			if (result == DialogResult.OK)
			{
				if (action != null)
				{
					action();
				}
			}
		}

		private void ShowErrorBox(string text, Action action)
		{
			DialogResult result = MessageBox.Show(text, "ERROR!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);

			if (result == DialogResult.OK)
			{
				if (action != null)
				{
					action();
				}
			}
		}

		private bool CheckLocalSettings()
		{
			if (string.IsNullOrEmpty(LocalSettings.Region) || string.IsNullOrEmpty(LocalSettings.UpdateTag) || string.IsNullOrEmpty(LocalSettings.FireWallTag) || string.IsNullOrEmpty(LocalSettings.S3BucketName))
			{
				ShowErrorBox("請先設定Control/LocalSettings.config", null);
				Enabled = false;
				return false;
			}

			Enabled = true;
			return true;
		}
	}
}
