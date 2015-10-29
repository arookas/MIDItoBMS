using arookas.Audio.MusicalInstrumentDigitalInterface;
using arookas.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace arookas
{
	class MidiConverter
	{
		Midi midi;
		BmsOptions bOptions;

		BmsWriter bWriter;
		Stack<BmsPoint?> loopPoints;
		MidiMetaData metaData;
		BmsVoices bVoices;

		CC ccBank;
		CC ccVol;
		CC ccPan;

		private MidiConverter() { }

		public static void Convert(Midi midi, BmsOptions bOptions)
		{
			MidiConverter conv = new MidiConverter();
			conv.ConvertMidi(midi, bOptions);
		}

		void ConvertMidi(Midi midi, BmsOptions bOptions)
		{
			// init
			this.midi = midi;
			this.bOptions = bOptions;
			loopPoints = new Stack<BmsPoint?>(10);
			bVoices = new BmsVoices();
			ccBank = new CC();
			ccVol = new CC();
			ccPan = new CC();

			DetectTracks();
			CheckLoopPoints();
			Console.WriteLine("Writing BMS...");
			bWriter = new BmsWriter(File.Create(bOptions.OutputFile));
			// root
			Console.WriteLine("Writing root track...");
			BmsPoint?[] childPoints = new BmsPoint?[16];
			for (int i = 0; i < 16; ++i)
			{
				if (metaData[i] == null)
				{
					continue;
				}
				childPoints[i] = bWriter.WriteAddChild((byte)i);
			}
			bWriter.WritePpqn((ushort)(midi.Division as TicksPerBeatDivision).TicksPerBeat);
			ConvertTrack(metaData.Root);
			// children
			for (int i = 0; i < 16; ++i)
			{
				if (childPoints[i] == null)
				{
					continue;
				}
				Console.WriteLine("Writing child track {0}...", i);
				bWriter.ClosePoint(childPoints[i].Value);
				if (bOptions.AddTrackInit)
				{
					bWriter.WriteTrackInit(0);
				}
				ConvertTrack(metaData[i]);
			}
			Console.WriteLine("Finished writing BMS.");
			Console.WriteLine("Closing file...");
			bWriter.Dispose();
			Console.WriteLine("Closed file.");
		}
		void DetectTracks()
		{
			Console.WriteLine("Collecting meta data and detecting tracks...");
			metaData = new MidiMetaData(midi, bOptions.TrackDetection);
			Console.WriteLine("Done.");
		}
		void CheckLoopPoints()
		{
			Console.WriteLine("Checking loop points...");
			int scope = 0;
			foreach (var track in midi)
			{
				foreach (var e in track.All<ControlChangeEvent>())
				{
					switch (e.Controller)
					{
						case MidiController.EMidiLoopBegin:
						case MidiController.EMidiGlobalLoopBegin:
						{
							++scope;
							break;
						}
						case MidiController.EMidiLoopEnd:
						case MidiController.EMidiGlobalLoopEnd:
						{
							--scope;
							break;
						}
					}
					if (scope < 0)
					{
						MIDItoBMS.Error("Loop is missing a start point.");
					}
				}
			}
			if (scope > 0)
			{
				MIDItoBMS.Error("Loop is missing an end point.");
			}
			Console.WriteLine("Loop points seem fine.");
			Console.WriteLine("Detecting global loop points...");
			var loops = metaData.Root.MidiTrack.Where<ControlChangeEvent>(IsEMidiGlobalLoopPoint).ToArray();
			if (loops.Length > 0)
			{
				Console.WriteLine("Global loop points detected.");
				Console.WriteLine("Converting global loop points...");
				foreach (var track in midi)
				{
					track.RemoveAll<ControlChangeEvent>(IsEMidiGlobalLoopPoint);
					track.AddRange(loops.Select(loop => new ControlChangeEvent(loop.Time, loop.ChannelNumber, loop.ControllerNumber - 2, loop.ControllerValue)));
				}
			}
		}
		static bool IsEMidiGlobalLoopPoint(ControlChangeEvent e)
		{
			switch (e.Controller)
			{
				case MidiController.EMidiGlobalLoopBegin:
				case MidiController.EMidiGlobalLoopEnd:
				{
					return true;
				}
			}
			return false;
		}

		// track
		void ConvertTrack(MidiTrackMetaData mTrack)
		{
			ushort pdur = bOptions.PerfDuration;
			ulong time = 0;
			foreach (var e in mTrack.MidiTrack)
			{
				// delta
				bWriter.WriteDelay(e.Time - time);
				time = e.Time;

				switch (e.EventType)
				{
					case MidiEventType.Channel:
					{
						var ev = (e as ChannelEvent);
						if (ev.ChannelNumber == mTrack.TrackId)
						{
							ConvertChannelEvent(ev);
						}
						break;
					}
					case MidiEventType.Meta: ConvertMetaEvent(e as MetaEvent); break;
				}
				// silently ignore unsupported events
			}
		}

		// channel
		void ConvertChannelEvent(ChannelEvent e)
		{
			switch (e.ChannelEventType)
			{
				case ChannelEventType.NoteOn: ConvertNoteOn(e as NoteOnEvent); break;
				case ChannelEventType.NoteOff: ConvertNoteOff(e as NoteOffEvent); break;
				case ChannelEventType.PitchBend: ConvertPitchBend(e as PitchBendEvent); break;
				case ChannelEventType.ProgramChange: ConvertProgramSelect(e as ProgramChangeEvent); break;
				case ChannelEventType.Controller: ConvertControlChange(e as ControlChangeEvent); break;
			}
			// silently ignore unsupported events
		}
		void ConvertNoteOn(NoteOnEvent e)
		{
			byte note = (byte)e.NoteNumber;
			if (e.Velocity > 0)
			{
				bWriter.WriteVoiceOn(note, (byte)(bVoices.Alloc(note) + 1), (byte)e.Velocity);
			}
			else
			{
				bWriter.WriteVoiceOff((byte)(bVoices.Free(note) + 1));
			}
		}
		void ConvertNoteOff(NoteOffEvent e)
		{
			bWriter.WriteVoiceOff((byte)(bVoices.Free((byte)e.NoteNumber) + 1));
		}
		void ConvertPitchBend(PitchBendEvent e)
		{
			bWriter.WritePerf(BmsPerfType.Pitch, (short)((e.PitchBend - 8192) * 4), bOptions.PerfDuration);
		}
		void ConvertProgramSelect(ProgramChangeEvent e)
		{
			bWriter.WriteProgramSelect((byte)e.ProgramNumber);
		}
		void ConvertControlChange(ControlChangeEvent e)
		{
			bool msb = e.Controller.IsMSB();
			switch (e.Controller)
			{
				case MidiController.BankSelect_MSB:
				case MidiController.BankSelect_LSB:
				{
					ConvertBankSelect(e, msb);
					break;
				}
				case MidiController.Volume_MSB:
				case MidiController.Volume_LSB:
				{
					ConvertVolume(e, msb);
					break;
				}
				case MidiController.Pan_MSB:
				case MidiController.Pan_LSB:
				{
					ConvertPan(e, msb);
					break;
				}
				case MidiController.EMidiLoopBegin: ConvertLoopBegin(e); break;
				case MidiController.EMidiLoopEnd: ConvertLoopEnd(e); break;
			}
			// silently ignore unsupported events
		}

		// controller
		void ConvertBankSelect(ControlChangeEvent e, bool msb)
		{
			ccBank.Set((sbyte)e.ControllerValue, msb);
			bWriter.WriteBankSelect(ccBank);
		}
		void ConvertVolume(ControlChangeEvent e, bool msb)
		{
			ccVol.Set((sbyte)e.ControllerValue, msb);
			bWriter.WritePerf(BmsPerfType.Volume, ccVol, bOptions.PerfDuration);
		}
		void ConvertPan(ControlChangeEvent e, bool msb)
		{
			ccPan.Set((sbyte)e.ControllerValue, msb);
			bWriter.WritePerf(BmsPerfType.Pan, ccPan, bOptions.PerfDuration);
		}
		void ConvertLoopBegin(ControlChangeEvent e)
		{
			if (e.ControllerValue > 0)
			{
				bWriter.WriteLoopBegin((ushort)e.ControllerValue);
				loopPoints.Push(null);
			}
			else
			{
				loopPoints.Push(bWriter.OpenPoint());
			}
		}
		void ConvertLoopEnd(ControlChangeEvent e)
		{
			BmsPoint? point = loopPoints.Pop();
			if (point == null)
			{
				bWriter.WriteLoopEnd();
			}
			else
			{
				bWriter.WriteSeekEx(point.Value);
			}
		}

		// meta
		void ConvertMetaEvent(MetaEvent e)
		{
			switch (e.MetaEventType)
			{
				case MetaEventType.EndOfTrack: ConvertEOT(e as EndOfTrackEvent); break;
				case MetaEventType.TempoChange: ConvertTempo(e as TempoChangeEvent); break;
				case MetaEventType.SequencerSpecific: ConvertSequencerSpecific(e as SequencerSpecificEvent); break;
			}
			// silently ignore unsupported events
		}
		void ConvertEOT(EndOfTrackEvent e)
		{
			bWriter.WriteTrackEnd();
		}
		void ConvertTempo(TempoChangeEvent e)
		{
			bWriter.WriteTempo((ushort)e.BeatsPerMinute);
		}
		void ConvertSequencerSpecific(SequencerSpecificEvent e)
		{
			if (e.Id == MidiId.Placeholder)
			{
				bWriter.WriteRaw(e.Data.ToArray());
			}
		}

		// util types
		class BmsVoices
		{
			public const int VoiceCount = 7;
			byte?[] voices;

			int FreeCount { get { return voices.Count(i => i == null); } }
			bool HasFreeVoice { get { return FreeCount > 0; } }

			public BmsVoices()
			{
				voices = new byte?[VoiceCount];
			}

			public byte Alloc(byte note)
			{
				if (!HasFreeVoice)
				{
					throw new InvalidOperationException("Failed to allocate a voice.");
				}
				int i = voices.IndexOfFirst(j => j == null);
				voices[i] = note;
				return (byte)i;

			}
			public byte Free(byte note)
			{
				int i = voices.IndexOfFirst(j => j == note);
				voices[i] = null;
				return (byte)i;
			}
		}
	}
}
