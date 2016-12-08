using arookas.Audio.MusicalInstrumentDigitalInterface;
using arookas.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace arookas {
	class MidiConverter {
		Midi mMidi;
		BmsOptions mOptions;

		BmsWriter mWriter;
		Stack<BmsPoint?> mLoopPoints;
		MidiMetaData mMetaData;
		BmsVoices mVoices;

		CC mBankCC;
		CC mVolCC;
		CC mPanCC;

		MidiConverter() { }

		public static void Convert(Midi midi, BmsOptions options) {
			var conv = new MidiConverter();
			conv.ConvertMidi(midi, options);
		}

		void ConvertMidi(Midi midi, BmsOptions options) {
			// init
			mMidi = midi;
			mOptions = options;
			mLoopPoints = new Stack<BmsPoint?>(10);
			mVoices = new BmsVoices();
			mBankCC = new CC();
			mVolCC = new CC();
			mPanCC = new CC();

			DetectTracks();
			CheckLoopPoints();
			Console.WriteLine("Writing BMS...");
			using (var file = File.Create(options.mOutputFile)) {
				mWriter = new BmsWriter(file);

				// root
				Console.WriteLine("Writing root track...");
				BmsPoint?[] childPoints = new BmsPoint?[16];
				for (var i = 0; i < 16; ++i) {
					if (mMetaData[i] == null) {
						continue;
					}
					childPoints[i] = mWriter.WriteAddChild((byte)i);
				}
				mWriter.WritePpqn((ushort)(midi.Division as TicksPerBeatDivision).TicksPerBeat);
				ConvertTrack(mMetaData.Root);

				// children
				for (var i = 0; i < 16; ++i) {
					if (childPoints[i] == null) {
						continue;
					}
					Console.WriteLine("Writing child track {0}...", i);
					mWriter.ClosePoint(childPoints[i].Value);
					if (options.mAddTrackInit) {
						mWriter.WriteTrackInit(0);
					}
					ConvertTrack(mMetaData[i]);
				}
				Console.WriteLine("Finished writing BMS.");
				Console.WriteLine("Closing file...");
			}
			Console.WriteLine("Closed file.");
		}
		void DetectTracks() {
			Console.WriteLine("Collecting meta data and detecting tracks...");
			mMetaData = new MidiMetaData(mMidi, mOptions.mTrackMode);
			Console.WriteLine("Done.");
		}
		void CheckLoopPoints() {
			Console.WriteLine("Checking loop points...");
			var scope = 0;
			foreach (var track in mMidi) {
				foreach (var e in track.All<ControlChangeEvent>()) {
					switch (e.Controller) {
						case MidiController.EMidiLoopBegin:
						case MidiController.EMidiGlobalLoopBegin: {
							++scope;
							break;
						}
						case MidiController.EMidiLoopEnd:
						case MidiController.EMidiGlobalLoopEnd: {
							--scope;
							break;
						}
					}
					if (scope < 0) {
						MIDItoBMS.error("Loop is missing a start point.");
					}
				}
			}
			if (scope > 0) {
				MIDItoBMS.error("Loop is missing an end point.");
			}
			Console.WriteLine("Loop points seem fine.");
			Console.WriteLine("Detecting global loop points...");
			var loops = mMetaData.Root.MidiTrack.Where<ControlChangeEvent>(IsEMidiGlobalLoopPoint).ToArray();
			if (loops.Length > 0) {
				Console.WriteLine("Global loop points detected.");
				Console.WriteLine("Converting global loop points...");
				foreach (var track in mMidi) {
					track.RemoveAll<ControlChangeEvent>(IsEMidiGlobalLoopPoint);
					track.AddRange(loops.Select(loop => new ControlChangeEvent(loop.Time, loop.ChannelNumber, loop.ControllerNumber - 2, loop.ControllerValue)));
				}
			}
		}
		static bool IsEMidiGlobalLoopPoint(ControlChangeEvent e) {
			switch (e.Controller) {
				case MidiController.EMidiGlobalLoopBegin:
				case MidiController.EMidiGlobalLoopEnd: {
					return true;
				}
			}
			return false;
		}

		// track
		void ConvertTrack(MidiTrackMetaData mTrack) {
			ushort pdur = mOptions.mPerfDuration;
			ulong time = 0;
			foreach (var e in mTrack.MidiTrack) {
				// delta
				mWriter.WriteDelay(e.Time - time);
				time = e.Time;

				switch (e.EventType) {
					case MidiEventType.Channel: {
						var ev = (e as ChannelEvent);
						if (ev.ChannelNumber == mTrack.TrackId) {
							ConvertChannelEvent(ev);
						}
						break;
					}
					case MidiEventType.Meta: {
						ConvertMetaEvent(e as MetaEvent);
						break;
					}
				}
				// silently ignore unsupported events
			}
		}

		// channel
		void ConvertChannelEvent(ChannelEvent e) {
			switch (e.ChannelEventType) {
				case ChannelEventType.NoteOn: ConvertNoteOn(e as NoteOnEvent); break;
				case ChannelEventType.NoteOff: ConvertNoteOff(e as NoteOffEvent); break;
				case ChannelEventType.PitchBend: ConvertPitchBend(e as PitchBendEvent); break;
				case ChannelEventType.ProgramChange: ConvertProgramSelect(e as ProgramChangeEvent); break;
				case ChannelEventType.Controller: ConvertControlChange(e as ControlChangeEvent); break;
			}
			// silently ignore unsupported events
		}
		void ConvertNoteOn(NoteOnEvent e) {
			byte note = (byte)e.NoteNumber;
			if (e.Velocity > 0) {
				mWriter.WriteVoiceOn(note, (byte)(mVoices.Alloc(note) + 1), (byte)e.Velocity);
			}
			else {
				mWriter.WriteVoiceOff((byte)(mVoices.Free(note) + 1));
			}
		}
		void ConvertNoteOff(NoteOffEvent e) {
			mWriter.WriteVoiceOff((byte)(mVoices.Free((byte)e.NoteNumber) + 1));
		}
		void ConvertPitchBend(PitchBendEvent e) {
			mWriter.WritePerf(BmsPerfType.Pitch, (short)((e.PitchBend - 8192) * 4), mOptions.mPerfDuration);
		}
		void ConvertProgramSelect(ProgramChangeEvent e) {
			mWriter.WriteProgramSelect((byte)e.ProgramNumber);
		}
		void ConvertControlChange(ControlChangeEvent e) {
			bool msb = e.Controller.IsMSB();
			switch (e.Controller) {
				case MidiController.BankSelect_MSB:
				case MidiController.BankSelect_LSB: {
					ConvertBankSelect(e, msb);
					break;
				}
				case MidiController.Volume_MSB:
				case MidiController.Volume_LSB: {
					ConvertVolume(e, msb);
					break;
				}
				case MidiController.Pan_MSB:
				case MidiController.Pan_LSB: {
					ConvertPan(e, msb);
					break;
				}
				case MidiController.EMidiLoopBegin: {
					ConvertLoopBegin(e);
					break;
				}
				case MidiController.EMidiLoopEnd: {
					ConvertLoopEnd(e);
					break;
				}
			}
			// silently ignore unsupported events
		}

		// controller
		void ConvertBankSelect(ControlChangeEvent e, bool msb) {
			mBankCC.Set((sbyte)e.ControllerValue, msb);
			mWriter.WriteBankSelect(mBankCC);
		}
		void ConvertVolume(ControlChangeEvent e, bool msb) {
			mVolCC.Set((sbyte)e.ControllerValue, msb);
			mWriter.WritePerf(BmsPerfType.Volume, (short)(mVolCC.Value * 2), mOptions.mPerfDuration);
		}
		void ConvertPan(ControlChangeEvent e, bool msb) {
			mPanCC.Set((sbyte)e.ControllerValue, msb);
			mWriter.WritePerf(BmsPerfType.Pan, (short)(mPanCC.Value * 2), mOptions.mPerfDuration);
		}
		void ConvertLoopBegin(ControlChangeEvent e) {
			if (e.ControllerValue > 0) {
				mWriter.WriteLoopBegin((ushort)e.ControllerValue);
				mLoopPoints.Push(null);
			}
			else {
				mLoopPoints.Push(mWriter.OpenPoint());
			}
		}
		void ConvertLoopEnd(ControlChangeEvent e) {
			BmsPoint? point = mLoopPoints.Pop();
			if (point == null) {
				mWriter.WriteLoopEnd();
			}
			else {
				mWriter.WriteSeekEx(point.Value);
			}
		}

		// meta
		void ConvertMetaEvent(MetaEvent e) {
			switch (e.MetaEventType) {
				case MetaEventType.EndOfTrack: {
					ConvertEOT(e as EndOfTrackEvent);
					break;
				}
				case MetaEventType.TempoChange: {
					ConvertTempo(e as TempoChangeEvent);
					break;
				}
				case MetaEventType.SequencerSpecific: {
					ConvertSequencerSpecific(e as SequencerSpecificEvent);
					break;
				}
			}
			// silently ignore unsupported events
		}
		void ConvertEOT(EndOfTrackEvent e) {
			mWriter.WriteTrackEnd();
		}
		void ConvertTempo(TempoChangeEvent e) {
			mWriter.WriteTempo((ushort)e.BeatsPerMinute);
		}
		void ConvertSequencerSpecific(SequencerSpecificEvent e) {
			if (e.Id == MidiId.Placeholder) {
				mWriter.WriteRaw(e.Data.ToArray());
			}
		}

		// util types
		class BmsVoices {
			byte?[] mVoices;

			public const int cVoiceCount = 7;

			public BmsVoices() {
				mVoices = new byte?[cVoiceCount];
			}

			public int Alloc(byte note) {
				for (var i = 0; i < mVoices.Length; ++i) {
					if (mVoices[i] == null) {
						mVoices[i] = note;
						return i;
					}
				}
				throw new InvalidOperationException("Failed to allocate a voice.");
			}
			public int Free(byte note) {
				for (var i = 0; i < mVoices.Length; ++i) {
					if (mVoices[i] == note) {
						mVoices[i] = null;
						return i;
					}
				}
				throw new InvalidOperationException("Failed to free a voice.");
			}
		}
	}
}
