using arookas.IO.Binary;
using System;
using System.IO;

namespace arookas
{
	class BmsWriter : IDisposable {
		aBinaryWriter mWriter;

		UInt24 Position { get { return (UInt24)mWriter.Position; } }

		public BmsWriter(Stream stream) {
			mWriter = new aBinaryWriter(stream, Endianness.Big);
		}

		public void Dispose() {
			mWriter.Dispose();
		}

		// points
		public BmsPoint OpenPoint() {
			return new BmsPoint(Position);
		}
		public void ClosePoint(BmsPoint point) {
			UInt24 pos = Position;
			mWriter.Keep();
			mWriter.Goto(point.Position);
			mWriter.Write24(pos);
			mWriter.Back();
		}

		// timing
		public void WriteDelay(byte value) {
			mWriter.Write8(0x80);
			mWriter.Write8(value);
		}
		public void WriteDelay(ushort value) {
			if (value > 0xFF) {
				mWriter.Write8(0x88);
				mWriter.Write16(value);
			}
			else {
				WriteDelay((byte)value);
			}
		}
		public void WriteDelay(UInt24 value) {
			if (value > 0xFFFF) {
				mWriter.Write8(0xEA); // CF is not supported
				mWriter.Write24(value);
			}
			else {
				WriteDelay((ushort)value);
			}
		}
		public void WriteDelay(ulong value) {
			ulong amt;
			while (value > 0) {
				amt = System.Math.Min(value, UInt24.MaxValue);
				WriteDelay((UInt24)amt);
				value -= amt;
			}
		}

		// voices
		public void WriteVoiceOn(byte note, byte voiceId, byte velocity) {
			mWriter.Write8(note);
			mWriter.Write8(voiceId);
			mWriter.Write8(velocity);
		}
		public void WriteVoiceOff(byte voiceId) {
			mWriter.Write8((byte)(0x80 + voiceId));
		}
		public void WriteVoiceOff(byte voiceId, byte unk) {
			mWriter.Write8((byte)(0x88 + voiceId));
			mWriter.Write8(unk);
		}

		// performance
		public void WritePerf(BmsPerfType type, byte value) {
			mWriter.Write8(0x94);
			mWriter.Write8((byte)type);
			mWriter.Write8(value);
		}
		public void WritePerf(BmsPerfType type, byte value, byte duration) {
			if (duration > 0) {
				mWriter.Write8(0x96);
				mWriter.Write8((byte)type);
				mWriter.Write8(value);
				mWriter.Write8(duration);
			}
			else {
				WritePerf(type, value);
			}
		}
		public void WritePerf(BmsPerfType type, byte value, ushort duration) {
			if (duration > 0xFF) {
				mWriter.Write8(0x97);
				mWriter.Write8((byte)type);
				mWriter.Write8(value);
				mWriter.Write16(duration);
			}
			else {
				WritePerf(type, value, (byte)duration);
			}
		}

		public void WritePerf(BmsPerfType type, sbyte value) {
			mWriter.Write8(0x98);
			mWriter.Write8((byte)type);
			mWriter.WriteS8(value);
		}
		public void WritePerf(BmsPerfType type, sbyte value, byte duration) {
			if (duration > 0) {
				mWriter.Write8(0x9A);
				mWriter.Write8((byte)type);
				mWriter.WriteS8(value);
				mWriter.Write8(duration);
			}
			else {
				WritePerf(type, value);
			}
		}
		public void WritePerf(BmsPerfType type, sbyte value, ushort duration) {
			if (duration > 0xFF) {
				mWriter.Write8(0x9B);
				mWriter.Write8((byte)type);
				mWriter.WriteS8(value);
				mWriter.Write16(duration);
			}
			else {
				WritePerf(type, value, (byte)duration);
			}
		}

		public void WritePerf(BmsPerfType type, short value) {
			mWriter.Write8(0x9C);
			mWriter.Write8((byte)type);
			mWriter.WriteS16(value);
		}
		public void WritePerf(BmsPerfType type, short value, byte duration) {
			if (duration > 0) {
				mWriter.Write8(0x9E);
				mWriter.Write8((byte)type);
				mWriter.WriteS16(value);
				mWriter.Write8(duration);
			}
			else {
				WritePerf(type, value);
			}
		}
		public void WritePerf(BmsPerfType type, short value, ushort duration) {
			if (duration > 0xFF) {
				mWriter.Write8(0x9F);
				mWriter.Write8((byte)type);
				mWriter.WriteS16(value);
				mWriter.Write16(duration);
			}
			else {
				WritePerf(type, value, (byte)duration);
			}
		}

		// bank/program
		public void WriteBankSelect(byte bank) {
			mWriter.Write8(0xA4);
			mWriter.Write8(0x20);
			mWriter.Write8(bank);
		}
		public void WriteBankSelect(ushort bank) {
			mWriter.Write8(0xAC);
			mWriter.Write8(0x20);
			mWriter.Write16(bank);
		}
		public void WriteProgramSelect(byte program) {
			mWriter.Write8(0xA4);
			mWriter.Write8(0x21);
			mWriter.Write8(program);
		}
		public void WriteProgramSelect(ushort program) {
			mWriter.Write8(0xAC);
			mWriter.Write8(0x21);
			mWriter.Write16(program);
		}

		// tracks
		public BmsPoint WriteAddChild(byte id) {
			mWriter.Write8(0xC1);
			mWriter.Write8(id);
			BmsPoint point = OpenPoint();
			mWriter.Write24(0); // dummy value
			return point;
		}
		public BmsPoint WriteAddSibling(byte id) {
			mWriter.Write8(0xC2);
			mWriter.Write8(id);
			BmsPoint point = OpenPoint();
			mWriter.Write24(0); // dummy value
			return point;
		}
		public void WriteAddChild(byte id, BmsPoint point) {
			mWriter.Write8(0xC1);
			mWriter.Write8(id);
			mWriter.Write24(point.Position);
		}
		public void WriteAddSibling(byte id, BmsPoint point) {
			mWriter.Write8(0xC2);
			mWriter.Write8(id);
			mWriter.Write24(point.Position);
		}
		public void WriteStopChild(byte idx) {
			mWriter.Write8(0xDA);
			mWriter.Write8(idx);
		}

		public void WriteTrackInit(short arg) {
			mWriter.Write8(0xE7);
			mWriter.WriteS16(arg);
		}

		// seeking
		public BmsPoint WriteSeek() {
			return WriteSeek(BmsSeekMode.Always);
		}
		public BmsPoint WriteSeekEx() {
			return WriteSeekEx(BmsSeekMode.Always);
		}
		public BmsPoint WriteSeek(BmsSeekMode mode) {
			mWriter.Write8(0xC4);
			mWriter.Write8((byte)mode);
			BmsPoint point = OpenPoint();
			mWriter.Write24(0); // dummy
			return point;
		}
		public BmsPoint WriteSeekEx(BmsSeekMode mode) {
			mWriter.Write8(0xC8);
			mWriter.Write8((byte)mode);
			BmsPoint point = OpenPoint();
			mWriter.Write24(0); // dummy
			return point;
		}

		public void WriteSeek(BmsPoint point) {
			WriteSeek(BmsSeekMode.Always, point);
		}
		public void WriteSeekEx(BmsPoint point) {
			WriteSeekEx(BmsSeekMode.Always, point);
		}
		public void WriteSeek(BmsSeekMode mode, BmsPoint point) {
			mWriter.Write8(0xC4);
			mWriter.Write8((byte)mode);
			mWriter.Write24(point.Position);
		}
		public void WriteSeekEx(BmsSeekMode mode, BmsPoint point) {
			mWriter.Write8(0xC8);
			mWriter.Write8((byte)mode);
			mWriter.Write24(point.Position);
		}

		public void WriteBack() {
			WriteBack(BmsSeekMode.Always);
		}
		public void WriteBack(BmsSeekMode mode) {
			mWriter.Write8(0xC6);
			mWriter.Write8((byte)mode);
		}

		// looping
		public void WriteLoopBegin(ushort count) {
			mWriter.Write8(0xC9);
			mWriter.Write16(count);
		}
		public void WriteLoopEnd() {
			mWriter.Write8(0xCA);
		}

		// dynamics
		public BmsPoint WriteSetDynamic(byte idx) {
			mWriter.Write8(0xDF);
			mWriter.Write8(idx);
			BmsPoint point = OpenPoint();
			mWriter.Write24(0); // dummy
			return point;
		}
		public void WriteSetDynamic(byte idx, BmsPoint point) {
			mWriter.Write8(0xDF);
			mWriter.Write8(idx);
			mWriter.Write24(point.Position);
		}

		public void WriteUnsetDynamic(byte idx) {
			mWriter.Write8(0xE0);
			mWriter.Write8(idx);
		}
		public void WriteClearDynamic() {
			mWriter.Write8(0xE1);
		}
		// TODO: finish dynamics

		// tempo/ppqn
		public void WriteTempo(ushort tempo) {
			mWriter.Write8(0xFD);
			mWriter.Write16(tempo);
		}
		public void WritePpqn(ushort ppqn) {
			mWriter.Write8(0xFE);
			mWriter.Write16(ppqn);
		}

		// eot
		public void WriteTrackEnd() {
			mWriter.Write8(0xFF);
		}

		// misc
		public void WritePrevNote(byte prevNote) {
			mWriter.Write8(0xD4);
			mWriter.Write8(prevNote);
		}
		public void WriteTranspose(sbyte transpose) {
			mWriter.Write8(0xD9);
			mWriter.WriteS8(transpose);
		}
		public void WriteFlags(byte flags) {
			mWriter.Write8(0xDE);
			mWriter.Write8(flags);
		}

		public void WriteAddPool() {
			mWriter.Write8(0xE5);
		}
		public void WriteRemovePool() {
			mWriter.Write8(0xE6);
		}

		public void WriteRaw(byte[] data) {
			mWriter.Write8s(data);
		}

	}

	enum BmsPerfType : byte {
		Volume = 0,
		Pitch = 1,
		Pan = 3,
	}

	enum BmsSeekMode : byte {
		Always,
		Zero,
		NonZero,
		One,
		GreaterThan,
		LessThan,
	}

	struct BmsPoint {
		public UInt24 Position { get; private set; }

		public BmsPoint(UInt24 position)
			: this() {
			Position = position;
		}
	}
}
