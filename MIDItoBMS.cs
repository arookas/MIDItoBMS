using arookas.Audio.MusicalInstrumentDigitalInterface;
using arookas.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace arookas
{
	static class MIDItoBMS
	{
		static BmsOptions bOptions;
		static readonly Version version = new Version(0, 3, 0);
		static readonly string seperator = new String('=', 75);

		public static Version Version { get { return version; } }

		static int Main(string[] arguments)
		{
			Message("miditobms v{0} arookas", version);
			Seperator();
			try
			{
				CommandLine cmd = new CommandLine(arguments);
				if (cmd.Count < 1)
				{
					Message("Usage (drag & drop):");
					Message("miditobms <input.mid>");
					Message("Usage (command-line):");
					Message("miditobms -input <in.mid> [-option [...]]");
					Message("See official repo page for more details.");
					Pause();
				}
				else
				{
					bOptions = LoadOptions(cmd);
					DisplayOptions(bOptions);
					Message("Loading MIDI...");
					Midi midi = Midi.FromFile(bOptions.InputFile);
					MidiConverter.Convert(midi, bOptions);
					Message("Done!");
					Pause();
				}
			}
#if DEBUG
			catch (Exception ex)
			{
				Error("{0}\n{1}\n{2}", ex.Message, ex.GetType().Name, ex.StackTrace);
			}
#else
			catch
			{
				Error("Failed to convert MIDI.");
			}
#endif
			return 0;
		}

		// option loading
		static BmsOptions LoadOptions(CommandLine cmd)
		{
			if (cmd.Count == 1)
			{
				// drag & drop mode
				return new BmsOptions()
				{
					InputFile = cmd[0],
					OutputFile = String.Concat(cmd[0], ".bms"),
				};
			}
			BmsOptions bOptions = new BmsOptions()
			{
				IgnoreMidiBanks = cmd.HasArg("-ignoremidibanks"),
				IgnoreMidiPrograms = cmd.HasArg("-ignoremidiprograms"),
				IgnoreMidiPitchBends = cmd.HasArg("-ignoremidipitchbends"),
				IgnoreMidiExpressions = cmd.HasArg("-ignoremidiexpressions"),
				IgnoreMidiVolumes = cmd.HasArg("-ignoremidivolumes"),
				IgnoreMidiPans = cmd.HasArg("-ignoremidipans"),
				AddTrackInit = cmd.HasArg("-addtrackinit"),
				SkipPitchRange = cmd.HasArg("-skippitchrange"),
				Batch = cmd.HasArg("-batch"),
			};
			if (!TryGetArg(cmd, "-input", out bOptions.InputFile))
			{
				Error("Missing input parameter.");
			}
			if (!TryGetArg(cmd, "-output", out bOptions.OutputFile))
			{
				bOptions.OutputFile = String.Concat(bOptions.InputFile, ".bms");
			}
			TryGetArg(cmd, "-trackdetection", TryParseEnum, out bOptions.TrackDetection, BmsTrackDetectionMode.Auto);
			TryGetArg(cmd, "-perfduration", UInt16.TryParse, out bOptions.PerfDuration);
			TryGetArg(cmd, "-velscale", Double.TryParse, out bOptions.VelocityScale, 1.0f);
			return bOptions;
		}
		static bool HasArg(this CommandLine cmd, string arg)
		{
			return cmd.Any(p => p.Name.Equals(arg, StringComparison.InvariantCultureIgnoreCase));
		}
		static bool TryGetArg(CommandLine cmd, string name, out string result)
		{
			var param = cmd.LastOrDefault(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			if (param != null && param.Count == 1)
			{
				result = param[0];
				return true;
			}
			result = null;
			return false;
		}
		static bool TryGetArg<T>(CommandLine cmd, string name, TryParse<T> parser, out T result, T def = default(T))
		{
			T temp;
			var param = cmd.LastOrDefault(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			if (param != null && param.Count == 1 && parser(param[0], out temp))
			{
				result = temp;
				return true;
			}
			result = def;
			return false;
		}
		static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct { return Enum.TryParse(value, true, out result); }

		// info display
		static void DisplayOptions(BmsOptions bOptions)
		{
			DisplayOption("Input File          ", Path.GetFileName(bOptions.InputFile));
			DisplayOption("Output File         ", Path.GetFileName(bOptions.OutputFile));
			DisplayOption("Track Detection Mode", bOptions.TrackDetection);
			DisplayOption("Performance Duration", bOptions.PerfDuration);
			DisplayOption("Velocity Scale      ", bOptions.VelocityScale);
			DisplayFlag(bOptions.IgnoreMidiBanks, "Ignore MIDI bank-select events");
			DisplayFlag(bOptions.IgnoreMidiPrograms, "Ignore MIDI program-select events");
			DisplayFlag(bOptions.IgnoreMidiPitchBends, "Ignore MIDI pitch-bend events");
			DisplayFlag(bOptions.IgnoreMidiVolumes, "Ignore MIDI volume events");
			DisplayFlag(bOptions.IgnoreMidiPans, "Ignore MIDI pan events");
			DisplayFlag(bOptions.AddTrackInit, "Add track-initialization commands to child tracks");
			DisplayFlag(bOptions.SkipPitchRange, "Don't add default pitch-bend range commands to child tracks");
		}
		static void DisplayFlag(bool option, string description)
		{
			if (option)
			{
				Console.WriteLine("  + {0}", description);
			}
		}
		static void DisplayOption(string name, object value)
		{
			Console.WriteLine("{0} : {1}", name, value);
		}

		// global util
		public static void Seperator()
		{
			Console.WriteLine(seperator);
		}
		public static void Message(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}
		public static void Warning(string format, params object[] args)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("WARNING: ");
			Console.WriteLine(format, args);
			Console.ResetColor();
		}
		public static void Error(string format, params object[] args)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("ERROR: ");
			Console.WriteLine(format, args);
			Console.ResetColor();
			Pause();
			Environment.Exit(1);
		}
		public static void Pause()
		{
			if (!bOptions.Batch)
			{
				Console.ReadKey();
			}
		}
	}
}
