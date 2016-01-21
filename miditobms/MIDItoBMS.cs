using arookas.Audio.MusicalInstrumentDigitalInterface;
using System;
using System.IO;
using System.Linq;

namespace arookas {
	static class MIDItoBMS {
		static BmsOptions sOptions;
		static readonly Version sVersion = new Version(0, 3, 0);
		static readonly string sSeparator = new String('=', 75);

		static int Main(string[] args) {
			write("miditobms v{0} arookas\n", sVersion);
			separator();
#if !DEBUG
			try {
#endif
			CommandLine cmd = new CommandLine(args);
			if (cmd.Count < 1) {
				write("Usage (drag & drop):\n");
				write("miditobms <input.mid>\n");
				write("Usage (command-line):\n");
				write("miditobms -input <in.mid> [-option [...]]\n");
				write("See official repo page for more details.\n");
				pause();
			}
			else {
				sOptions = LoadOptions(cmd);
				displayOptions(sOptions);
				write("Loading MIDI...\n");
				var midi = Midi.FromFile(sOptions.mInputFile);
				MidiConverter.Convert(midi, sOptions);
				write("Done!\n");
				pause();
			}
#if !DEBUG
			}
			catch {
				Error("Failed to convert MIDI.");
			}
#endif
			return 0;
		}

		// option loading
		static BmsOptions LoadOptions(CommandLine cmd) {
			if (cmd.Count == 1) {
				// drag & drop mode
				return new BmsOptions() {
					mInputFile = cmd[0],
					mOutputFile = String.Concat(cmd[0], ".bms"),
				};
			}
			var options = new BmsOptions() {
				mIgnoreBanks = findArg(cmd, "-ignoremidibanks"),
				mIgnorePrograms = findArg(cmd, "-ignoremidiprograms"),
				mIgnorePitchBends = findArg(cmd, "-ignoremidipitchbends"),
				mIgnoreExpressions = findArg(cmd, "-ignoremidiexpressions"),
				mIgnoreVolumes = findArg(cmd, "-ignoremidivolumes"),
				mIgnorePans = findArg(cmd, "-ignoremidipans"),
				mAddTrackInit = findArg(cmd, "-addtrackinit"),
				mSkipPitchRange = findArg(cmd, "-skippitchrange"),
				mBatch = findArg(cmd, "-batch"),
			};
			if (!getArg(cmd, "-input", out options.mInputFile)) {
				error("Missing input parameter.");
			}
			if (!getArg(cmd, "-output", out options.mOutputFile)) {
				options.mOutputFile = String.Concat(options.mInputFile, ".bms");
			}
			getArg(cmd, "-trackdetection", parseEnum, out options.mTrackMode, BmsTrackDetectionMode.Auto);
			getArg(cmd, "-perfduration", UInt16.TryParse, out options.mPerfDuration);
			getArg(cmd, "-velscale", Double.TryParse, out options.mVelScale, 1.0f);
			return options;
		}
		static bool findArg(CommandLine cmd, string arg) {
			return cmd.Any(p => p.Name.Equals(arg, StringComparison.InvariantCultureIgnoreCase));
		}
		static bool getArg(CommandLine cmd, string name, out string result) {
			var param = cmd.LastOrDefault(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			if (param != null && param.Count == 1) {
				result = param[0];
				return true;
			}
			result = null;
			return false;
		}
		static bool getArg<T>(CommandLine cmd, string name, TryParse<T> parser, out T result, T def = default(T)) {
			T temp;
			var param = cmd.LastOrDefault(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			if (param != null && param.Count == 1 && parser(param[0], out temp)) {
				result = temp;
				return true;
			}
			result = def;
			return false;
		}

		static bool parseEnum<TEnum>(string value, out TEnum result) where TEnum : struct {
			return Enum.TryParse(value, true, out result);
		}

		// info display
		static void displayOptions(BmsOptions options) {
			displayOption("Input File          ", Path.GetFileName(options.mInputFile));
			displayOption("Output File         ", Path.GetFileName(options.mOutputFile));
			displayOption("Track Detection Mode", options.mTrackMode);
			displayOption("Performance Duration", options.mPerfDuration);
			displayOption("Velocity Scale      ", options.mVelScale);
			displayFlag(options.mIgnoreBanks, "Ignore MIDI bank-select events");
			displayFlag(options.mIgnorePrograms, "Ignore MIDI program-select events");
			displayFlag(options.mIgnorePitchBends, "Ignore MIDI pitch-bend events");
			displayFlag(options.mIgnoreVolumes, "Ignore MIDI volume events");
			displayFlag(options.mIgnorePans, "Ignore MIDI pan events");
			displayFlag(options.mAddTrackInit, "Add track-initialization commands to child tracks");
			displayFlag(options.mSkipPitchRange, "Don't add default pitch-bend range commands to child tracks");
		}
		static void displayOption(string name, object value) {
			Console.WriteLine("{0} : {1}", name, value);
		}
		static void displayFlag(bool option, string description) {
			if (option) {
				Console.WriteLine("  + {0}", description);
			}
		}

		// console
		public static void separator() {
			write(sSeparator);
			write("\n");
		}
		public static void write(string msg) {
			write("{0}", msg);
		}
		public static void write(string format, params object[] args) {
			Console.Write(format, args);
		}
		public static void warn(string msg) {
			warn("{0}", msg);
		}
		public static void warn(string format, params object[] args) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			write("WARNING: ");
			write(format, args);
			Console.ResetColor();
		}
		public static void error(string msg) {
			error("{0}", msg);
		}
		public static void error(string format, params object[] args) {
			Console.ForegroundColor = ConsoleColor.Red;
			write("ERROR: ");
			write(format, args);
			Console.ResetColor();
			pause();
			Environment.Exit(1);
		}
		public static void pause() {
			if (!sOptions.mBatch) {
				Console.ReadKey();
			}
		}
	}
}
