using arookas.Audio.MusicalInstrumentDigitalInterface;
using System.Linq;

namespace arookas
{
	class MidiMetaData
	{
		MidiTrackMetaData[] children;

		public MidiTrackMetaData Root { get; private set; }
		public MidiTrackMetaData this[int id] { get { return children[id]; } }

		public MidiMetaData(Midi midi, BmsTrackDetectionMode mode)
		{
			children = new MidiTrackMetaData[16];
			switch (mode)
			{
				case BmsTrackDetectionMode.Manual:
				{
					var metaData = midi.Select(track => MidiTrackMetaData.FromTrack(track));
					Root = metaData.FirstOrDefault(t => t.TrackType == BmsTrackType.Root);
					for (int i = 0; i < 16; ++i)
					{
						children[i] = metaData.FirstOrDefault(t => t.TrackType == BmsTrackType.Child && t.TrackId == i);
					}
					break;
				}
				case BmsTrackDetectionMode.Auto:
				{
					Root = MidiTrackMetaData.FromTrack(midi.FirstOrDefault(track => track.All(midiEvent => midiEvent is MetaEvent || midiEvent is ControlChangeEvent)));
					foreach (var track in midi.Where(track => track.Any<NoteOnEvent>()))
					{
						int id = track.First<ChannelEvent>().ChannelNumber;
						if (children[id] != null)
						{
							MIDItoBMS.Error("More than one track with ID {0}.", id);
						}
						children[id] = MidiTrackMetaData.FromTrack(track);
					}
					break;
				}
			}
			if (Root == null)
			{
				MIDItoBMS.Error("Failed to detect root track.");
			}
		}
	}
}
