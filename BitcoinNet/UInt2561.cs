﻿using System;
using System.Linq;
using BitcoinNet.DataEncoders;

namespace BitcoinNet
{
	public class uint256
	{
		private const int WIDTH_BYTE = 256 / 8;

		private static readonly HexEncoder Encoder = new HexEncoder();
		internal readonly uint pn0;
		internal readonly uint pn1;
		internal readonly uint pn2;
		internal readonly uint pn3;
		internal readonly uint pn4;
		internal readonly uint pn5;
		internal readonly uint pn6;
		internal readonly uint pn7;

		public uint256()
		{
		}

		public uint256(uint256 b)
		{
			pn0 = b.pn0;
			pn1 = b.pn1;
			pn2 = b.pn2;
			pn3 = b.pn3;
			pn4 = b.pn4;
			pn5 = b.pn5;
			pn6 = b.pn6;
			pn7 = b.pn7;
		}

		public uint256(ulong b)
		{
			pn0 = (uint) b;
			pn1 = (uint) (b >> 32);
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
			pn5 = 0;
			pn6 = 0;
			pn7 = 0;
		}

		public uint256(byte[] vch, bool lendian = true) : this(vch, 0, vch.Length, lendian)
		{
		}

		public uint256(byte[] vch, int offset, int length, bool lendian = true)
		{
			if (vch.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 32 bytes long");
			}

			if (!lendian)
			{
				if (length != vch.Length)
				{
					vch = vch.Take(32).ToArray();
				}

				vch = vch.Reverse().ToArray();
			}

			pn0 = Utils.ToUInt32(vch, offset + 4 * 0, true);
			pn1 = Utils.ToUInt32(vch, offset + 4 * 1, true);
			pn2 = Utils.ToUInt32(vch, offset + 4 * 2, true);
			pn3 = Utils.ToUInt32(vch, offset + 4 * 3, true);
			pn4 = Utils.ToUInt32(vch, offset + 4 * 4, true);
			pn5 = Utils.ToUInt32(vch, offset + 4 * 5, true);
			pn6 = Utils.ToUInt32(vch, offset + 4 * 6, true);
			pn7 = Utils.ToUInt32(vch, offset + 4 * 7, true);
		}

		public uint256(ReadOnlySpan<byte> bytes)
		{
			if (bytes.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 32 bytes long");
			}

			pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
			pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
			pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
			pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
			pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
			pn5 = Utils.ToUInt32(bytes, 4 * 5, true);
			pn6 = Utils.ToUInt32(bytes, 4 * 6, true);
			pn7 = Utils.ToUInt32(bytes, 4 * 7, true);
		}

		public uint256(string str)
		{
			pn0 = 0;
			pn1 = 0;
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
			pn5 = 0;
			pn6 = 0;
			pn7 = 0;
			str = str.Trim();

			if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				str = str.Substring(2);
			}

			var bytes = Encoder.DecodeData(str);
			Array.Reverse(bytes);
			if (bytes.Length != WIDTH_BYTE)
			{
				throw new FormatException("Invalid hex length");
			}

			pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
			pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
			pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
			pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
			pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
			pn5 = Utils.ToUInt32(bytes, 4 * 5, true);
			pn6 = Utils.ToUInt32(bytes, 4 * 6, true);
			pn7 = Utils.ToUInt32(bytes, 4 * 7, true);
		}

		public uint256(byte[] vch)
			: this(vch, true)
		{
		}

		public static uint256 Zero { get; } = new uint256();

		public static uint256 One { get; } = new uint256(1);

		public int Size => WIDTH_BYTE;

		public static uint256 Parse(string hex)
		{
			return new uint256(hex);
		}

		public static bool TryParse(string hex, out uint256 result)
		{
			if (hex == null)
			{
				throw new ArgumentNullException(nameof(hex));
			}

			if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				hex = hex.Substring(2);
			}

			result = null;
			if (hex.Length != WIDTH_BYTE * 2)
			{
				return false;
			}

			if (!((HexEncoder) Encoders.Hex).IsValid(hex))
			{
				return false;
			}

			result = new uint256(hex);
			return true;
		}

		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			uint value;
			switch (uintIndex)
			{
				case 0:
					value = pn0;
					break;
				case 1:
					value = pn1;
					break;
				case 2:
					value = pn2;
					break;
				case 3:
					value = pn3;
					break;
				case 4:
					value = pn4;
					break;
				case 5:
					value = pn5;
					break;
				case 6:
					value = pn6;
					break;
				case 7:
					value = pn7;
					break;
				default:
					throw new ArgumentOutOfRangeException("index");
			}

			return (byte) (value >> (byteIndex * 8));
		}

		public override string ToString()
		{
			var bytes = ToBytes();
			Array.Reverse(bytes);
			return Encoder.EncodeData(bytes);
		}

		public override bool Equals(object obj)
		{
			var item = obj as uint256;
			if (item == null)
			{
				return false;
			}

			var equals = true;
			equals &= pn0 == item.pn0;
			equals &= pn1 == item.pn1;
			equals &= pn2 == item.pn2;
			equals &= pn3 == item.pn3;
			equals &= pn4 == item.pn4;
			equals &= pn5 == item.pn5;
			equals &= pn6 == item.pn6;
			equals &= pn7 == item.pn7;
			return equals;
		}

		public static bool operator ==(uint256 a, uint256 b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if ((object) a == null || (object) b == null)
			{
				return false;
			}

			var equals = true;
			equals &= a.pn0 == b.pn0;
			equals &= a.pn1 == b.pn1;
			equals &= a.pn2 == b.pn2;
			equals &= a.pn3 == b.pn3;
			equals &= a.pn4 == b.pn4;
			equals &= a.pn5 == b.pn5;
			equals &= a.pn6 == b.pn6;
			equals &= a.pn7 == b.pn7;
			return equals;
		}

		public static bool operator <(uint256 a, uint256 b)
		{
			return Comparison(a, b) < 0;
		}

		public static bool operator >(uint256 a, uint256 b)
		{
			return Comparison(a, b) > 0;
		}

		public static bool operator <=(uint256 a, uint256 b)
		{
			return Comparison(a, b) <= 0;
		}

		public static bool operator >=(uint256 a, uint256 b)
		{
			return Comparison(a, b) >= 0;
		}

		private static int Comparison(uint256 a, uint256 b)
		{
			if (a.pn7 < b.pn7)
			{
				return -1;
			}

			if (a.pn7 > b.pn7)
			{
				return 1;
			}

			if (a.pn6 < b.pn6)
			{
				return -1;
			}

			if (a.pn6 > b.pn6)
			{
				return 1;
			}

			if (a.pn5 < b.pn5)
			{
				return -1;
			}

			if (a.pn5 > b.pn5)
			{
				return 1;
			}

			if (a.pn4 < b.pn4)
			{
				return -1;
			}

			if (a.pn4 > b.pn4)
			{
				return 1;
			}

			if (a.pn3 < b.pn3)
			{
				return -1;
			}

			if (a.pn3 > b.pn3)
			{
				return 1;
			}

			if (a.pn2 < b.pn2)
			{
				return -1;
			}

			if (a.pn2 > b.pn2)
			{
				return 1;
			}

			if (a.pn1 < b.pn1)
			{
				return -1;
			}

			if (a.pn1 > b.pn1)
			{
				return 1;
			}

			if (a.pn0 < b.pn0)
			{
				return -1;
			}

			if (a.pn0 > b.pn0)
			{
				return 1;
			}

			return 0;
		}

		public static bool operator !=(uint256 a, uint256 b)
		{
			return !(a == b);
		}

		public static bool operator ==(uint256 a, ulong b)
		{
			return a == new uint256(b);
		}

		public static bool operator !=(uint256 a, ulong b)
		{
			return !(a == new uint256(b));
		}

		public static implicit operator uint256(ulong value)
		{
			return new uint256(value);
		}


		public byte[] ToBytes(bool lendian = true)
		{
			var arr = new byte[WIDTH_BYTE];
			ToBytes(arr);
			if (!lendian)
			{
				Array.Reverse(arr);
			}

			return arr;
		}

		public void ToBytes(byte[] output)
		{
			Buffer.BlockCopy(Utils.ToBytes(pn0, true), 0, output, 4 * 0, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn1, true), 0, output, 4 * 1, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn2, true), 0, output, 4 * 2, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn3, true), 0, output, 4 * 3, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn4, true), 0, output, 4 * 4, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn5, true), 0, output, 4 * 5, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn6, true), 0, output, 4 * 6, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn7, true), 0, output, 4 * 7, 4);
		}

		public void ToBytes(Span<byte> output, bool lendian = true)
		{
			if (output.Length < WIDTH_BYTE)
			{
				throw new ArgumentException($"The array should be at least of size {WIDTH_BYTE}", nameof(output));
			}

			var initial = output;
			Utils.ToBytes(pn0, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn1, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn2, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn3, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn4, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn5, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn6, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn7, true, output);

			if (!lendian)
			{
				initial.Reverse();
			}
		}

		public MutableUint256 AsBitcoinSerializable()
		{
			return new MutableUint256(this);
		}

		public int GetSerializeSize(int nType = 0, uint? protocolVersion = null)
		{
			return WIDTH_BYTE;
		}

		public ulong GetLow64()
		{
			return pn0 | ((ulong) pn1 << 32);
		}

		public uint GetLow32()
		{
			return pn0;
		}

		public override int GetHashCode()
		{
			var hash = 17;
			unchecked
			{
				hash = hash * 31 + (int) pn0;
				hash = hash * 31 + (int) pn1;
				hash = hash * 31 + (int) pn2;
				hash = hash * 31 + (int) pn3;
				hash = hash * 31 + (int) pn4;
				hash = hash * 31 + (int) pn5;
				hash = hash * 31 + (int) pn6;
				hash = hash * 31 + (int) pn7;
			}

			return hash;
		}

		public class MutableUint256 : IBitcoinSerializable
		{
			public MutableUint256()
			{
				Value = Zero;
			}

			public MutableUint256(uint256 value)
			{
				Value = value;
			}

			public uint256 Value { get; set; }

			public void ReadWrite(BitcoinStream stream)
			{
				if (stream.Serializing)
				{
					Span<byte> b = stackalloc byte[WIDTH_BYTE];
					Value.ToBytes(b);
					stream.ReadWrite(ref b);
				}
				else
				{
					Span<byte> b = stackalloc byte[WIDTH_BYTE];
					stream.ReadWrite(ref b);
					Value = new uint256(b);
				}
			}
		}
	}

	public class uint160
	{
		private const int WIDTH_BYTE = 160 / 8;

		private static readonly HexEncoder Encoder = new HexEncoder();
		internal readonly uint pn0;
		internal readonly uint pn1;
		internal readonly uint pn2;
		internal readonly uint pn3;
		internal readonly uint pn4;

		public uint160()
		{
		}

		public uint160(uint160 b)
		{
			pn0 = b.pn0;
			pn1 = b.pn1;
			pn2 = b.pn2;
			pn3 = b.pn3;
			pn4 = b.pn4;
		}

		public uint160(ulong b)
		{
			pn0 = (uint) b;
			pn1 = (uint) (b >> 32);
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
		}

		public uint160(byte[] vch, bool lendian = true)
		{
			if (vch.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 20 bytes long");
			}

			if (!lendian)
			{
				vch = vch.Reverse().ToArray();
			}

			pn0 = Utils.ToUInt32(vch, 4 * 0, true);
			pn1 = Utils.ToUInt32(vch, 4 * 1, true);
			pn2 = Utils.ToUInt32(vch, 4 * 2, true);
			pn3 = Utils.ToUInt32(vch, 4 * 3, true);
			pn4 = Utils.ToUInt32(vch, 4 * 4, true);
		}

		public uint160(string str)
		{
			pn0 = 0;
			pn1 = 0;
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
			str = str.Trim();

			if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				str = str.Substring(2);
			}

			var bytes = Encoder.DecodeData(str).Reverse().ToArray();
			if (bytes.Length != WIDTH_BYTE)
			{
				throw new FormatException("Invalid hex length");
			}

			pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
			pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
			pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
			pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
			pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
		}

		public uint160(byte[] vch)
			: this(vch, true)
		{
		}

		public static uint160 Zero { get; } = new uint160();

		public static uint160 One { get; } = new uint160(1);

		public int Size => WIDTH_BYTE;

		public static uint160 Parse(string hex)
		{
			return new uint160(hex);
		}

		public static bool TryParse(string hex, out uint160 result)
		{
			if (hex == null)
			{
				throw new ArgumentNullException(nameof(hex));
			}

			if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				hex = hex.Substring(2);
			}

			result = null;
			if (hex.Length != WIDTH_BYTE * 2)
			{
				return false;
			}

			if (!((HexEncoder) Encoders.Hex).IsValid(hex))
			{
				return false;
			}

			result = new uint160(hex);
			return true;
		}

		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			uint value;
			switch (uintIndex)
			{
				case 0:
					value = pn0;
					break;
				case 1:
					value = pn1;
					break;
				case 2:
					value = pn2;
					break;
				case 3:
					value = pn3;
					break;
				case 4:
					value = pn4;
					break;
				default:
					throw new ArgumentOutOfRangeException("index");
			}

			return (byte) (value >> (byteIndex * 8));
		}

		public override string ToString()
		{
			return Encoder.EncodeData(ToBytes().Reverse().ToArray());
		}

		public override bool Equals(object obj)
		{
			var item = obj as uint160;
			if (item == null)
			{
				return false;
			}

			var equals = true;
			equals &= pn0 == item.pn0;
			equals &= pn1 == item.pn1;
			equals &= pn2 == item.pn2;
			equals &= pn3 == item.pn3;
			equals &= pn4 == item.pn4;
			return equals;
		}

		public static bool operator ==(uint160 a, uint160 b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if ((object) a == null || (object) b == null)
			{
				return false;
			}

			var equals = true;
			equals &= a.pn0 == b.pn0;
			equals &= a.pn1 == b.pn1;
			equals &= a.pn2 == b.pn2;
			equals &= a.pn3 == b.pn3;
			equals &= a.pn4 == b.pn4;
			return equals;
		}

		public static bool operator <(uint160 a, uint160 b)
		{
			return Comparison(a, b) < 0;
		}

		public static bool operator >(uint160 a, uint160 b)
		{
			return Comparison(a, b) > 0;
		}

		public static bool operator <=(uint160 a, uint160 b)
		{
			return Comparison(a, b) <= 0;
		}

		public static bool operator >=(uint160 a, uint160 b)
		{
			return Comparison(a, b) >= 0;
		}

		private static int Comparison(uint160 a, uint160 b)
		{
			if (a.pn4 < b.pn4)
			{
				return -1;
			}

			if (a.pn4 > b.pn4)
			{
				return 1;
			}

			if (a.pn3 < b.pn3)
			{
				return -1;
			}

			if (a.pn3 > b.pn3)
			{
				return 1;
			}

			if (a.pn2 < b.pn2)
			{
				return -1;
			}

			if (a.pn2 > b.pn2)
			{
				return 1;
			}

			if (a.pn1 < b.pn1)
			{
				return -1;
			}

			if (a.pn1 > b.pn1)
			{
				return 1;
			}

			if (a.pn0 < b.pn0)
			{
				return -1;
			}

			if (a.pn0 > b.pn0)
			{
				return 1;
			}

			return 0;
		}

		public static bool operator !=(uint160 a, uint160 b)
		{
			return !(a == b);
		}

		public static bool operator ==(uint160 a, ulong b)
		{
			return a == new uint160(b);
		}

		public static bool operator !=(uint160 a, ulong b)
		{
			return !(a == new uint160(b));
		}

		public static implicit operator uint160(ulong value)
		{
			return new uint160(value);
		}


		public byte[] ToBytes(bool lendian = true)
		{
			var arr = new byte[WIDTH_BYTE];
			Buffer.BlockCopy(Utils.ToBytes(pn0, true), 0, arr, 4 * 0, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn1, true), 0, arr, 4 * 1, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn2, true), 0, arr, 4 * 2, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn3, true), 0, arr, 4 * 3, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn4, true), 0, arr, 4 * 4, 4);
			if (!lendian)
			{
				Array.Reverse(arr);
			}

			return arr;
		}

		public MutableUint160 AsBitcoinSerializable()
		{
			return new MutableUint160(this);
		}

		public int GetSerializeSize(int nType = 0, uint? protocolVersion = null)
		{
			return WIDTH_BYTE;
		}

		public ulong GetLow64()
		{
			return pn0 | ((ulong) pn1 << 32);
		}

		public uint GetLow32()
		{
			return pn0;
		}

		public override int GetHashCode()
		{
			var hash = 17;
			unchecked
			{
				hash = hash * 31 + (int) pn0;
				hash = hash * 31 + (int) pn1;
				hash = hash * 31 + (int) pn2;
				hash = hash * 31 + (int) pn3;
				hash = hash * 31 + (int) pn4;
			}

			return hash;
		}

		public class MutableUint160 : IBitcoinSerializable
		{
			public MutableUint160()
			{
				Value = Zero;
			}

			public MutableUint160(uint160 value)
			{
				Value = value;
			}

			public uint160 Value { get; set; }

			public void ReadWrite(BitcoinStream stream)
			{
				if (stream.Serializing)
				{
					var b = Value.ToBytes();
					stream.ReadWrite(ref b);
				}
				else
				{
					var b = new byte[WIDTH_BYTE];
					stream.ReadWrite(ref b);
					Value = new uint160(b);
				}
			}
		}
	}
}