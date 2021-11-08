using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LiveSplit.ComponentUtil;

namespace BoostRaceEnabler
{
    public class Program
    {
        private const string exeName = "GameApp_PcDx11_x64Final";

        public static void Main()
        {
            Application.EnableVisualStyles();
            Process game = Process.GetProcessesByName(exeName).FirstOrDefault(x => !x.HasExited);

            if (game == null)
            {
                MessageBox.Show("Please start the game first!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            var scanner = new SignatureScanner(game, game.MainModuleWow64Safe().BaseAddress, game.MainModuleWow64Safe().ModuleMemorySize);
            IntPtr ptr;

            ptr = scanner.Scan(new SigScanTarget(7, "48 8B 35 ???????? 8B 9E"));
            if (ptr != IntPtr.Zero)
            {
                // Code gets applied if the application determines the game is unpatched
                byte[] injectedcode = new byte[] { 0x83, 0x3D, 0x00, 0x00, 0x00, 0x00, 0x01, 0x75, 0x2F, 0x53, 0x48, 0x8B, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x8A, 0x9B, 0x60, 0x56, 0x00,
                        0x00, 0x84, 0xDB, 0x5B, 0x75, 0x1C, 0x66, 0x50, 0x8A, 0x86, 0x40, 0x03, 0x00, 0x00, 0x3C, 0x02, 0x75, 0x02, 0x04, 0x0A, 0x3C, 0x03, 0x75, 0x02, 0x04, 0x19, 0x88,
                        0x86, 0x40, 0x03, 0x00, 0x00, 0x66, 0x58, 0x8B, 0x9E, 0x40, 0x03, 0x00, 0x00, 0xEB, 0x3F };
                byte[] injectedcodecall = new byte[] { 0xEB, 0x85, 0x90, 0x90, 0x90, 0x90 };

                // Game Mode pointer
                IntPtr ptr2;
                ptr2 = scanner.Scan(new SigScanTarget(1, "74 56 8D 4B 38"));
                ptr2 += game.ReadValue<byte>(ptr2) + 0x1 + 0x2;
                ptr2 += 4 + game.ReadValue<int>(ptr2);

                // Data to calculate on the fly and inject
                byte[] offset = BitConverter.GetBytes((int)((Int64)ptr2 - (Int64)ptr + 0x72));
                offset.CopyTo(injectedcode, 2);

                // Other pointer
                ptr2 = scanner.Scan(new SigScanTarget(7, "48 83 C4 08 48 8B 35 ???????? 41 83 E6 00"));
                if (ptr2 != IntPtr.Zero)
                {
                    ptr2 += 4 + game.ReadValue<int>(ptr2);
                    offset = BitConverter.GetBytes((int)((Int64)ptr2 - (Int64)ptr + 0x68));
                    offset.CopyTo(injectedcode, 13);
                } else {
                    new byte[] { 0xEB, 0x11, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }.CopyTo(injectedcode, 9);
                }
                
                game.WriteBytes(ptr - 0x79, injectedcode);
                game.WriteBytes(ptr, injectedcodecall);

                MessageBox.Show("All exhibition races modes are now BOOST RACES!!!\n" +
                                "Only white wisps will appear from item boxes!\n\n" +
                                "To restore the default behaviour, either restart the game\nor launch this tool again!\n\n" +
                                "Enjoy! :)", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                ptr = scanner.Scan(new SigScanTarget(7, "48 8B 35 ???????? EB 85"));

                // Unpatching the game if the no patch was already applied, but ask for confirmation first
                if (MessageBox.Show("It appears your game is already patched\n" +
                                   "Do you want to remove the patch and restore stock settings?", "TSR Boost Race Enabler", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    Environment.Exit(0);
                }

                byte[] buffer = new byte[64];
                for (int index = 0; index < buffer.Length; ++index) buffer[index] = 0xCC;

                game.WriteBytes(ptr, new byte[] { 0x8B, 0x9E, 0x40, 0x03, 0x00, 0x00 });
                game.WriteBytes(ptr - 0x79, buffer);

                // RaceRulings reset
                ptr = scanner.Scan(new SigScanTarget(3, "48 8B 0D ???????? E8 ???????? 85 C0 75 28"));
                new DeepPointer(ptr + 4 + game.ReadValue<int>(ptr), 0x340).DerefOffsets(game, out IntPtr ptr2);

                switch (game.ReadValue<byte>(ptr2))
                {
                    case 0x0C:
                        game.WriteValue<byte>(ptr2, 0x02);
                        break;
                    case 0x1C:
                        game.WriteValue<byte>(ptr2, 0x03);
                        break;
                }

                MessageBox.Show(
                    "The patch has been removed!\n" +
                    "Enjoy your stock experience!", "TSR Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}