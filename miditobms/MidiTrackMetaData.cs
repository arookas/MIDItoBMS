using arookas.Audio.MusicalInstrumentDigitalInterface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace arookas
{
	class MidiTrackMetaData
	{
		MidiTrack mTrack;
		BmsTrackType mType;
		int? mId;
		short? mArg;

		public MidiTrack MidiTrack {
			get {
				return mTrack;
			}
		}
		public BmsTrackType? TrackType {
			get {
				return mType;
			}
		}
		public int? TrackId {
			get {
				return mId;
			}
		}
		public short? TrackArg {
			get {
				return mArg;
			}
		}

		static Regex sTrackRegex = new Regex(@"(?'key'[a-z0-9.-]+)\s*:\s*(?'value'[a-z0-9.-]+)", RegexOptions.IgnoreCase);
		static Regex sIntRegex = new Regex(@"^(?'isNeg'-)?(?'value'[0-9a-f]+)(?'isHex'h)?$", RegexOptions.IgnoreCase);
		static Regex sFloatRegex = new Regex(@"^(?'value'-?[0-9.]+)?$");
		static Dictionary<string, Parser> sParserLUT = new Dictionary<string, Parser>() {
			{ "track-type", parseTrackType },
			{ "track-id", parseTrackId },
			{ "track-arg", parseTrackArg },
		};

		MidiTrackMetaData(MidiTrack track) {
			mTrack = track;
			if (mTrack.Name != null) {
				var matches = sTrackRegex.Matches(mTrack.Name);
				for (var i = 0; i < matches.Count; ++i) {
					var name = matches[i].Groups["key"].Value.ToLowerInvariant();
					var value = matches[i].Groups["value"].Value;
					if (sParserLUT.ContainsKey(name)) {
						sParserLUT[name](this, value);
					}
				}
			}
		}

		public static MidiTrackMetaData fromTrack(MidiTrack track) {
			if (track == null) {
				return null;
			}
			return new MidiTrackMetaData(track);
		}

		static void parseTrackType(MidiTrackMetaData meta, string value) {
			BmsTrackType result;
			if (Enum.TryParse(value, true, out result)) {
				meta.mType = result;
			}
		}
		static void parseTrackId(MidiTrackMetaData meta, string value) {
			int result;
			if (parseInt(value, 0, 15, out result)) {
				meta.mId = result;
			}
		}
		static void parseTrackArg(MidiTrackMetaData meta, string value) {
			int result;
			if (parseInt(value, Int16.MinValue, Int16.MaxValue, out result)) {
				meta.mArg = (short)result;
			}
		}

		static bool parseInt(string str, int min, int max, out int result) {
			result = 0;
			var match = sIntRegex.Match(str);
			if (!match.Success) {
				return false;
			}
			var value = match.Groups["value"].Value;
			var isHex = match.Groups["isHex"].Success;
			var isNeg = match.Groups["isNeg"].Success;
			var style = NumberStyles.None;
			if (isHex) {
				style |= NumberStyles.AllowHexSpecifier;
			}
			if (!Int32.TryParse(value, style, null, out result)) {
				return false;
			}
			if (isNeg) {
				result = -result;
			}
			return result >= min && result <= max;
		}

		delegate void Parser(MidiTrackMetaData meta, string value);
	}

	enum BmsTrackType {
		Unspecified,
		Root,
		Child,
	}
}
