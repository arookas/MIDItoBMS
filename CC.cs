using System;

namespace arookas
{
	class CC
	{
		public byte? LSB { get; private set; }
		public byte? MSB { get; private set; }
		public short Value
		{
			get
			{
				if (MSB == null)
				{
					throw new InvalidCastException("CC does not have a complete value.");
				}
				return (short)((MSB.Value << 7) | LSB.Value);
			}
		}

		public CC()
		{
			LSB = null;
			MSB = null;
		}
		public CC(sbyte value, bool msb) { Set(value, msb); }
		public CC(sbyte lsb, sbyte msb) { Set(lsb, msb); }
		public CC(short value) { Set(value); }
		public CC(byte value, bool msb) { Set(value, msb); }
		public CC(byte lsb, byte msb) { Set(lsb, msb); }
		public CC(ushort value) { Set(value); }

		public void Set(sbyte value, bool msb)
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (msb)
			{
				MSB = (byte)value;
				LSB = 0;
			}
			else
			{
				LSB = (byte)value;
			}
		}
		public void Set(sbyte lsb, sbyte msb)
		{
			if (lsb < 0)
			{
				throw new ArgumentOutOfRangeException("lsb");
			}
			if (msb < 0)
			{
				throw new ArgumentOutOfRangeException("msb");
			}
			LSB = (byte)lsb;
			MSB = (byte)msb;
		}
		public void Set(short value)
		{
			if (value < 0 || value > 0x3FFF)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			LSB = (byte)((value >> 0) & 0x7F);
			MSB = (byte)((value >> 7) & 0x7F);
		}
		public void Set(byte value, bool msb) { Set((sbyte)value, msb); }
		public void Set(byte lsb, byte msb) { Set((sbyte)lsb, (sbyte)msb); }
		public void Set(ushort value) { Set((short)value); }

		public static implicit operator short(CC cc)
		{
			if (cc != null)
			{
				return cc.Value;
			}
			return 0;
		}
		public static implicit operator CC(short value)
		{
			return new CC(value);
		}
		public static implicit operator ushort(CC cc)
		{
			if (cc != null)
			{
				return (ushort)cc.Value;
			}
			return 0;
		}
		public static implicit operator CC(ushort value)
		{
			return new CC(value);
		}

		public static int operator +(CC lhs, CC rhs)
		{
			return (short)lhs + (short)rhs;
		}
		public static int operator -(CC lhs, CC rhs)
		{
			return (short)lhs - (short)rhs;
		}
		public static int operator *(CC lhs, CC rhs)
		{
			return (short)lhs * (short)rhs;
		}
		public static int operator /(CC lhs, CC rhs)
		{
			return (short)lhs / (short)rhs;
		}
	}
}
