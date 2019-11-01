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

        [DllImport("kernel32")]
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

                if (process.MainModule.ModuleMemorySize != 0x15E9D000)
                {
                    MessageBox.Show("Cannot apply patch. Please ensure you are\n" +
                                    "running the correct version of the game!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(2);
                }

                byte[] status = new byte[1];
                ReadProcessMemory(processHandle, 0x14027F7A9, status, 1, 0);

                if (status[0] != 0xEB)
                {
                    // Code gets applied if the application determines the game is unpatched
                    byte[] injectedcode = new byte[] { 0x83, 0x3D, 0x35, 0x67, 0xEB, 0x00, 0x01, 0x75, 0x2F, 0x53, 0x48, 0x8B, 0x1D, 0x6F, 0x21, 0xEC, 0x00, 0x8A, 0x9B, 0x60, 0x56, 0x00,
                        0x00, 0x84, 0xDB, 0x5B, 0x75, 0x1C, 0x66, 0x50, 0x8A, 0x86, 0x40, 0x03, 0x00, 0x00, 0x3C, 0x02, 0x75, 0x02, 0x04, 0x0A, 0x3C, 0x03, 0x75, 0x02, 0x04, 0x19, 0x88,
                        0x86, 0x40, 0x03, 0x00, 0x00, 0x66, 0x58, 0x8B, 0x9E, 0x40, 0x03, 0x00, 0x00, 0xEB, 0x3F };
                    WriteProcessMemory(processHandle, 0x14027F730, injectedcode, injectedcode.Length, 0);

                    byte[] injectedcodecall = new byte[] { 0xEB, 0x85, 0x90, 0x90, 0x90, 0x90 };
                    WriteProcessMemory(processHandle, 0x14027F7A9, injectedcodecall, injectedcodecall.Length, 0);
                    MessageBox.Show("All exhibition races modes are now BOOST RACES!!!\n" +
                    "Only white wisps will appear from item boxes!\n\n" +
                    "To restore the default behaviour, either restart the game\nor launch this tool again!\n\n" +
                    "Enjoy! :)", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                else
                {
                    // Unpatching the game if the no patch was already applied, but ask for confirmation first
                    if (MessageBox.Show("It appears your game is already patched\n" +
                                       "Do you want to remove the patch and restore stock settings?", "TSR Boost Race Enabler", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        Environment.Exit(0);
                    }

                    WriteProcessMemory(processHandle, 0x14027F7A9, new byte[] { 0x8B, 0x9E, 0x40, 0x03, 0x00, 0x00 }, 6, 0);

                    byte[] buffer = new byte[64];
                    for (int index = 0; index < buffer.Length; ++index) buffer[index] = 0xCC;
                    WriteProcessMemory(processHandle, 0x14027F730, buffer, buffer.Length, 0);

                    // Read current game mode and revert the data in memory
                    buffer = new byte[8];
                    ReadProcessMemory(processHandle, 0x14102DE40, buffer, 8, 0);
                    byte[] currentstatus = new byte[1];
                    ReadProcessMemory(processHandle, BitConverter.ToInt64(buffer, 0) + 0x340, currentstatus, 1, 0);

                    if (currentstatus[0] == 0x0C)
                    {
                        WriteProcessMemory(processHandle, BitConverter.ToInt64(buffer, 0) + 0x340, new byte[] { 0x02 }, 1, 0);

                    }
                    else if (currentstatus[0] == 0x1C)
                    {
                        WriteProcessMemory(processHandle, BitConverter.ToInt64(buffer, 0) + 0x340, new byte[] { 0x03 }, 1, 0);
                    }

                    MessageBox.Show(
                    "The patch has been removed!\n" +
                    "Enjoy your stock experience!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}