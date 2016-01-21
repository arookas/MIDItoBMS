using arookas.Audio.MusicalInstrumentDigitalInterface;
using System.Linq;

namespace arookas {
	class MidiMetaData {
		MidiTrackMetaData[] mChildren;

		public MidiTrackMetaData Root { get; private set; }
		public MidiTrackMetaData this[int id] { get { return mChildren[id]; } }

		public MidiMetaData(Midi midi, BmsTrackDetectionMode mode) {
			mChildren = new MidiTrackMetaData[16];
			switch (mode) {
				case BmsTrackDetectionMode.Manual: {
					var metaData = midi.Select(track => MidiTrackMetaData.fromTrack(track));
					Root = metaData.FirstOrDefault(t => t.TrackType == BmsTrackType.Root);
					for (var i = 0; i < 16; ++i) {
						mChildren[i] = metaData.FirstOrDefault(t => t.TrackType == BmsTrackType.Child && t.TrackId == i);
					}
					break;
				}
				case BmsTrackDetectionMode.Auto: {
					Root = MidiTrackMetaData.fromTrack(midi.FirstOrDefault(track => track.All(midiEvent => midiEvent is MetaEvent || midiEvent is ControlChangeEvent)));
					foreach (var track in midi.Where(track => track.Any<NoteOnEvent>())) {
						var id = track.First<ChannelEvent>().ChannelNumber;
						if (mChildren[id] != null) {
							MIDItoBMS.error("More than one track with ID {0}.", id);
						}
						mChildren[id] = MidiTrackMetaData.fromTrack(track);
					}
					break;
				}
			}
			if (Root == null) {
				MIDItoBMS.error("Failed to detect root track.");
			}
		}
	}
}
