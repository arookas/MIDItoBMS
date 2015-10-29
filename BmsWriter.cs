using arookas.IO.Binary;
using System;
using System.IO;

namespace arookas
{
	class BmsWriter : IDisposable
	{
		aBinaryWriter writer;

		UInt24 Position { get { return (UInt24)writer.Position; } }

		// constructors
		public BmsWriter(Stream stream)
		{
			writer = new aBinaryWriter(stream, Endianness.Big);
		}

		public void Dispose() { writer.Dispose(); }

		// points
		public BmsPoint OpenPoint() { return new BmsPoint(Position); }
		public void ClosePoint(BmsPoint point)
		{
			UInt24 pos = Position;
			writer.Keep();
			writer.Goto(point.Position);
			writer.Write24(pos);
			writer.Back();
		}

		// timing
		public void WriteDelay(byte value)
		{
			writer.Write8(0x80);
			writer.Write8(value);
		}
		public void WriteDelay(ushort value)
		{
			if (value > 0xFF)
			{
				writer.Write8(0x88);
				writer.Write16(value);
			}
			else
			{
				WriteDelay((byte)value);
			}
		}
		public void WriteDelay(UInt24 value)
		{
			if (value > 0xFFFF)
			{
				writer.Write8(0xEA); // CF is not supported
				writer.Write24(value);
			}
			else
			{
				WriteDelay((ushort)value);
			}
		}
		public void WriteDelay(ulong value)
		{
			ulong amt;
			while (value > 0)
			{
				amt = System.Math.Min(value, UInt24.MaxValue);
				WriteDelay((UInt24)amt);
				value -= amt;
			}
		}

		// voices
		public void WriteVoiceOn(byte note, byte voiceId, byte velocity)
		{
			writer.Write8(note);
			writer.Write8(voiceId);
			writer.Write8(velocity);
		}
		public void WriteVoiceOff(byte voiceId)
		{
			writer.Write8((byte)(0x80 + voiceId));
		}
		public void WriteVoiceOff(byte voiceId, byte unk)
		{
			writer.Write8((byte)(0x88 + voiceId));
			writer.Write8(unk);
		}

		// performance
		public void WritePerf(BmsPerfType type, byte value)
		{
			writer.Write8(0x94);
			writer.Write8((byte)type);
			writer.Write8(value);
		}
		public void WritePerf(BmsPerfType type, byte value, byte duration)
		{
			if (duration > 0)
			{
				writer.Write8(0x96);
				writer.Write8((byte)type);
				writer.Write8(value);
				writer.Write8(duration);
			}
			else
			{
				WritePerf(type, value);
			}
		}
		public void WritePerf(BmsPerfType type, byte value, ushort duration)
		{
			if (duration > 0xFF)
			{
				writer.Write8(0x97);
				writer.Write8((byte)type);
				writer.Write8(value);
				writer.Write16(duration);
			}
			else
			{
				WritePerf(type, value, (byte)duration);
			}
		}

		public void WritePerf(BmsPerfType type, sbyte value)
		{
			writer.Write8(0x98);
			writer.Write8((byte)type);
			writer.WriteS8(value);
		}
		public void WritePerf(BmsPerfType type, sbyte value, byte duration)
		{
			if (duration > 0)
			{
				writer.Write8(0x9A);
				writer.Write8((byte)type);
				writer.WriteS8(value);
				writer.Write8(duration);
			}
			else
			{
				WritePerf(type, value);
			}
		}
		public void WritePerf(BmsPerfType type, sbyte value, ushort duration)
		{
			if (duration > 0xFF)
			{
				writer.Write8(0x9B);
				writer.Write8((byte)type);
				writer.WriteS8(value);
				writer.Write16(duration);
			}
			else
			{
				WritePerf(type, value, (byte)duration);
			}
		}

		public void WritePerf(BmsPerfType type, short value)
		{
			writer.Write8(0x9C);
			writer.Write8((byte)type);
			writer.WriteS16(value);
		}
		public void WritePerf(BmsPerfType type, short value, byte duration)
		{
			if (duration > 0)
			{
				writer.Write8(0x9E);
				writer.Write8((byte)type);
				writer.WriteS16(value);
				writer.Write8(duration);
			}
			else
			{
				WritePerf(type, value);
			}
		}
		public void WritePerf(BmsPerfType type, short value, ushort duration)
		{
			if (duration > 0xFF)
			{
				writer.Write8(0x9F);
				writer.Write8((byte)type);
				writer.WriteS16(value);
				writer.Write16(duration);
			}
			else
			{
				WritePerf(type, value, (byte)duration);
			}
		}

		// bank/program
		public void WriteBankSelect(byte bank)
		{
			writer.Write8(0xA4);
			writer.Write8(0x20);
			writer.Write8(bank);
		}
		public void WriteBankSelect(ushort bank)
		{
			writer.Write8(0xAC);
			writer.Write8(0x20);
			writer.Write16(bank);
		}
		public void WriteProgramSelect(byte program)
		{
			writer.Write8(0xA4);
			writer.Write8(0x21);
			writer.Write8(program);
		}
		public void WriteProgramSelect(ushort program)
		{
			writer.Write8(0xAC);
			writer.Write8(0x21);
			writer.Write16(program);
		}

		// tracks
		public BmsPoint WriteAddChild(byte id)
		{
			writer.Write8(0xC1);
			writer.Write8(id);
			BmsPoint point = OpenPoint();
			writer.Write24(0); // dummy value
			return point;
		}
		public BmsPoint WriteAddSibling(byte id)
		{
			writer.Write8(0xC2);
			writer.Write8(id);
			BmsPoint point = OpenPoint();
			writer.Write24(0); // dummy value
			return point;
		}
		public void WriteAddChild(byte id, BmsPoint point)
		{
			writer.Write8(0xC1);
			writer.Write8(id);
			writer.Write24(point.Position);
		}
		public void WriteAddSibling(byte id, BmsPoint point)
		{
			writer.Write8(0xC2);
			writer.Write8(id);
			writer.Write24(point.Position);
		}
		public void WriteStopChild(byte idx)
		{
			writer.Write8(0xDA);
			writer.Write8(idx);
		}

		public void WriteTrackInit(short arg)
		{
			writer.Write8(0xE7);
			writer.WriteS16(arg);
		}

		// seeking
		public BmsPoint WriteSeek() { return WriteSeek(BmsSeekMode.Always); }
		public BmsPoint WriteSeekEx() { return WriteSeekEx(BmsSeekMode.Always); }
		public BmsPoint WriteSeek(BmsSeekMode mode)
		{
			writer.Write8(0xC4);
			writer.Write8((byte)mode);
			BmsPoint point = OpenPoint();
			writer.Write24(0); // dummy
			return point;
		}
		public BmsPoint WriteSeekEx(BmsSeekMode mode)
		{
			writer.Write8(0xC8);
			writer.Write8((byte)mode);
			BmsPoint point = OpenPoint();
			writer.Write24(0); // dummy
			return point;
		}

		public void WriteSeek(BmsPoint point) { WriteSeek(BmsSeekMode.Always, point); }
		public void WriteSeekEx(BmsPoint point) { WriteSeekEx(BmsSeekMode.Always, point); }
		public void WriteSeek(BmsSeekMode mode, BmsPoint point)
		{
			writer.Write8(0xC4);
			writer.Write8((byte)mode);
			writer.Write24(point.Position);
		}
		public void WriteSeekEx(BmsSeekMode mode, BmsPoint point)
		{
			writer.Write8(0xC8);
			writer.Write8((byte)mode);
			writer.Write24(point.Position);
		}

		public void WriteBack() { WriteBack(BmsSeekMode.Always); }
		public void WriteBack(BmsSeekMode mode)
		{
			writer.Write8(0xC6);
			writer.Write8((byte)mode);
		}

		// looping
		public void WriteLoopBegin(ushort count)
		{
			writer.Write8(0xC9);
			writer.Write16(count);
		}
		public void WriteLoopEnd()
		{
			writer.Write8(0xCA);
		}

		// dynamics
		public BmsPoint WriteSetDynamic(byte idx)
		{
			writer.Write8(0xDF);
			writer.Write8(idx);
			BmsPoint point = OpenPoint();
			writer.Write24(0); // dummy
			return point;
		}
		public void WriteSetDynamic(byte idx, BmsPoint point)
		{
			writer.Write8(0xDF);
			writer.Write8(idx);
			writer.Write24(point.Position);
		}

		public void WriteUnsetDynamic(byte idx)
		{
			writer.Write8(0xE0);
			writer.Write8(idx);
		}
		public void WriteClearDynamic() { writer.Write8(0xE1); }
		// TODO: finish dynamics

		// tempo/ppqn
		public void WriteTempo(ushort tempo)
		{
			writer.Write8(0xFD);
			writer.Write16(tempo);
		}
		public void WritePpqn(ushort ppqn)
		{
			writer.Write8(0xFE);
			writer.Write16(ppqn);
		}

		// eot
		public void WriteTrackEnd()
		{
			writer.Write8(0xFF);
		}

		// misc
		public void WritePrevNote(byte prevNote)
		{
			writer.Write8(0xD4);
			writer.Write8(prevNote);
		}
		public void WriteTranspose(sbyte transpose)
		{
			writer.Write8(0xD9);
			writer.WriteS8(transpose);
		}
		public void WriteFlags(byte flags)
		{
			writer.Write8(0xDE);
			writer.Write8(flags);
		}

		public void WriteAddPool() { writer.Write8(0xE5); }
		public void WriteRemovePool() { writer.Write8(0xE6); }

		public void WriteRaw(byte[] data) { writer.Write8s(data); }

	}

	enum BmsPerfType : byte
	{
		Volume = 0,
		Pitch = 1,
		Pan = 3,
	}

	enum BmsSeekMode : byte
	{
		Always,
		Zero,
		NonZero,
		One,
		GreaterThan,
		LessThan,
	}

	struct BmsPoint
	{
		public UInt24 Position { get; private set; }

		public BmsPoint(UInt24 position)
			: this()
		{
			Position = position;
		}
	}
}
