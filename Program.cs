using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BoostRaceEnabler
{
    public class Program
    {
		[DllImport("kernel32")]
		private static extern int OpenProcess(int dwDesiredAccess, int bInheritHandle, int dwProcessId);
		 
		[DllImport("kernel32")]
		private static extern bool WriteProcessMemory(int hProcess, Int64 lpBaseAddress, byte[] lpbuffer, int nSize, int lpNumberOfBytesWritten);
		
		[DllImport("kernel32.dll")]
		private static extern bool ReadProcessMemory(int hProcess, Int64 lpBaseAddress, byte[] lpBuffer, int nSize, int lpNumberOfBytesRead);
		
        private static int processHandle;
		
		public static void Main()
		{
			Application.EnableVisualStyles();
			
			Process[] processList = Process.GetProcessesByName("GameApp_PcDx11_x64Final");
			
			if (processList.Length == 0)
			{
				MessageBox.Show("Please start the game first!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(0);
			}

			foreach (Process process in processList)
			{
				processHandle = OpenProcess(0x38, 0, process.Id);
				
				if (processHandle == 0)
				{
					MessageBox.Show("Could not access the game, please run as administrator!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(2);
                }
				
				if (process.MainModule.ModuleMemorySize != 367644672)
				{
					MessageBox.Show("Cannot apply patch. Please ensure you are\n" +
									"running the correct version of the game!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(2);
                }
	
				byte[] status = new byte[6];
				int bytesRead = 0;
				ReadProcessMemory(processHandle, 0x140268CDE, status, status.Length, bytesRead);

				if (BitConverter.ToString(status, 0) == "89-88-40-03-00-00")
				{
					// Code gets applied if the application determines the game is unpatched
					WriteProcessMemory(processHandle, 0x140268CDE, new byte[] {0xEB, 0x75, 0x90, 0x90, 0x90, 0x90}, 6, 0);
					WriteProcessMemory(processHandle, 0x140268D55, new byte[] {0x89, 0x88, 0x40, 0x03, 0x00, 0x00, 0x48, 0x8B, 0x0D, 0x3E,
																			   0x98, 0xEC, 0x00, 0x83, 0x39, 0x01, 0x74, 0x26, 0x83, 0xB8,
																			   0x40, 0x03, 0x00, 0x00, 0x02, 0x75, 0x0A, 0xC7, 0x80, 0x40,
																			   0x03, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x83, 0xB8, 0x40,
																			   0x03, 0x00, 0x00, 0x03, 0x75, 0x0A, 0xC7, 0x80, 0x40, 0x03,
																			   0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0xE9, 0x52, 0xFF, 0xFF, 0xFF}, 61, 0);
					
					MessageBox.Show(
					"All exhibition races modes are now BOOST RACES!!!\n" +
					"Only white wisps will appear from item boxes!\n\n" +
					"To restore the default behaviour, either restart the game\nor launch this tool again!\n\n" +
					"Enjoy! :)", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					// Unpatching the game if the no patch was already applied, but ask for confirmation first
					if(MessageBox.Show("It appears your game is already patched\n" +
									   "Do you want to remove the patch and restore stock settings?", "TSR Boost Race Enabler", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
					{
					   Environment.Exit(0);
					}

					WriteProcessMemory(processHandle, 0x140268CDE, new byte[] {0x89, 0x88, 0x40, 0x03, 0x00, 0x00}, 6, 0);
					
					byte[] buffer = new byte[61];
					for (int index = 0; index < buffer.Length; ++index)
					{
						buffer[index] = 0xCC;
					}
					WriteProcessMemory(processHandle, 0x140268D55, buffer, 61, 0);
					
					MessageBox.Show(
					"The patch has been removed!\n" +
					"Enjoy your stock experience!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
        }
    }
}