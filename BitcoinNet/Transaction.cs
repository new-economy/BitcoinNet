﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitcoinNet.Crypto;
using BitcoinNet.DataEncoders;
using BitcoinNet.Protocol;
using BitcoinNet.Scripting;

namespace BitcoinNet
{
	public class OutPoint : IBitcoinSerializable
	{
		private uint256 _hash = uint256.Zero;
		private uint _n;

		public OutPoint()
		{
			SetNull();
		}

		public OutPoint(uint256 hashIn, uint nIn)
		{
			_hash = hashIn;
			_n = nIn;
		}

		public OutPoint(uint256 hashIn, int nIn)
		{
			_hash = hashIn;
			_n = nIn == -1 ? _n = uint.MaxValue : (uint) nIn;
		}

		public OutPoint(Transaction tx, uint i)
			: this(tx.GetHash(), i)
		{
		}

		public OutPoint(Transaction tx, int i)
			: this(tx.GetHash(), i)
		{
		}

		public OutPoint(OutPoint outpoint)
		{
			this.FromBytes(outpoint.ToBytes());
		}

		public bool IsNull => _hash == uint256.Zero && _n == uint.MaxValue;


		public uint256 Hash
		{
			get => _hash;
			set => _hash = value;
		}

		public uint N
		{
			get => _n;
			set => _n = value;
		}
		//IMPLEMENT_SERIALIZE( READWRITE(FLATDATA(*this)); )

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _hash);
			stream.ReadWrite(ref _n);
		}

		public static bool TryParse(string str, out OutPoint result)
		{
			result = null;
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}

			var splitted = str.Split('-');
			if (splitted.Length != 2)
			{
				return false;
			}

			if (!uint256.TryParse(splitted[0], out var hash))
			{
				return false;
			}

			if (!uint.TryParse(splitted[1], out var index))
			{
				return false;
			}

			result = new OutPoint(hash, index);
			return true;
		}

		public static OutPoint Parse(string str)
		{
			if (TryParse(str, out var result))
			{
				return result;
			}

			throw new FormatException("The format of the outpoint is incorrect");
		}


		private void SetNull()
		{
			_hash = uint256.Zero;
			_n = uint.MaxValue;
		}

		public static bool operator <(OutPoint a, OutPoint b)
		{
			return a._hash < b._hash || a._hash == b._hash && a._n < b._n;
		}

		public static bool operator >(OutPoint a, OutPoint b)
		{
			return a._hash > b._hash || a._hash == b._hash && a._n > b._n;
		}

		public static bool operator ==(OutPoint a, OutPoint b)
		{
			if (ReferenceEquals(a, null))
			{
				return ReferenceEquals(b, null);
			}

			if (ReferenceEquals(b, null))
			{
				return false;
			}

			return a._hash == b._hash && a._n == b._n;
		}

		public static bool operator !=(OutPoint a, OutPoint b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			var item = obj as OutPoint;
			if (ReferenceEquals(null, item))
			{
				return false;
			}

			return item == this;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return 17 + _hash.GetHashCode() * 31 + _n.GetHashCode() * 31 * 31;
			}
		}

		public override string ToString()
		{
			return Hash + "-" + N;
		}
	}


	public class TxIn : IBitcoinSerializable
	{
		protected uint _nSequence = uint.MaxValue;
		protected OutPoint _prevout = new OutPoint();
		protected Script _scriptSig = Script.Empty;

		public TxIn()
		{
		}

		public TxIn(Script scriptSig)
		{
			_scriptSig = scriptSig;
		}

		public TxIn(OutPoint prevout, Script scriptSig)
		{
			_prevout = prevout;
			_scriptSig = scriptSig;
		}

		public TxIn(OutPoint prevout)
		{
			_prevout = prevout;
		}

		public Sequence Sequence
		{
			get => _nSequence;
			set => _nSequence = value.Value;
		}

		public OutPoint PrevOut
		{
			get => _prevout;
			set => _prevout = value;
		}


		public Script ScriptSig
		{
			get => _scriptSig;
			set => _scriptSig = value;
		}

		public bool IsFinal => _nSequence == uint.MaxValue;

		// IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _prevout);
			stream.ReadWrite(ref _scriptSig);
			stream.ReadWrite(ref _nSequence);
		}


		/// <summary>
		///     Try to get the expected scriptPubKey of this TxIn based on its scriptSig and witScript.
		/// </summary>
		/// <returns>Null if could not infer the scriptPubKey, else, the expected scriptPubKey</returns>
		public IDestination GetSigner()
		{
			return _scriptSig.GetSigner();
		}

		public bool IsFrom(PubKey pubKey)
		{
			var result = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(ScriptSig);
			return result != null && result.PublicKey == pubKey;
		}

		public virtual ConsensusFactory GetConsensusFactory()
		{
			return BitcoinCash.Instance.Mainnet.Consensus.ConsensusFactory;
		}

		public virtual TxIn Clone()
		{
			var consensusFactory = GetConsensusFactory();
			if (!consensusFactory.TryCreateNew<TxIn>(out var txin))
			{
				txin = new TxIn();
			}

			txin.ReadWrite(new BitcoinStream(this.ToBytes()) {ConsensusFactory = consensusFactory});
			//txin.WitScript = (witScript ?? WitScript.Empty).Clone();
			return txin;
		}

		public static TxIn CreateCoinbase(int height)
		{
			var txin = new TxIn();
			txin.ScriptSig = new Script(Op.GetPushOp(height)) + OpcodeType.OP_0;
			return txin;
		}
	}

	public class TxOutCompressor : IBitcoinSerializable
	{
		public TxOutCompressor()
		{
		}

		public TxOutCompressor(TxOut txOut)
		{
			TxOut = txOut;
		}

		public TxOut TxOut { get; } = new TxOut();

		// IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				var val = CompressAmount((ulong) TxOut.Value.Satoshi);
				stream.ReadWriteAsCompactVarInt(ref val);
			}
			else
			{
				ulong val = 0;
				stream.ReadWriteAsCompactVarInt(ref val);
				TxOut.Value = new Money(DecompressAmount(val));
			}

			var cscript = new ScriptCompressor(TxOut.ScriptPubKey);
			stream.ReadWrite(ref cscript);
			if (!stream.Serializing)
			{
				TxOut.ScriptPubKey = new Script(cscript.ScriptBytes);
			}
		}
		// Amount compression:
		// * If the amount is 0, output 0
		// * first, divide the amount (in base units) by the largest power of 10 possible; call the exponent e (e is max 9)
		// * if e<9, the last digit of the resulting number cannot be 0; store it as d, and drop it (divide by 10)
		//   * call the result n
		//   * output 1 + 10*(9*n + d - 1) + e
		// * if e==9, we only know the resulting number is not zero, so output 1 + 10*(n - 1) + 9
		// (this is decodable, as d is in [1-9] and e is in [0-9])

		private ulong CompressAmount(ulong n)
		{
			if (n == 0)
			{
				return 0;
			}

			var e = 0;
			while (n % 10 == 0 && e < 9)
			{
				n /= 10;
				e++;
			}

			if (e < 9)
			{
				var d = (int) (n % 10);
				n /= 10;
				return 1 + (n * 9 + (ulong) (d - 1)) * 10 + (ulong) e;
			}

			return 1 + (n - 1) * 10 + 9;
		}

		private ulong DecompressAmount(ulong x)
		{
			// x = 0  OR  x = 1+10*(9*n + d - 1) + e  OR  x = 1+10*(n - 1) + 9
			if (x == 0)
			{
				return 0;
			}

			x--;
			// x = 10*(9*n + d - 1) + e
			var e = (int) (x % 10);
			x /= 10;
			ulong n = 0;
			if (e < 9)
			{
				// x = 9*n + d - 1
				var d = (int) (x % 9 + 1);
				x /= 9;
				// x = n
				n = x * 10 + (ulong) d;
			}
			else
			{
				n = x + 1;
			}

			while (e != 0)
			{
				n *= 10;
				e--;
			}

			return n;
		}
	}

	public class ScriptCompressor : IBitcoinSerializable
	{
		// make this static for now (there are only 6 special scripts defined)
		// this can potentially be extended together with a new nVersion for
		// transactions, in which case this value becomes dependent on nVersion
		// and nHeight of the enclosing transaction.
		private const uint NSpecialScripts = 6;
		private byte[] _script;

		public ScriptCompressor(Script script)
		{
			_script = script.ToBytes(true);
		}

		public ScriptCompressor()
		{
		}

		public byte[] ScriptBytes => _script;

		// IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				var compr = Compress();
				if (compr != null)
				{
					stream.ReadWrite(ref compr);
					return;
				}

				var nSize = (uint) _script.Length + NSpecialScripts;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				stream.ReadWrite(ref _script);
			}
			else
			{
				uint nSize = 0;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				if (nSize < NSpecialScripts)
				{
					var vch = new byte[GetSpecialSize(nSize)];
					stream.ReadWrite(ref vch);
					_script = Decompress(nSize, vch).ToBytes();
					return;
				}

				nSize -= NSpecialScripts;
				_script = new byte[nSize];
				stream.ReadWrite(ref _script);
			}
		}

		public Script GetScript()
		{
			return new Script(_script);
		}

		private byte[] Compress()
		{
			byte[] result = null;
			var script = Script.FromBytesUnsafe(_script);
			var keyID = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if (keyID != null)
			{
				result = new byte[21];
				result[0] = 0x00;
				Array.Copy(keyID.ToBytes(true), 0, result, 1, 20);
				return result;
			}

			var scriptID = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if (scriptID != null)
			{
				result = new byte[21];
				result[0] = 0x01;
				Array.Copy(scriptID.ToBytes(true), 0, result, 1, 20);
				return result;
			}

			var pubkey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(script, true);
			if (pubkey != null)
			{
				result = new byte[33];
				var pubBytes = pubkey.ToBytes(true);
				Array.Copy(pubBytes, 1, result, 1, 32);
				if (pubBytes[0] == 0x02 || pubBytes[0] == 0x03)
				{
					result[0] = pubBytes[0];
					return result;
				}

				if (pubBytes[0] == 0x04)
				{
					result[0] = (byte) (0x04 | (pubBytes[64] & 0x01));
					return result;
				}
			}

			return null;
		}

		private Script Decompress(uint nSize, byte[] data)
		{
			switch (nSize)
			{
				case 0x00:
					return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(new KeyId(data.SafeSubArray(0, 20)));
				case 0x01:
					return PayToScriptHashTemplate.Instance.GenerateScriptPubKey(
						new ScriptId(data.SafeSubArray(0, 20)));
				case 0x02:
				case 0x03:
					var keyPart = data.SafeSubArray(0, 32);
					var keyBytes = new byte[33];
					keyBytes[0] = (byte) nSize;
					Array.Copy(keyPart, 0, keyBytes, 1, 32);
					return PayToPubkeyTemplate.Instance.GenerateScriptPubKey(keyBytes);
				case 0x04:
				case 0x05:
					var vch = new byte[33];
					vch[0] = (byte) (nSize - 2);
					Array.Copy(data, 0, vch, 1, 32);
					var pubkey = new PubKey(vch, true);
					pubkey = pubkey.Decompress();
					return PayToPubkeyTemplate.Instance.GenerateScriptPubKey(pubkey);
			}

			return null;
		}

		private int GetSpecialSize(uint nSize)
		{
			if (nSize == 0 || nSize == 1)
			{
				return 20;
			}

			if (nSize == 2 || nSize == 3 || nSize == 4 || nSize == 5)
			{
				return 32;
			}

			return 0;
		}
	}

	public class TxOut : IBitcoinSerializable, IDestination
	{
		private static readonly Money NullMoney = new Money(-1);
		protected Script _publicKey = Script.Empty;

		public TxOut()
		{
		}

		public TxOut(Money value, IDestination destination)
		{
			Value = value;
			if (destination != null)
			{
				ScriptPubKey = destination.ScriptPubKey;
			}
		}

		public TxOut(Money value, Script scriptPubKey)
		{
			Value = value;
			ScriptPubKey = scriptPubKey;
		}

		public Money Value { get; set; } = NullMoney;

		// IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			var value = Value.Satoshi;
			stream.ReadWrite(ref value);
			if (!stream.Serializing)
			{
				Value = new Money(value);
			}

			stream.ReadWrite(ref _publicKey);
		}

		public Script ScriptPubKey
		{
			get => _publicKey;
			set => _publicKey = value;
		}


		public bool IsDust(FeeRate minRelayTxFee)
		{
			return Value < GetDustThreshold(minRelayTxFee);
		}

		public Money GetDustThreshold(FeeRate minRelayTxFee)
		{
			if (minRelayTxFee == null)
			{
				throw new ArgumentNullException(nameof(minRelayTxFee));
			}

			var nSize = this.GetSerializedSize() + 148;
			return 3 * minRelayTxFee.GetFee(nSize);
		}

		public bool IsTo(IDestination destination)
		{
			return ScriptPubKey == destination.ScriptPubKey;
		}

		public static TxOut Parse(string hex)
		{
			var ret = new TxOut();
			ret.FromBytes(Encoders.Hex.DecodeData(hex));
			return ret;
		}

		public virtual TxOut Clone()
		{
			var consensusFactory = GetConsensusFactory();
			if (!consensusFactory.TryCreateNew<TxOut>(out var txout))
			{
				txout = new TxOut();
			}

			txout.ReadWrite(new BitcoinStream(this.ToBytes()) {ConsensusFactory = consensusFactory});
			return txout;
		}

		public virtual ConsensusFactory GetConsensusFactory()
		{
			return BitcoinCash.Instance.Mainnet.Consensus.ConsensusFactory;
		}
	}

	public class IndexedTxIn
	{
		public TxIn TxIn { get; set; }

		/// <summary>
		///     The index of this TxIn in its transaction
		/// </summary>
		public uint Index { get; set; }

		public OutPoint PrevOut
		{
			get => TxIn.PrevOut;
			set => TxIn.PrevOut = value;
		}

		public Script ScriptSig
		{
			get => TxIn.ScriptSig;
			set => TxIn.ScriptSig = value;
		}

		public Transaction Transaction { get; set; }

		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError unused;
			return VerifyScript(scriptPubKey, scriptVerify, out unused);
		}

		public bool VerifyScript(Script scriptPubKey, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int) Index, null, out error);
		}

		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int) Index, null, scriptVerify, SigHash.Undefined,
				out error);
		}

		public bool VerifyScript(Script scriptPubKey, Money value, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int) Index, value, scriptVerify, SigHash.Undefined,
				out error);
		}

		public bool VerifyScript(ICoin coin, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			return VerifyScript(coin, scriptVerify, out _);
		}

		public bool VerifyScript(ICoin coin, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(coin.TxOut.ScriptPubKey, Transaction, (int) Index, coin.TxOut.Value,
				scriptVerify, SigHash.Undefined, out error);
		}

		public bool VerifyScript(ICoin coin, out ScriptError error)
		{
			return VerifyScript(coin, ScriptVerify.Standard, out error);
		}

		public TransactionSignature Sign(Key key, ICoin coin, SigHash sigHash)
		{
			var hash = GetSignatureHash(coin, sigHash);
			return key.Sign(hash, sigHash);
		}

		public uint256 GetSignatureHash(ICoin coin, SigHash sigHash = SigHash.All)
		{
			return Transaction.GetSignatureHash(coin.GetScriptCode(), (int) Index, sigHash, coin.TxOut.Value);
		}
	}

	public class TxInList : UnsignedList<TxIn>
	{
		public TxInList()
		{
		}

		public TxInList(Transaction parent)
			: base(parent)
		{
		}

		public TxIn this[OutPoint outpoint]
		{
			get => this[outpoint.N];
			set => this[outpoint.N] = value;
		}

		/// <summary>
		///     Returns the IndexedTxIn whose PrevOut is equal to <paramref name="outpoint" /> or null.
		/// </summary>
		/// <param name="outpoint">The outpoint being searched for</param>
		/// <returns>The IndexedTxIn which PrevOut is equal to <paramref name="outpoint" /> or null if not found</returns>
		public IndexedTxIn FindIndexedInput(OutPoint outpoint)
		{
			if (outpoint == null)
			{
				throw new ArgumentNullException(nameof(outpoint));
			}

			for (var i = 0; i < Count; i++)
			{
				var txin = this[i];
				if (outpoint == txin.PrevOut)
				{
					return new IndexedTxIn
					{
						TxIn = txin,
						Index = (uint) i,
						Transaction = Transaction
					};
				}
			}

			return null;
		}

		public TxIn CreateNewTxIn(OutPoint outpoint = null, Script scriptSig = null, Sequence? sequence = null)
		{
			if (!Transaction.GetConsensusFactory().TryCreateNew(out TxIn txIn))
			{
				txIn = new TxIn();
			}

			if (outpoint != null)
			{
				txIn.PrevOut = outpoint;
			}

			if (scriptSig != null)
			{
				txIn.ScriptSig = scriptSig;
			}

			if (sequence.HasValue)
			{
				txIn.Sequence = sequence.Value;
			}

			return txIn;
		}

		public TxIn Add(OutPoint outpoint = null, Script scriptSig = null, Sequence? sequence = null)
		{
			var txIn = CreateNewTxIn(outpoint, scriptSig, sequence);
			return Add(txIn);
		}

		public new TxIn Add(TxIn txIn)
		{
			base.Add(txIn);
			return txIn;
		}

		public IEnumerable<IndexedTxIn> AsIndexedInputs()
		{
			// We want i as the index of txIn in Intputs[], not index in enumerable after where filter
			return this.Select((r, i) => new IndexedTxIn
			{
				TxIn = r,
				Index = (uint) i,
				Transaction = Transaction
			});
		}

		public TxIn Add(Transaction prevTx, int outIndex)
		{
			if (outIndex >= prevTx.Outputs.Count)
			{
				throw new InvalidOperationException("Output " + outIndex + " is not present in the prevTx");
			}

			var @in = CreateNewTxIn();
			@in.PrevOut.Hash = prevTx.GetHash();
			@in.PrevOut.N = (uint) outIndex;
			return Add(@in);
		}
	}

	public class IndexedTxOut
	{
		public TxOut TxOut { get; set; }

		public uint N { get; set; }

		public Transaction Transaction { get; set; }

		public Coin ToCoin()
		{
			return new Coin(this);
		}
	}

	public class TxOutList : UnsignedList<TxOut>
	{
		public TxOutList()
		{
		}

		public TxOutList(Transaction parent)
			: base(parent)
		{
		}

		public IEnumerable<TxOut> To(IDestination destination)
		{
			return this.Where(r => r.IsTo(destination));
		}

		public IEnumerable<TxOut> To(Script scriptPubKey)
		{
			return this.Where(r => r.ScriptPubKey == scriptPubKey);
		}

		public IEnumerable<IndexedTxOut> AsIndexedOutputs()
		{
			// We want i as the index of txOut in Outputs[], not index in enumerable after where filter
			return this.Select((r, i) => new IndexedTxOut
			{
				TxOut = r,
				N = (uint) i,
				Transaction = Transaction
			});
		}

		public IEnumerable<Coin> AsCoins()
		{
			var txId = Transaction.GetHash();
			for (var i = 0; i < Count; i++)
			{
				yield return new Coin(new OutPoint(txId, i), this[i]);
			}
		}

		public TxOut CreateNewTxOut()
		{
			return CreateNewTxOut(null, null as Script);
		}

		public TxOut CreateNewTxOut(Money money = null, Script scriptPubKey = null)
		{
			if (!Transaction.GetConsensusFactory().TryCreateNew<TxOut>(out var txout))
			{
				txout = new TxOut();
			}

			if (money != null)
			{
				txout.Value = money;
			}

			if (scriptPubKey != null)
			{
				txout.ScriptPubKey = scriptPubKey;
			}

			return txout;
		}

		public TxOut CreateNewTxOut(Money money = null, IDestination destination = null)
		{
			return CreateNewTxOut(money, destination?.ScriptPubKey);
		}

		public TxOut Add(Money money = null, Script scriptPubKey = null)
		{
			var txOut = CreateNewTxOut(money, scriptPubKey);
			return Add(txOut);
		}

		public TxOut Add(Money money = null, IDestination destination = null)
		{
			return Add(money, destination?.ScriptPubKey);
		}

		public new TxOut Add(TxOut txOut)
		{
			base.Add(txOut);
			return txOut;
		}
	}

	[Flags]
	public enum TransactionOptions : uint
	{
		None = 0x00000000,
		Witness = 0x40000000,
		All = Witness
	}

	//https://en.bitcoin.it/wiki/Transactions
	//https://en.bitcoin.it/wiki/Protocol_specification
	public class Transaction : IBitcoinSerializable
	{
		[Flags]
		public enum LockTimeFlags
		{
			None = 0,

			/// <summary>
			///     Interpret sequence numbers as relative lock-time constraints.
			/// </summary>
			VerifySequence = 1 << 0,

			/// <summary>
			///     Use GetMedianTimePast() instead of nTime for end point timestamp.
			/// </summary>
			MedianTimePast = 1 << 1
		}

		//Since it is impossible to serialize a transaction with 0 input without problems during deserialization with wit activated, we fit a flag in the version to workaround it
		protected const uint NoDummyInput = 1 << 27;

		public static uint CurrentVersion = 2;
		public static uint MaxStandardTxSize = 100000;

		internal static readonly int WitnessScaleFactor = 4;

		private static readonly uint MaxBlockSize = 1000000;
		private static readonly ulong MaxMoney = 21000000ul * Money.Coin;

		private uint256[] _hashes;
		private LockTime _nLockTime;
		private uint _nVersion = 1;
		private TxInList _vin;
		private TxOutList _vout;


		[Obsolete("You should better use Transaction.Create(Network network)")]
		public Transaction()
		{
			_vin = new TxInList(this);
			_vout = new TxOutList(this);
		}

		[Obsolete("You should instantiate Transaction from ConsensusFactory.CreateTransaction")]
		public Transaction(string hex, uint? version = null)
			: this()
		{
			this.FromBytes(Encoders.Hex.DecodeData(hex), version);
		}

		[Obsolete("You should instantiate Transaction from ConsensusFactory.CreateTransaction")]
		public Transaction(byte[] bytes)
			: this()
		{
			FromBytes(bytes);
		}

		public bool RBF
		{
			get { return Inputs.Any(i => i.Sequence < 0xffffffff - 1); }
		}

		public uint Version
		{
			get => _nVersion;
			set => _nVersion = value;
		}

		public Money TotalOut
		{
			get { return Outputs.Sum(v => v.Value); }
		}

		public LockTime LockTime
		{
			get => _nLockTime;
			set => _nLockTime = value;
		}

		public TxInList Inputs => _vin;

		public TxOutList Outputs => _vout;

		public bool IsCoinBase => Inputs.Count == 1 && Inputs[0].PrevOut.IsNull;

		// IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			if (!stream.Serializing)
			{
				stream.ReadWrite(ref _nVersion);
				/* Try to read the vin. In case the dummy is there, this will be read as an empty vector. */
				stream.ReadWrite<TxInList, TxIn>(ref _vin);

				/* We read a non-empty vin. Assume a normal vout follows. */
				stream.ReadWrite<TxOutList, TxOut>(ref _vout);
				_vout.Transaction = this;
			}
			else
			{
				var version = _nVersion;
				stream.ReadWrite(ref version);
				stream.ReadWrite<TxInList, TxIn>(ref _vin);
				_vin.Transaction = this;
				stream.ReadWrite<TxOutList, TxOut>(ref _vout);
				_vout.Transaction = this;
			}

			stream.ReadWriteStruct(ref _nLockTime);
		}

		public static Transaction Create(Network network)
		{
			return network.Consensus.ConsensusFactory.CreateTransaction();
		}

		public uint256 GetHash()
		{
			uint256 h = null;
			var hashes = _hashes;
			if (hashes != null)
			{
				h = hashes[0];
			}

			if (h != null)
			{
				return h;
			}

			using (var hs = CreateHashStream())
			{
				var stream = new BitcoinStream(hs, true)
				{
					TransactionOptions = TransactionOptions.None,
					ConsensusFactory = GetConsensusFactory()
				};
				stream.SerializationTypeScope(SerializationType.Hash);
				ReadWrite(stream);
				h = hs.GetHash();
			}

			hashes = _hashes;
			if (hashes != null)
			{
				hashes[0] = h;
			}

			return h;
		}

		protected virtual HashStreamBase CreateHashStream()
		{
			return new HashStream();
		}

		protected virtual HashStreamBase CreateSignatureHashStream()
		{
			return new HashStream();
		}

		[Obsolete("Call PrecomputeHash(true, true) instead")]
		public void CacheHashes()
		{
			PrecomputeHash(true, true);
		}

		/// <summary>
		///     Precompute the transaction hash and witness hash so that later calls to GetHash() and GetWitHash() will returns the
		///     precomputed hash
		/// </summary>
		/// <param name="invalidateExisting">If true, the previous precomputed hash is thrown away, else it is reused</param>
		/// <param name="lazily">
		///     If true, the hash will be calculated and cached at the first call to GetHash(), else it will be
		///     immediately
		/// </param>
		public void PrecomputeHash(bool invalidateExisting, bool lazily)
		{
			_hashes = invalidateExisting ? new uint256[2] : _hashes ?? new uint256[2];
			if (!lazily && _hashes[0] == null)
			{
				_hashes[0] = GetHash();
			}

			if (!lazily && _hashes[1] == null)
			{
				_hashes[1] = GetWitHash();
			}
		}

		public Transaction Clone(bool cloneCache)
		{
			var clone = BitcoinSerializableExtensions.Clone(this);
			if (cloneCache)
			{
				clone._hashes = _hashes.ToArray();
			}

			return clone;
		}

		public uint256 GetWitHash()
		{
			return GetHash();
		}

		public uint256 GetSignatureHash(ICoin coin, SigHash sigHash = SigHash.All)
		{
			return GetIndexedInput(coin).GetSignatureHash(coin, sigHash);
		}

		public TransactionSignature SignInput(ISecret secret, ICoin coin, SigHash sigHash = SigHash.All)
		{
			return SignInput(secret.PrivateKey, coin, sigHash);
		}

		public TransactionSignature SignInput(Key key, ICoin coin, SigHash sigHash = SigHash.All)
		{
			return GetIndexedInput(coin).Sign(key, coin, sigHash);
		}

		private IndexedTxIn GetIndexedInput(ICoin coin)
		{
			return Inputs.FindIndexedInput(coin.Outpoint) ??
			       throw new ArgumentException("The coin is not being spent by this transaction", nameof(coin));
		}

		[Obsolete("Use Output.Add(Money money = null, IDestination destination = null) instead")]
		public TxOut AddOutput(Money money, IDestination destination)
		{
			return AddOutput(money, destination.ScriptPubKey);
		}

		[Obsolete("Use Output.Add(Money money = null, Script scriptPubKey = null) instead")]
		public TxOut AddOutput(Money money, Script scriptPubKey)
		{
			return AddOutput(CreateOutput(money, scriptPubKey));
		}

		[Obsolete("Use Output.CreateNewTxOut(Money money = null, Script scriptPubKey = null) instead")]
		public TxOut CreateOutput(Money money, Script scriptPubKey)
		{
			return Outputs.CreateNewTxOut(money, scriptPubKey);
		}

		[Obsolete("Use Output.Add(Money money = null, Script scriptPubKey = null) instead")]
		public TxOut AddOutput(TxOut @out)
		{
			_vout.Add(@out);
			return @out;
		}

		[Obsolete(
			"Use Inputs.Add(OutPoint outpoint = null, Script scriptSig = null, Sequence? sequence = null) instead")]
		public TxIn AddInput(TxIn @in)
		{
			_vin.Add(@in);
			return @in;
		}

		/// <summary>
		///     Size of the transaction discounting the witness (Used for fee calculation)
		/// </summary>
		/// <returns>Transaction size</returns>
		public int GetVirtualSize()
		{
			var totalSize = this.GetSerializedSize(TransactionOptions.Witness);
			var strippedSize = this.GetSerializedSize(TransactionOptions.None);
			// This implements the weight = (stripped_size * 4) + witness_size formula,
			// using only serialization with and without witness data. As witness_size
			// is equal to total_size - stripped_size, this formula is identical to:
			// weight = (stripped_size * 3) + total_size.
			var weight = strippedSize * (WitnessScaleFactor - 1) + totalSize;
			return (weight + WitnessScaleFactor - 1) / WitnessScaleFactor;
		}

		[Obsolete("Use Inputs.Add(prevTx, int outIndex) instead")]
		public TxIn AddInput(Transaction prevTx, int outIndex)
		{
			return Inputs.Add(prevTx, outIndex);
		}


		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secrets">Secrets</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret[] secrets, ICoin[] coins)
		{
			Sign(secrets.Select(s => s.PrivateKey).ToArray(), coins);
		}

		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="keys">Private keys</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(Key[] keys, ICoin[] coins)
		{
			var builder = GetConsensusFactory().CreateTransactionBuilder();
			builder.AddKeys(keys);
			builder.AddCoins(coins);
			builder.SignTransactionInPlace(this);
		}

		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secret">Secret</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret secret, ICoin[] coins)
		{
			Sign(new[] {secret}, coins);
		}

		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secrets">Secrets</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret[] secrets, ICoin coin)
		{
			Sign(secrets, new[] {coin});
		}

		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secret">Secret</param>
		/// <param name="coin">Coins to sign</param>
		public void Sign(ISecret secret, ICoin coin)
		{
			Sign(new[] {secret}, new[] {coin});
		}

		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="key">Private key</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(Key key, ICoin[] coins)
		{
			Sign(new[] {key}, coins);
		}

		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="key">Private key</param>
		/// <param name="coin">Coin to sign</param>
		public void Sign(Key key, ICoin coin)
		{
			Sign(new[] {key}, new[] {coin});
		}

		/// <summary>
		///     Sign a specific coin with the given secret
		/// </summary>
		/// <param name="keys">Private keys</param>
		/// <param name="coin">Coin to sign</param>
		public void Sign(Key[] keys, ICoin coin)
		{
			Sign(keys, new[] {coin});
		}

		/// <summary>
		///     Sign the transaction with a private key
		///     <para>ScriptSigs should be filled with previous ScriptPubKeys</para>
		///     <para>For more complex scenario, use TransactionBuilder</para>
		/// </summary>
		/// <param name="secret"></param>
		[Obsolete("Use Sign(ISecret,ICoin[]) instead)")]
		public void Sign(ISecret secret, bool assumeP2SH)
		{
			Sign(secret.PrivateKey, assumeP2SH);
		}

		/// <summary>
		///     Sign the transaction with a private key
		///     <para>ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)</para>
		///     <para>For more complex scenario, use TransactionBuilder</para>
		/// </summary>
		/// <param name="secret"></param>
		[Obsolete("Use Sign(Key,ICoin[]) instead)")]
		public void Sign(Key key, bool assumeP2SH)
		{
			var coins = new List<Coin>();
			for (var i = 0; i < Inputs.Count; i++)
			{
				var txin = Inputs[i];
				if (Script.IsNullOrEmpty(txin.ScriptSig))
				{
					throw new InvalidOperationException(
						"ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)");
				}

				if (assumeP2SH)
				{
					var p2shSig = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(txin.ScriptSig);
					if (p2shSig == null)
					{
						coins.Add(new ScriptCoin(txin.PrevOut, new TxOut
						{
							ScriptPubKey = txin.ScriptSig.PaymentScript
						}, txin.ScriptSig));
					}
					else
					{
						coins.Add(new ScriptCoin(txin.PrevOut, new TxOut
						{
							ScriptPubKey = p2shSig.RedeemScript.PaymentScript
						}, p2shSig.RedeemScript));
					}
				}
				else
				{
					coins.Add(new Coin(txin.PrevOut, new TxOut
					{
						ScriptPubKey = txin.ScriptSig
					}));
				}
			}

			Sign(key, coins.ToArray());
		}

		public TxPayload CreatePayload()
		{
			return new TxPayload(Clone());
		}

		public static Transaction Parse(string hex, Network network)
		{
			var tx = network.Consensus.ConsensusFactory.CreateTransaction();
			var data = Encoders.Hex.DecodeData(hex);
			var stream = new BitcoinStream(data) {ConsensusFactory = network.Consensus.ConsensusFactory};
			tx.ReadWrite(stream);
			return tx;
		}

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(this.ToBytes());
		}

		/// <summary>
		///     Calculate the fee of the transaction
		/// </summary>
		/// <param name="spentCoins">Coins being spent</param>
		/// <returns>Fee or null if some spent coins are missing or if spentCoins is null</returns>
		public virtual Money GetFee(ICoin[] spentCoins)
		{
			if (IsCoinBase)
			{
				return Money.Zero;
			}

			spentCoins = spentCoins ?? new ICoin[0];

			var fees = -TotalOut;
			foreach (var input in Inputs)
			{
				var coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut);
				if (coin == null)
				{
					return null;
				}

				fees += coin.TxOut.Value;
			}

			return fees;
		}

		/// <summary>
		///     Calculate the fee rate of the transaction
		/// </summary>
		/// <param name="spentCoins">Coins being spent</param>
		/// <returns>Fee or null if some spent coins are missing or if spentCoins is null</returns>
		public FeeRate GetFeeRate(ICoin[] spentCoins)
		{
			var fee = GetFee(spentCoins);
			if (fee == null)
			{
				return null;
			}

			return new FeeRate(fee, GetVirtualSize());
		}

		public bool IsFinal(ChainedBlock block)
		{
			if (block == null)
			{
				return IsFinal(Utils.UnixTimeToDateTime(0), 0);
			}

			if (block.Header == null)
			{
				throw new InvalidOperationException("ChainedBlock.Header must be available");
			}

			return IsFinal(block.Header.BlockTime, block.Height);
		}

		public bool IsFinal(DateTimeOffset blockTime, int blockHeight)
		{
			var nBlockTime = Utils.DateTimeToUnixTime(blockTime);
			if (_nLockTime == 0)
			{
				return true;
			}

			if (_nLockTime < ((long) _nLockTime < LockTime.LockTimeThreshold ? (long) blockHeight : nBlockTime))
			{
				return true;
			}

			foreach (var txin in Inputs)
			{
				if (!txin.IsFinal)
				{
					return false;
				}
			}

			return true;
		}


		/// <summary>
		///     Calculates the block height and time which the transaction must be later than
		///     in order to be considered final in the context of BIP 68.  It also removes
		///     from the vector of input heights any entries which did not correspond to sequence
		///     locked inputs as they do not affect the calculation.
		/// </summary>
		/// <param name="prevHeights">Previous Height</param>
		/// <param name="block">The block being evaluated</param>
		/// <param name="flags">If VerifySequence is not set, returns always true SequenceLock</param>
		/// <returns>Sequence lock of minimum SequenceLock to satisfy</returns>
		public bool CheckSequenceLocks(int[] prevHeights, ChainedBlock block,
			LockTimeFlags flags = LockTimeFlags.VerifySequence)
		{
			return CalculateSequenceLocks(prevHeights, block, flags).Evaluate(block);
		}

		/// <summary>
		///     Calculates the block height and time which the transaction must be later than
		///     in order to be considered final in the context of BIP 68.  It also removes
		///     from the vector of input heights any entries which did not correspond to sequence
		///     locked inputs as they do not affect the calculation.
		/// </summary>
		/// <param name="prevHeights">Previous Height</param>
		/// <param name="block">The block being evaluated</param>
		/// <param name="flags">If VerifySequence is not set, returns always true SequenceLock</param>
		/// <returns>Sequence lock of minimum SequenceLock to satisfy</returns>
		public SequenceLock CalculateSequenceLocks(int[] prevHeights, ChainedBlock block,
			LockTimeFlags flags = LockTimeFlags.VerifySequence)
		{
			if (prevHeights.Length != Inputs.Count)
			{
				throw new ArgumentException(
					"The number of element in prevHeights should be equal to the number of inputs", "prevHeights");
			}

			// Will be set to the equivalent height- and time-based nLockTime
			// values that would be necessary to satisfy all relative lock-
			// time constraints given our view of block chain history.
			// The semantics of nLockTime are the last invalid height/time, so
			// use -1 to have the effect of any height or time being valid.
			var nMinHeight = -1;
			long nMinTime = -1;

			// tx.nVersion is signed integer so requires cast to unsigned otherwise
			// we would be doing a signed comparison and half the range of nVersion
			// wouldn't support BIP 68.
			var fEnforceBIP68 = Version >= 2
			                    && (flags & LockTimeFlags.VerifySequence) != 0;

			// Do not enforce sequence numbers as a relative lock time
			// unless we have been instructed to
			if (!fEnforceBIP68)
			{
				return new SequenceLock(nMinHeight, nMinTime);
			}

			for (var txinIndex = 0; txinIndex < Inputs.Count; txinIndex++)
			{
				var txin = Inputs[txinIndex];

				// Sequence numbers with the most significant bit set are not
				// treated as relative lock-times, nor are they given any
				// consensus-enforced meaning at this point.
				if ((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_DISABLE_FLAG) != 0)
				{
					// The height of this input is not relevant for sequence locks
					prevHeights[txinIndex] = 0;
					continue;
				}

				var nCoinHeight = prevHeights[txinIndex];

				if ((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG) != 0)
				{
					var nCoinTime =
						(long) Utils.DateTimeToUnixTimeLong(block.GetAncestor(Math.Max(nCoinHeight - 1, 0))
							.GetMedianTimePast());

					// Time-based relative lock-times are measured from the
					// smallest allowed timestamp of the block containing the
					// txout being spent, which is the median time past of the
					// block prior.
					nMinTime = Math.Max(nMinTime,
						nCoinTime + ((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_MASK) <<
						             Sequence.SEQUENCE_LOCKTIME_GRANULARITY) - 1);
				}
				else
				{
					// We subtract 1 from relative lock-times because a lock-
					// time of 0 has the semantics of "same block," so a lock-
					// time of 1 should mean "next block," but nLockTime has
					// the semantics of "last invalid block height."
					nMinHeight = Math.Max(nMinHeight,
						nCoinHeight + (int) (txin.Sequence & Sequence.SEQUENCE_LOCKTIME_MASK) - 1);
				}
			}

			return new SequenceLock(nMinHeight, nMinTime);
		}


		private DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b)
		{
			return a > b ? a : b;
		}

		/// <summary>
		///     Create a transaction with the specified option only. (useful for stripping data from a transaction)
		/// </summary>
		/// <param name="options">Options to keep</param>
		/// <returns>A new transaction with only the options wanted</returns>
		public Transaction WithOptions(TransactionOptions options)
		{
			if (options == TransactionOptions.None)
			{
				return this;
			}

			var instance = GetConsensusFactory().CreateTransaction();
			var ms = new MemoryStream();
			var bms = new BitcoinStream(ms, true) {TransactionOptions = options};
			ReadWrite(bms);
			ms.Position = 0;
			bms = new BitcoinStream(ms, false) {TransactionOptions = options};
			instance.ReadWrite(bms);
			return instance;
		}

		/// <summary>
		///     Context free transaction check
		/// </summary>
		/// <returns>The error or success of the check</returns>
		public TransactionCheckResult Check()
		{
			// Basic checks that don't depend on any context
			if (Inputs.Count == 0)
			{
				return TransactionCheckResult.NoInput;
			}

			if (Outputs.Count == 0)
			{
				return TransactionCheckResult.NoOutput;
			}

			// Size limits
			if (this.GetSerializedSize() > MaxBlockSize)
			{
				return TransactionCheckResult.TransactionTooLarge;
			}

			// Check for negative or overflow output values
			long nValueOut = 0;
			foreach (var txout in Outputs)
			{
				if (txout.Value < 0)
				{
					return TransactionCheckResult.NegativeOutput;
				}

				if (txout.Value > MaxMoney)
				{
					return TransactionCheckResult.OutputTooLarge;
				}

				nValueOut += txout.Value;
				if (!(nValueOut >= 0 && nValueOut <= (long) MaxMoney))
				{
					return TransactionCheckResult.OutputTotalTooLarge;
				}
			}

			// Check for duplicate inputs
			var vInOutPoints = new HashSet<OutPoint>();
			foreach (var txin in Inputs)
			{
				if (vInOutPoints.Contains(txin.PrevOut))
				{
					return TransactionCheckResult.DuplicateInputs;
				}

				vInOutPoints.Add(txin.PrevOut);
			}

			if (IsCoinBase)
			{
				if (Inputs[0].ScriptSig.Length < 2 || Inputs[0].ScriptSig.Length > 100)
				{
					return TransactionCheckResult.CoinbaseScriptTooLarge;
				}
			}
			else
			{
				foreach (var txin in Inputs)
				{
					if (txin.PrevOut.IsNull)
					{
						return TransactionCheckResult.NullInputPrevOut;
					}
				}
			}

			return TransactionCheckResult.Success;
		}


		public virtual uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, Money amount,
			PrecomputedTransactionData precomputedTransactionData)
		{
			if (nIn >= Inputs.Count)
			{
				Utils.log("ERROR: SignatureHash() : nIn=" + nIn + " out of range\n");
				return uint256.One;
			}

			var hashType = nHashType & (SigHash) 31;

			// Check for invalid use of SIGHASH_SINGLE
			if (hashType == SigHash.Single)
			{
				if (nIn >= Outputs.Count)
				{
					Utils.log("ERROR: SignatureHash() : nOut=" + nIn + " out of range\n");
					return uint256.One;
				}
			}

			var scriptCopy = new Script(scriptCode._script);
			scriptCopy = scriptCopy.FindAndDelete(OpcodeType.OP_CODESEPARATOR);

			var txCopy = GetConsensusFactory().CreateTransaction();
			txCopy.FromBytes(this.ToBytes());
			//Set all TxIn script to empty string
			foreach (var txin in txCopy.Inputs)
			{
				txin.ScriptSig = new Script();
			}

			//Copy subscript into the txin script you are checking
			txCopy.Inputs[nIn].ScriptSig = scriptCopy;

			if (hashType == SigHash.None)
			{
				//The output of txCopy is set to a vector of zero size.
				txCopy.Outputs.Clear();

				//All other inputs aside from the current input in txCopy have their nSequence index set to zero
				foreach (var input in txCopy.Inputs.Where((x, i) => i != nIn))
				{
					input.Sequence = 0;
				}
			}
			else if (hashType == SigHash.Single)
			{
				//The output of txCopy is resized to the size of the current input index+1.
				txCopy.Outputs.RemoveRange(nIn + 1, txCopy.Outputs.Count - (nIn + 1));
				//All other txCopy outputs aside from the output that is the same as the current input index are set to a blank script and a value of (long) -1.
				for (var i = 0; i < txCopy.Outputs.Count; i++)
				{
					if (i == nIn)
					{
						continue;
					}

					txCopy.Outputs[i] = txCopy.Outputs.CreateNewTxOut();
				}

				//All other txCopy inputs aside from the current input are set to have an nSequence index of zero.
				foreach (var input in txCopy.Inputs.Where((x, i) => i != nIn))
				{
					input.Sequence = 0;
				}
			}


			if ((nHashType & SigHash.AnyoneCanPay) != 0)
			{
				//The txCopy input vector is resized to a length of one.
				var script = txCopy.Inputs[nIn];
				txCopy.Inputs.Clear();
				txCopy.Inputs.Add(script);
				//The subScript (lead in by its length as a var-integer encoded!) is set as the first and only member of this vector.
				txCopy.Inputs[0].ScriptSig = scriptCopy;
			}


			//Serialize TxCopy, append 4 byte hashtypecode
			var stream = CreateHashWriter();
			txCopy.ReadWrite(stream);
			stream.ReadWrite((uint) nHashType);
			return GetHash(stream);
		}

		public uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, Money amount = null)
		{
			return GetSignatureHash(scriptCode, nIn, nHashType, amount, null);
		}

		private static uint256 GetHash(BitcoinStream stream)
		{
			var preimage = ((HashStreamBase) stream.Inner).GetHash();
			stream.Inner.Dispose();
			return preimage;
		}

		internal virtual uint256 GetHashOutputs()
		{
			uint256 hashOutputs;
			var ss = CreateHashWriter(); //BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
			foreach (var txout in Outputs)
			{
				ss.ReadWrite(txout);
			}

			hashOutputs = GetHash(ss);
			return hashOutputs;
		}

		internal virtual uint256 GetHashSequence()
		{
			uint256 hashSequence;
			var ss = CreateHashWriter(); //BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
			foreach (var input in Inputs)
			{
				ss.ReadWrite((uint) input.Sequence);
			}

			hashSequence = GetHash(ss);
			return hashSequence;
		}

		internal virtual uint256 GetHashPrevouts()
		{
			uint256 hashPrevouts;
			var ss = CreateHashWriter(); //BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
			foreach (var input in Inputs)
			{
				ss.ReadWrite(input.PrevOut);
			}

			hashPrevouts = GetHash(ss);
			return hashPrevouts;
		}

		protected BitcoinStream CreateHashWriter()
		{
			var hs = CreateSignatureHashStream();
			var stream = new BitcoinStream(hs, true)
			{
				Type = SerializationType.Hash, TransactionOptions = TransactionOptions.None
			};
			return stream;
		}

		public virtual ConsensusFactory GetConsensusFactory()
		{
			return BitcoinCash.Instance.Mainnet.Consensus.ConsensusFactory;
		}

		public Transaction Clone()
		{
			var instance = GetConsensusFactory().CreateTransaction();
			instance.ReadWrite(new BitcoinStream(this.ToBytes()) {ConsensusFactory = GetConsensusFactory()});
			return instance;
		}

		public void FromBytes(byte[] bytes)
		{
			ReadWrite(new BitcoinStream(bytes) {ConsensusFactory = GetConsensusFactory()});
		}
	}

	public enum TransactionCheckResult
	{
		Success,
		NoInput,
		NoOutput,
		NegativeOutput,
		OutputTooLarge,
		OutputTotalTooLarge,
		TransactionTooLarge,
		DuplicateInputs,
		NullInputPrevOut,
		CoinbaseScriptTooLarge
	}
}