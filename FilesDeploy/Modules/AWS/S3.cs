using System;
using System.IO;
using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace EC2_Manager.Modules.AWS
{
	public class S3
	{
		private Action<string, string, Action> _showMsgBox;
		private Action<string, Action> _showErrorBox;

		private IAmazonS3 _s3Client;

		private const string directoryPath = "Upload";

		public string S3KeyName
		{
			get; private set;
		}

		public S3(RegionEndpoint regionEndpoint, Action<string, string, Action> showMsgBox, Action<string, Action> showErrorBox)
		{
			_s3Client = new AmazonS3Client(regionEndpoint);
			_showMsgBox = showMsgBox;
			_showErrorBox = showErrorBox;
		}

		/// <summary>
		/// 在Upload資料夾內只能放一個檔案
		/// </summary>
		/// <param name="bucketName"></param>
		public void PutObject(string bucketName,Action callback)
		{
			DirectoryInfo di = new DirectoryInfo(directoryPath);
			if (di.GetFiles().Length == 0)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox($"在{directoryPath}資料夾內無任何檔案", null);
				}
				return;
			}
			if (di.GetFiles().Length >= 2)
			{
				if (_showErrorBox != null)
				{
					_showErrorBox($"在{directoryPath}資料夾內只能放一個檔案", null);
				}
				return;
			}
			bool putOK = true;
			foreach (FileInfo fi in di.GetFiles())
			{
				try
				{
					var putRequest = new PutObjectRequest
					{
						BucketName = bucketName,
						Key = fi.Name,
						FilePath = fi.FullName
					};

					PutObjectResponse response = _s3Client.PutObject(putRequest);
					putOK &= (response.HttpStatusCode == HttpStatusCode.OK);
					S3KeyName = fi.Name;
				}
				catch (AmazonS3Exception amazonS3Exception)
				{
					if (amazonS3Exception.ErrorCode != null &&
						(amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
					{
						if (_showErrorBox != null)
						{
							_showErrorBox($"Permission denied putting {fi.FullName}: {amazonS3Exception.ErrorCode}", null);
						}
					}
					else
					{
						if (_showErrorBox != null)
						{
							_showErrorBox($"AWS error occurred when putting {fi.FullName}: {amazonS3Exception.Message}", null);
						}
					}
					return;
				}
				catch (Exception exception)
				{
					if (_showErrorBox != null)
					{
						_showErrorBox($"Exception occurred: {exception.Message}", null);
					}
					return;
				}
			}

			if (putOK)
			{
				if (callback != null)
				{
					callback();
				}
			}
		}
	}
}
