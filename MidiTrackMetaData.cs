using arookas.Audio.MusicalInstrumentDigitalInterface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace arookas
{
	class MidiTrackMetaData
	{
		public MidiTrack MidiTrack { get; private set; }
		public BmsTrackType? TrackType { get; private set; }
		public int? TrackId { get; private set; }
		public short? TrackArg { get; private set; }

		static Regex trackRegex = new Regex(@"(?'key'[a-z0-9.-]+)\s*:\s*(?'value'[a-z0-9.-]+)", RegexOptions.IgnoreCase);
		static Regex intRegex = new Regex(@"^(?'isNeg'-)?(?'value'[0-9a-f]+)(?'isHex'h)?$", RegexOptions.IgnoreCase);
		static Regex floatRegex = new Regex(@"^(?'value'-?[0-9.]+)?$");
		static Dictionary<string, Parser> parsers = new Dictionary<string, Parser>()
		{
			{ "track-type", ParseTrackType },
			{ "track-id", ParseTrackId },
			{ "track-arg", ParseTrackArg },
		};

		MidiTrackMetaData(MidiTrack mTrack)
		{
			MidiTrack = mTrack;
			if (MidiTrack.Name != null)
			{
				var matches = trackRegex.Matches(MidiTrack.Name);
				for (int i = 0; i < matches.Count; ++i)
				{
					string keyName = matches[i].Groups["key"].Value.ToLowerInvariant();
					string keyValue = matches[i].Groups["value"].Value;
					if (parsers.ContainsKey(keyName))
					{
						parsers[keyName](this, keyValue);
					}
				}
			}
		}

		public static MidiTrackMetaData FromTrack(MidiTrack mTrack)
		{
			if (mTrack == null)
			{
				return null;
			}
			return new MidiTrackMetaData(mTrack);
		}

		static void ParseTrackType(MidiTrackMetaData meta, string keyValue)
		{
			BmsTrackType value;
			if (Enum.TryParse(keyValue, true, out value))
			{
				meta.TrackType = value;
			}
		}
		static void ParseTrackId(MidiTrackMetaData meta, string keyValue)
		{
			int value;
			if (ParseInt(keyValue, 0, 15, out value))
			{
				meta.TrackId = value;
			}
		}
		static void ParseTrackArg(MidiTrackMetaData meta, string keyValue)
		{
			int value;
			if (ParseInt(keyValue, Int16.MinValue, Int16.MaxValue, out value))
			{
				meta.TrackArg = (short)value;
			}
		}

		static bool ParseInt(string str, int min, int max, out int result)
		{
			result = 0;
			var match = intRegex.Match(str);
			if (!match.Success)
			{
				return false;
			}
			string value = match.Groups["value"].Value;
			bool isHex = match.Groups["isHex"].Success;
			bool isNeg = match.Groups["isNeg"].Success;
			NumberStyles style = NumberStyles.None;
			if (isHex)
			{
				style |= NumberStyles.AllowHexSpecifier;
			}
			if (!Int32.TryParse(value, style, null, out result))
			{
				return false;
			}
			if (isNeg)
			{
				result = -result;
			}
			return result >= min && result <= max;
		}

		delegate void Parser(MidiTrackMetaData meta, string keyValue);
	}

	enum BmsTrackType
	{
		Root,
		Child,
	}
}
