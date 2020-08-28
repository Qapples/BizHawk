﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// ZXHawk: Core Class
	/// * ISettable *
	/// </summary>
	public partial class ZXSpectrum : ISettable<ZXSpectrum.ZXSpectrumSettings, ZXSpectrum.ZXSpectrumSyncSettings>
	{
		internal ZXSpectrumSettings Settings = new ZXSpectrumSettings();
		internal ZXSpectrumSyncSettings SyncSettings = new ZXSpectrumSyncSettings();

		public ZXSpectrumSettings GetSettings() => Settings.Clone();

		public ZXSpectrumSyncSettings GetSyncSettings() => SyncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(ZXSpectrumSettings o)
		{
			// restore user settings to devices
			if (_machine?.AYDevice != null)
			{
				((AY38912)_machine.AYDevice).PanningConfiguration = o.AYPanConfig;
				_machine.AYDevice.Volume = o.AYVolume;
			}
			if (_machine?.BuzzerDevice != null)
			{
				_machine.BuzzerDevice.Volume = o.EarVolume;
			}
			if (_machine?.TapeBuzzer != null)
			{
				_machine.TapeBuzzer.Volume = o.TapeVolume;
			}

			Settings = o;

			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(ZXSpectrumSyncSettings o)
		{
			bool ret = ZXSpectrumSyncSettings.NeedsReboot(SyncSettings, o);
			SyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class ZXSpectrumSettings
		{
			[DisplayName("AY-3-8912 Panning Config")]
			[Description("Set the PSG panning configuration.\nThe chip has 3 audio channels that can be outputed in different configurations")]
			[DefaultValue(AY38912.AYPanConfig.ABC)]
			public AY38912.AYPanConfig AYPanConfig { get; set; }

			[DisplayName("Core OSD Message Verbosity")]
			[Description("Full: Display all GUI messages\nMedium: Display only emulator/device generated messages\nNone: Show no messages")]
			[DefaultValue(OSDVerbosity.Medium)]
			public OSDVerbosity OSDMessageVerbosity { get; set; }

			[DisplayName("Tape Loading Volume")]
			[Description("The buzzer volume when the tape is playing")]
			[DefaultValue(50)]
			public int TapeVolume { get; set; }

			[DisplayName("Ear (buzzer output) Volume")]
			[Description("The buzzer volume when sound is being generated by the spectrum")]
			[DefaultValue(90)]
			public int EarVolume { get; set; }

			[DisplayName("AY-3-8912 Volume")]
			[Description("The AY chip volume")]
			[DefaultValue(75)]
			public int AYVolume { get; set; }

			[DisplayName("Default Background Color")]
			[Description("The default BG color")]
			[DefaultValue(0)]
			public int BackgroundColor { get; set; }

			[DisplayName("Use Core Border Color")]
			[Description("The core renders the background color from the last detected generated border color")]
			[DefaultValue(false)]
			public bool UseCoreBorderForBackground { get; set; }

			public ZXSpectrumSettings Clone()
			{
				return (ZXSpectrumSettings)MemberwiseClone();
			}

			public ZXSpectrumSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class ZXSpectrumSyncSettings
		{
			[DisplayName("Deterministic Emulation")]
			[Description("If true, the core agrees to behave in a completely deterministic manner")]
			[DefaultValue(true)]
			public bool DeterministicEmulation { get; set; }

			[DisplayName("Spectrum model")]
			[Description("The model of spectrum to be emulated")]
			[DefaultValue(MachineType.ZXSpectrum48)]
			public MachineType MachineType { get; set; }

			[DisplayName("Border type")]
			[Description("Select how to show the border area")]
			[DefaultValue(BorderType.Full)]
			public BorderType BorderType { get; set; }

			[DisplayName("Tape Load Speed")]
			[Description("Select how fast the spectrum loads the game from tape")]
			[DefaultValue(TapeLoadSpeed.Accurate)]
			public TapeLoadSpeed TapeLoadSpeed { get; set; }

			[DisplayName("Joystick 1")]
			[Description("The emulated joystick assigned to P1 (SHOULD BE UNIQUE TYPE!)")]
			[DefaultValue(JoystickType.Kempston)]
			public JoystickType JoystickType1 { get; set; }

			[DisplayName("Joystick 2")]
			[Description("The emulated joystick assigned to P2 (SHOULD BE UNIQUE TYPE!)")]
			[DefaultValue(JoystickType.SinclairLEFT)]
			public JoystickType JoystickType2 { get; set; }

			[DisplayName("Joystick 3")]
			[Description("The emulated joystick assigned to P3 (SHOULD BE UNIQUE TYPE!)")]
			[DefaultValue(JoystickType.SinclairRIGHT)]
			public JoystickType JoystickType3 { get; set; }

			[DisplayName("Auto-load/stop tape")]
			[Description("Auto or manual tape operation. Auto will attempt to detect CPU tape traps and automatically Stop/Start the tape")]
			[DefaultValue(true)]
			public bool AutoLoadTape { get; set; }


			public ZXSpectrumSyncSettings Clone()
			{
				return (ZXSpectrumSyncSettings)MemberwiseClone();
			}

			public ZXSpectrumSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(ZXSpectrumSyncSettings x, ZXSpectrumSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		/// <summary>
		/// Verbosity of the ZXHawk generated OSD messages
		/// </summary>
		public enum OSDVerbosity
		{
			/// <summary>
			/// Show all OSD messages
			/// </summary>
			Full,
			/// <summary>
			/// Only show machine/device generated messages
			/// </summary>
			Medium,
			/// <summary>
			/// No core-driven OSD messages
			/// </summary>
			None
		}

		/// <summary>
		/// The size of the Spectrum border
		/// </summary>
		public enum BorderType
		{
			/// <summary>
			/// How it was originally back in the day
			/// </summary>
			Full,

			/// <summary>
			/// All borders 24px
			/// </summary>
			Medium,

			/// <summary>
			/// All borders 10px
			/// </summary>
			Small,

			/// <summary>
			/// No border at all
			/// </summary>
			None,

			/// <summary>
			/// Top and bottom border removed so that the result is *almost* 16:9
			/// </summary>
			Widescreen,
		}

		/// <summary>
		/// The speed at which the tape is loaded
		/// NOT IN USE YET
		/// </summary>
		public enum TapeLoadSpeed
		{
			Accurate,
			//Fast,
			//Fastest
		}
	}

	/// <summary>
	/// Provides information on each emulated machine
	/// </summary>
	public class ZXMachineMetaData
	{
		public MachineType MachineType { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Released { get; set; }
		public string CPU { get; set; }
		public string Memory { get; set; }
		public string Video { get; set; }
		public string Audio { get; set; }
		public string Media { get; set; }
		public string OtherMisc { get; set; }

		private Dictionary<string, string> Data = new Dictionary<string, string>();

		/// <summary>
		/// Detailed info to be displayed within the settings UIs
		/// </summary>
		public static ZXMachineMetaData GetMetaObject(MachineType type)
		{
			ZXMachineMetaData m = new ZXMachineMetaData { MachineType = type };

			switch (type)
			{
				case MachineType.ZXSpectrum16:
					m.Name = "Sinclair ZX Spectrum 16K";
					m.Description = "The original ZX Spectrum 16K RAM version. Aside from available RAM this machine is technically identical to the 48K machine that was released at the same time. ";
					m.Description += "Due to the small amount of RAM, very few games were actually made to run on this model.";
					m.Released = "1982";
					m.CPU = "Zilog Z80A @ 3.5MHz";
					m.Memory = "16KB ROM / 16KB RAM";
					m.Video = "ULA @ 7MHz - PAL (50.08Hz Interrupt)";
					m.Audio = "Beeper (HW 1ch. / 10oct.) - Internal Speaker";
					m.Media = "Cassette Tape (via 3rd party external tape player)";
					break;
				case MachineType.ZXSpectrum48:
					m.Name = "Sinclair ZX Spectrum 48K / 48K+";
					m.Description = "The original ZX Spectrum 48K RAM version. 2 years later a 'plus' version was released that had a better keyboard. ";
					m.Description += "Electronically both the 48K and + are identical, so ZXHawk treats them as the same emulated machine. ";
					m.Description += "These machines dominated the UK 8-bit home computer market throughout the 1980's so most non-128k only games are compatible.";
					m.Released = "1982 (48K) / 1984 (48K+)";
					m.CPU = "Zilog Z80A @ 3.5MHz";
					m.Memory = "16KB ROM / 48KB RAM";
					m.Video = "ULA @ 7MHz - PAL (50.08Hz Interrupt)";
					m.Audio = "Beeper (HW 1ch. / 10oct.) - Internal Speaker";
					m.Media = "Cassette Tape (via 3rd party external tape player)";
					break;
				case MachineType.ZXSpectrum128:
					m.Name = "Sinclair ZX Spectrum 128";
					m.Description = "The first Spectrum 128K machine released in Spain in 1985 and later UK in 1986. ";
					m.Description += "With an updated ROM and new memory paging system to work around the Z80's 16-bit address bus. ";
					m.Description += "The 128 shipped with a copy of the 48k ROM (that is paged in when required) and a new startup menu with the option of dropping into a '48k mode'. ";
					m.Description += "Even so, there were some compatibility issues with older Spectrum games that were written to utilise some of the previous model's intricacies. ";
					m.Description += "Many games released after 1985 supported the new AY-3-8912 PSG chip making for far superior audio. The extra memory also enabled many games to be loaded in all at once (rather than loading each level from tape when needed).";
					m.Released = "1985 / 1986";
					m.CPU = "Zilog Z80A @ 3.5469 MHz";
					m.Memory = "32KB ROM / 128KB RAM";
					m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
					m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
					m.Media = "Cassette Tape (via 3rd party external tape player)";
					break;
				case MachineType.ZXSpectrum128Plus2:
					m.Name = "Sinclair ZX Spectrum +2";
					m.Description = "The first Sinclair Spectrum 128K machine that was released after Amstrad purchased Sinclair in 1986. ";
					m.Description += "Electronically it was almost identical to the 128, but with the addition of a built-in tape deck and 2 Sinclair Joystick ports.";
					m.Released = "1986";
					m.CPU = "Zilog Z80A @ 3.5469 MHz";
					m.Memory = "32KB ROM / 128KB RAM";
					m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
					m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
					m.Media = "Cassette Tape (via built-in Datacorder)";
					break;
				case MachineType.ZXSpectrum128Plus2a:
					m.Name = "Sinclair ZX Spectrum +2a";
					m.Description = "The +2a looks almost identical to the +2 but is a variant of the +3 machine that was released the same year (except with the same built-in datacorder that the +2 had rather than a floppy drive). ";
					m.Description += "Memory paging again changed significantly and this (along with memory contention timing changes) caused more compatibility issues with some older games. ";
					m.Description += "Although functionally identical to the +3, it does not contain floppy disk controller.";
					m.Released = "1987";
					m.CPU = "Zilog Z80A @ 3.5469 MHz";
					m.Memory = "64KB ROM / 128KB RAM";
					m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
					m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
					m.Media = "Cassette Tape (via built-in Datacorder)";
					break;
				case MachineType.ZXSpectrum128Plus3:
					m.Name = "Sinclair ZX Spectrum +3";
					m.Description = "Amstrad released the +3 the same year as the +2a, but it featured a built-in floppy drive rather than a datacorder. An external cassette player could still be connected though as in the older 48k models. ";
					m.Description += "Memory paging again changed significantly and this (along with memory contention timing changes) caused more compatibility issues with some older games. ";
					m.Released = "1987";
					m.CPU = "Zilog Z80A @ 3.5469 MHz";
					m.Memory = "64KB ROM / 128KB RAM";
					m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
					m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
					m.Media = "3\" Floppy Disk (via built-in Floppy Drive)";
					break;
				case MachineType.Pentagon128:
					m.Name = "(NOT WORKING YET) Pentagon 128 Clone";
					m.Description = " ";
					m.Description += " ";
					m.Released = " ";
					m.CPU = " ";
					m.Memory = " ";
					m.Video = " ";
					m.Audio = " ";
					m.Media = " ";
					break;
			}

			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.Name), m.Name.Trim());
			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.Description), m.Description.Trim());
			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.Released), m.Released.Trim());
			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.CPU), m.CPU.Trim());
			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.Memory), m.Memory.Trim());
			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.Video), m.Video.Trim());
			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.Audio), m.Audio.Trim());
			m.Data.Add(ZXSpectrum.GetMemberName((ZXMachineMetaData c) => c.Media), m.Media.Trim());

			return m;
		}

		/// <summary>
		/// Returns machine metadata as a formatted string (to be displayed in a TextBox)
		/// </summary>
		public static string GetMetaString(MachineType type)
		{
			var m = GetMetaObject(type);

			var sb = new StringBuilder();

			// get longest title
			int titleLen = 0;
			foreach (var d in m.Data)
			{
				if (d.Key.Length > titleLen)
					titleLen = d.Key.Length;
			}

			var maxDataLineLen = 40;

			// generate layout
			foreach (var d in m.Data)
			{
				var tLen = d.Key.Length;
				var makeup = (titleLen - tLen) / 4;
				sb.Append(d.Key + ":\t");
				for (int i = 0; i < makeup; i++)
				{
					if (tLen > 4)
						sb.Append('\t');
					else
					{
						makeup--;
						sb.Append('\t');
					}
				}

				// output the data splitting and tabbing as necessary
				var arr = d.Value.Split(' ');
				//int cnt = 0;

				var builder = new List<string>();
				string working = "";
				foreach (var s in arr)
				{
					var len = s.Length;
					if (working.Length + 1 + len > maxDataLineLen)
					{
						// new line needed
						builder.Add(working.Trim(' '));
						working = "";
					}
					working += s + " ";
				}

				builder.Add(working.Trim(' '));

				// output the data
				for (int i = 0; i < builder.Count; i++)
				{
					if (i != 0)
					{
						sb.Append('\t');
						sb.Append('\t');
					}

					sb.Append(builder[i]);
					sb.Append("\r\n");
				}
			}

			return sb.ToString();
		}
	}
}
