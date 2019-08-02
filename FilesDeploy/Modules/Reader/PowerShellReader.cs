using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EC2_Manager.Modules.Reader
{
	public static class PowerShellReader
	{
		public static List<string> GetPowerShellScript(string filename)
		{
			return LoadScript(@"PowerShell/" + filename);
		}

		private static List<string> LoadScript(string filename)
		{
			try
			{
				// Create an instance of StreamReader to read from our file. 
				// The using statement also closes the StreamReader. 
				using (StreamReader sr = new StreamReader(filename))
				{
					// use a string builder to get all our lines from the file 
					List<string> fileContents = new List<string>();

					// string to hold the current line 
					string curLine;

					// loop through our file and read each line into our 
					// stringbuilder as we go along 

					while ((curLine = sr.ReadLine()) != null)
					{
						// read each line and MAKE SURE YOU ADD BACK THE 
						// LINEFEED THAT IT THE ReadLine() METHOD STRIPS OFF 
						fileContents.Add(curLine);
					}

					// call RunScript and pass in our file contents 
					// converted to a string 
					return fileContents;
				}
			}
			catch (Exception e)
			{
				// Let the user know what went wrong. 
				string errorText = "The file could not be read:";
				errorText += e.Message + "\n";
				return null;
			}
		}
	}
}
