﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BitcoinNet.DataEncoders;

namespace BitcoinNet.Scripting
{
	[DebuggerDisplay("{ToString()}")]
	public class Script
	{
		internal readonly byte[] _script;
		private ScriptId _hash;
		private Script _paymentScript;

		public Script()
		{
			_script = new byte[0];
		}

		public Script(params Op[] ops)
			: this((IEnumerable<Op>) ops)
		{
		}

		public Script(IEnumerable<Op> ops)
		{
			using (var ms = new MemoryStream())
			{
				foreach (var op in ops)
				{
					op.WriteTo(ms);
				}

				_script = ms.ToArray();
			}
		}

		public Script(string script)
		{
			_script = Parse(script);
		}

		public Script(byte[] data)
			: this((IEnumerable<byte>) data)
		{
		}


		private Script(byte[] data, bool @unsafe, bool unused)
		{
			_script = @unsafe ? data : data.ToArray();
		}

		public Script(IEnumerable<byte> data)
		{
			_script = data.ToArray();
		}

		public Script(byte[] data, bool compressed)
		{
			if (!compressed)
			{
				_script = data.ToArray();
			}
			else
			{
				var compressor = new ScriptCompressor();
				compressor.ReadWrite(new BitcoinStream(data));
				_script = compressor.GetScript()._script;
			}
		}

		public static Script Empty { get; } = new Script();

		public int Length => _script.Length;

		/// <summary>
		///     Get the P2SH scriptPubKey of this script
		/// </summary>
		public Script PaymentScript => _paymentScript ??
		                               (_paymentScript = PayToScriptHashTemplate.Instance.GenerateScriptPubKey(Hash));

		public bool IsPushOnly
		{
			get
			{
				foreach (var script in CreateReader().ToEnumerable())
				{
					if (script.PushData == null)
					{
						return false;
					}
				}

				return true;
			}
		}

		public bool HasCanonicalPushes
		{
			get
			{
				foreach (var op in CreateReader().ToEnumerable())
				{
					if (op.IsInvalid)
					{
						return false;
					}

					if (op.Code > OpcodeType.OP_16)
					{
						continue;
					}

					if (op.Code < OpcodeType.OP_PUSHDATA1 && op.Code > OpcodeType.OP_0 && op.PushData.Length == 1 &&
					    op.PushData[0] <= 16)
						// Could have used an OP_n code, rather than a 1-byte push.
					{
						return false;
					}

					if (op.Code == OpcodeType.OP_PUSHDATA1 && op.PushData.Length < (byte) OpcodeType.OP_PUSHDATA1)
						// Could have used a normal n-byte push, rather than OP_PUSHDATA1.
					{
						return false;
					}

					if (op.Code == OpcodeType.OP_PUSHDATA2 && op.PushData.Length <= 0xFF)
						// Could have used an OP_PUSHDATA1.
					{
						return false;
					}

					if (op.Code == OpcodeType.OP_PUSHDATA4 && op.PushData.Length <= 0xFFFF)
						// Could have used an OP_PUSHDATA2.
					{
						return false;
					}
				}

				return true;
			}
		}

		public ScriptId Hash => _hash ?? (_hash = new ScriptId(this));

		public bool IsPayToScriptHash => PayToScriptHashTemplate.Instance.CheckScriptPubKey(this);

		public bool IsUnspendable => _script.Length > 0 && _script[0] == (byte) OpcodeType.OP_RETURN;

		public bool IsValid
		{
			get { return ToOps().All(o => !o.IsInvalid); }
		}

		private static byte[] Parse(string script)
		{
			using (var reader = new StringReader(script.Trim()))
			{
				using (var result = new MemoryStream())
				{
					while (reader.Peek() != -1)
					{
						Op.Read(reader).WriteTo(result);
					}

					return result.ToArray();
				}
			}
		}

		public static Script FromBytesUnsafe(byte[] data)
		{
			return new Script(data, true, true);
		}

		/// <summary>
		///     Extract the ScriptCode delimited by the codeSeparatorIndex th OP_CODESEPARATOR.
		/// </summary>
		/// <param name="codeSeparatorIndex">Index of the OP_CODESEPARATOR, or -1 for fetching the whole script</param>
		/// <returns></returns>
		public Script ExtractScriptCode(int codeSeparatorIndex)
		{
			if (codeSeparatorIndex == -1)
			{
				return this;
			}

			if (codeSeparatorIndex < -1)
			{
				throw new ArgumentOutOfRangeException("codeSeparatorIndex");
			}

			var separatorIndex = -1;
			var ops = new List<Op>();
			foreach (var op in ToOps())
			{
				if (op.Code == OpcodeType.OP_CODESEPARATOR)
				{
					separatorIndex++;
				}

				if (separatorIndex >= codeSeparatorIndex &&
				    !(separatorIndex == codeSeparatorIndex && op.Code == OpcodeType.OP_CODESEPARATOR))
				{
					ops.Add(op);
				}
			}

			if (separatorIndex < codeSeparatorIndex)
			{
				throw new ArgumentOutOfRangeException(nameof(codeSeparatorIndex));
			}

			return new Script(ops.ToArray());
		}


		public ScriptReader CreateReader()
		{
			return new ScriptReader(_script);
		}

		private Script FindAndDelete(Op op)
		{
			return op == null
				? this
				: FindAndDelete(o => o.Code == op.Code && Utils.ArrayEqual(o.PushData, op.PushData));
		}

		internal Script FindAndDelete(byte[] pushedData)
		{
			if (pushedData.Length == 0)
			{
				return this;
			}

			var standardOp = Op.GetPushOp(pushedData);
			return FindAndDelete(op =>
				op.Code == standardOp.Code &&
				op.PushData != null && Utils.ArrayEqual(op.PushData, pushedData));
		}

		internal Script FindAndDelete(OpcodeType op)
		{
			return FindAndDelete(new Op
			{
				Code = op
			});
		}

		private Script FindAndDelete(Func<Op, bool> predicate)
		{
			var nFound = 0;
			var operations = new List<Op>();
			foreach (var op in ToOps())
			{
				var shouldDelete = predicate(op);
				if (!shouldDelete)
				{
					operations.Add(op);
				}
				else
				{
					nFound++;
				}
			}

			if (nFound == 0)
			{
				return this;
			}

			return new Script(operations);
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_script);
		}

		public override string ToString()
		{
			// by default StringBuilder capacity is 16 (too small)
			// 300 is enough for P2PKH
			var builder = new StringBuilder(300);
			var reader = new ScriptReader(_script);

			Op op;
			while ((op = reader.Read()) != null)
			{
				builder.Append(" ");
				builder.Append(op);
			}

			return builder.ToString().Trim();
		}

		public static Script operator +(Script a, IEnumerable<byte> bytes)
		{
			if (a == null)
			{
				return new Script(Op.GetPushOp(bytes.ToArray()));
			}

			return a + Op.GetPushOp(bytes.ToArray());
		}

		public static Script operator +(Script a, Op op)
		{
			return a == null ? new Script(op) : new Script(a._script.Concat(op.ToBytes()));
		}

		public static Script operator +(Script a, IEnumerable<Op> ops)
		{
			return a == null ? new Script(ops) : new Script(a._script.Concat(new Script(ops)._script));
		}

		public IEnumerable<Op> ToOps()
		{
			var reader = new ScriptReader(_script);
			return reader.ToEnumerable();
		}

		public uint GetSigOpCount(bool fAccurate)
		{
			uint n = 0;
			Op lastOpcode = null;
			foreach (var op in ToOps())
			{
				if (op.Code == OpcodeType.OP_CHECKSIG || op.Code == OpcodeType.OP_CHECKSIGVERIFY)
				{
					n++;
				}
				else if (op.Code == OpcodeType.OP_CHECKMULTISIG || op.Code == OpcodeType.OP_CHECKMULTISIGVERIFY)
				{
					if (fAccurate && lastOpcode != null && lastOpcode.Code >= OpcodeType.OP_1 &&
					    lastOpcode.Code <= OpcodeType.OP_16)
					{
						n += lastOpcode.PushData == null || lastOpcode.PushData.Length == 0
							? 0U
							: lastOpcode.PushData[0];
					}
					else
					{
						n += 20;
					}
				}

				lastOpcode = op;
			}

			return n;
		}

		public BitcoinScriptAddress GetScriptAddress(Network network)
		{
			return (BitcoinScriptAddress) Hash.GetAddress(network);
		}

		public uint GetSigOpCount(Script scriptSig)
		{
			if (!IsPayToScriptHash)
			{
				return GetSigOpCount(true);
			}

			// This is a pay-to-script-hash scriptPubKey;
			// get the last item that the scriptSig
			// pushes onto the stack:
			var validSig = new PayToScriptHashTemplate().CheckScriptSig(scriptSig, this);
			return !validSig ? 0 : new Script(scriptSig.ToOps().Last().PushData).GetSigOpCount(true);
			// ... and return its opcount:
		}

		public ScriptTemplate FindTemplate()
		{
			return StandardScripts.GetTemplateFromScriptPubKey(this);
		}

		/// <summary>
		///     Extract P2SH or P2PH address from scriptSig
		/// </summary>
		/// <param name="network">The network</param>
		/// <returns></returns>
		public BitcoinAddress GetSignerAddress(Network network)
		{
			var sig = GetSigner();
			return sig == null ? null : sig.GetAddress(network);
		}

		/// <summary>
		///     Extract P2SH or P2PH id from scriptSig
		/// </summary>
		/// <returns>The network</returns>
		public TxDestination GetSigner()
		{
			var pubKey = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(this);
			if (pubKey != null)
			{
				return pubKey.PublicKey.Hash;
			}

			var p2sh = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(this);
			return p2sh != null ? p2sh.RedeemScript.Hash : null;
		}

		/// <summary>
		///     Extract P2SH/P2PH/P2WSH/P2WPKH address from scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public BitcoinAddress GetDestinationAddress(Network network)
		{
			var dest = GetDestination();
			return dest == null ? null : dest.GetAddress(network);
		}

		/// <summary>
		///     Extract P2SH/P2PH/P2WSH/P2WPKH id from scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public TxDestination GetDestination()
		{
			var pubKeyHashParams = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if (pubKeyHashParams != null)
			{
				return pubKeyHashParams;
			}

			var scriptHashParams = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if (scriptHashParams != null)
			{
				return scriptHashParams;
			}

			return null;
		}

		/// <summary>
		///     Extract public keys if this script is a multi sig or pay to pub key scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public PubKey[] GetDestinationPublicKeys()
		{
			var result = new List<PubKey>();
			var single = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if (single != null)
			{
				result.Add(single);
			}
			else
			{
				var multiSig = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(this);
				if (multiSig != null)
				{
					result.AddRange(multiSig.PubKeys);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		///     Get script byte array
		/// </summary>
		/// <returns></returns>
		public byte[] ToBytes()
		{
			return ToBytes(false);
		}

		/// <summary>
		///     Get script byte array
		/// </summary>
		/// <param name="unsafe">if false, returns a copy of the internal byte array</param>
		/// <returns></returns>
		public byte[] ToBytes(bool @unsafe)
		{
			return @unsafe ? _script : _script.ToArray();
		}

		public byte[] ToCompressedBytes()
		{
			var compressor = new ScriptCompressor(this);
			return compressor.ToBytes();
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i,
			ScriptVerify scriptVerify = ScriptVerify.Standard, SigHash sigHash = SigHash.Undefined)
		{
			ScriptError unused;
			return VerifyScript(scriptSig, scriptPubKey, tx, i, null, scriptVerify, sigHash, out unused);
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i, Money value,
			ScriptVerify scriptVerify = ScriptVerify.Standard, SigHash sigHash = SigHash.Undefined)
		{
			ScriptError unused;
			return VerifyScript(scriptSig, scriptPubKey, tx, i, value, scriptVerify, sigHash, out unused);
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i, Money value,
			out ScriptError error)
		{
			return VerifyScript(scriptSig, scriptPubKey, tx, i, value, ScriptVerify.Standard, SigHash.Undefined,
				out error);
		}

		public static bool VerifyScript(Script scriptPubKey, Transaction tx, int i, Money value,
			ScriptVerify scriptVerify = ScriptVerify.Standard, SigHash sigHash = SigHash.Undefined)
		{
			ScriptError unused;
			var scriptSig = tx.Inputs[i].ScriptSig;
			return VerifyScript(scriptSig, scriptPubKey, tx, i, value, scriptVerify, sigHash, out unused);
		}

		public static bool VerifyScript(Script scriptPubKey, Transaction tx, int i, Money value, out ScriptError error)
		{
			var scriptSig = tx.Inputs[i].ScriptSig;
			return VerifyScript(scriptSig, scriptPubKey, tx, i, value, ScriptVerify.Standard, SigHash.Undefined,
				out error);
		}

		public static bool VerifyScript(Script scriptPubKey, Transaction tx, int i, Money value,
			ScriptVerify scriptVerify, SigHash sigHash, out ScriptError error)
		{
			var scriptSig = tx.Inputs[i].ScriptSig;
			return VerifyScript(scriptSig, scriptPubKey, tx, i, value, scriptVerify, sigHash, out error);
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i, Money value,
			ScriptVerify scriptVerify, SigHash sigHash, out ScriptError error)
		{
			var eval = new ScriptEvaluationContext
			{
				SigHash = sigHash,
				ScriptVerify = scriptVerify
			};
			var result = eval.VerifyScript(scriptSig, scriptPubKey, tx, i, value);
			error = eval.Error;
			return result;
		}

		public static bool IsNullOrEmpty(Script script)
		{
			return script == null || script._script.Length == 0;
		}

		public override bool Equals(object obj)
		{
			var item = obj as Script;
			return item != null && Utils.ArrayEqual(item._script, _script);
		}

		public static bool operator ==(Script a, Script b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if ((object) a == null || (object) b == null)
			{
				return false;
			}

			return Utils.ArrayEqual(a._script, b._script);
		}

		public static bool operator !=(Script a, Script b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(_script);
		}

		public Script Clone()
		{
			return new Script(_script);
		}

		public static Script CombineSignatures(Script scriptPubKey, Transaction transaction, int n, Script scriptSig1,
			Script scriptSig2)
		{
			return CombineSignatures(scriptPubKey, new TransactionChecker(transaction, n), new ScriptSigs
			{
				ScriptSig = scriptSig1
			}, new ScriptSigs
			{
				ScriptSig = scriptSig2
			}).ScriptSig;
		}

		public static ScriptSigs CombineSignatures(Script scriptPubKey, TransactionChecker checker, ScriptSigs input1,
			ScriptSigs input2)
		{
			if (scriptPubKey == null)
			{
				scriptPubKey = new Script();
			}

			var scriptSig1 = input1.ScriptSig;
			var scriptSig2 = input2.ScriptSig;

			var context = new ScriptEvaluationContext {ScriptVerify = ScriptVerify.StrictEnc};
			context.EvalScript(scriptSig1, checker);

			var stack1 = context.Stack.AsInternalArray();
			context = new ScriptEvaluationContext {ScriptVerify = ScriptVerify.StrictEnc};
			context.EvalScript(scriptSig2, checker);

			var stack2 = context.Stack.AsInternalArray();
			var result = CombineSignatures(scriptPubKey, checker, stack1, stack2);
			if (result == null)
			{
				return scriptSig1.Length < scriptSig2.Length ? input2 : input1;
			}

			return new ScriptSigs
			{
				ScriptSig = result
			};
		}

		private static Script CombineSignatures(Script scriptPubKey, TransactionChecker checker, byte[][] sigs1,
			byte[][] sigs2)
		{
			var template = StandardScripts.GetTemplateFromScriptPubKey(scriptPubKey);

			if (template is PayToPubkeyHashTemplate)
			{
				scriptPubKey = new KeyId(scriptPubKey.ToBytes(true).SafeSubArray(1, 20)).ScriptPubKey;
				template = StandardScripts.GetTemplateFromScriptPubKey(scriptPubKey);
			}

			if (template == null || template is TxNullDataTemplate)
			{
				return PushAll(Max(sigs1, sigs2));
			}

			if (template is PayToPubkeyTemplate || template is PayToPubkeyHashTemplate)
			{
				if (sigs1.Length == 0 || sigs1[0].Length == 0)
				{
					return PushAll(sigs2);
				}

				return PushAll(sigs1);
			}

			if (template is PayToScriptHashTemplate)
			{
				if (sigs1.Length == 0 || sigs1[sigs1.Length - 1].Length == 0)
				{
					return PushAll(sigs2);
				}

				if (sigs2.Length == 0 || sigs2[sigs2.Length - 1].Length == 0)
				{
					return PushAll(sigs1);
				}

				var redeemBytes = sigs1[sigs1.Length - 1];
				var redeem = new Script(redeemBytes);
				sigs1 = sigs1.Take(sigs1.Length - 1).ToArray();
				sigs2 = sigs2.Take(sigs2.Length - 1).ToArray();
				var result = CombineSignatures(redeem, checker, sigs1, sigs2);
				result += Op.GetPushOp(redeemBytes);
				return result;
			}

			if (template is PayToMultiSigTemplate)
			{
				return CombineMultisig(scriptPubKey, checker, sigs1, sigs2);
			}

			return null;
		}

		private static Script CombineMultisig(Script scriptPubKey, TransactionChecker checker, byte[][] sigs1,
			byte[][] sigs2)
		{
			// Combine all the signatures we've got:
			var allsigs = new List<TransactionSignature>();
			foreach (var v in sigs1)
			{
				if (TransactionSignature.IsValid(v))
				{
					allsigs.Add(new TransactionSignature(v));
				}
			}


			foreach (var v in sigs2)
			{
				if (TransactionSignature.IsValid(v))
				{
					allsigs.Add(new TransactionSignature(v));
				}
			}

			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if (multiSigParams == null)
			{
				throw new InvalidOperationException("The scriptPubKey is not a valid multi sig");
			}

			var sigs = new Dictionary<PubKey, TransactionSignature>();

			foreach (var sig in allsigs)
			{
				foreach (var pubkey in multiSigParams.PubKeys)
				{
					if (sigs.ContainsKey(pubkey))
					{
						continue; // Already got a sig for this pubkey
					}

					var eval = new ScriptEvaluationContext();
					if (eval.CheckSig(sig, pubkey, scriptPubKey, checker))
					{
						sigs.AddOrReplace(pubkey, sig);
					}
				}
			}


			// Now build a merged CScript:
			var nSigsHave = 0;
			var result = new Script(OpcodeType.OP_0); // pop-one-too-many workaround
			foreach (var pubkey in multiSigParams.PubKeys)
			{
				if (sigs.ContainsKey(pubkey))
				{
					result += Op.GetPushOp(sigs[pubkey].ToBytes());
					nSigsHave++;
				}

				if (nSigsHave >= multiSigParams.SignatureCount)
				{
					break;
				}
			}

			// Fill any missing with OP_0:
			for (var i = nSigsHave; i < multiSigParams.SignatureCount; i++)
			{
				result += OpcodeType.OP_0;
			}

			return result;
		}

		private static Script PushAll(byte[][] stack)
		{
			var s = new Script();
			foreach (var push in stack)
			{
				s += Op.GetPushOp(push);
			}

			return s;
		}

		private static byte[][] Max(byte[][] scriptSig1, byte[][] scriptSig2)
		{
			return scriptSig1.Length >= scriptSig2.Length ? scriptSig1 : scriptSig2;
		}


#if !NOCONSENSUSLIB

		public const string LibConsensusDll = "libbitcoinconsensus.dll";

		public enum BitcoinConsensusError
		{
			ERR_OK = 0,
			ERR_TX_INDEX,
			ERR_TX_SIZE_MISMATCH,
			ERR_TX_DESERIALIZE,
			ERR_AMOUNT_REQUIRED
		}

		/// Returns 1 if the input nIn of the serialized transaction pointed to by
		/// txTo correctly spends the scriptPubKey pointed to by scriptPubKey under
		/// the additional constraints specified by flags.
		/// If not NULL, err will contain an error/success code for the operation
		[DllImport(LibConsensusDll, EntryPoint = "bitcoinconsensus_verify_script",
			CallingConvention = CallingConvention.Cdecl)]
		private static extern int VerifyScriptConsensus(byte[] scriptPubKey, uint scriptPubKeyLen, byte[] txTo,
			uint txToLen, uint nIn, ScriptVerify flags, ref BitcoinConsensusError err);

		[DllImport(LibConsensusDll, EntryPoint = "bitcoinconsensus_verify_script_with_amount",
			CallingConvention = CallingConvention.Cdecl)]
		private static extern int VerifyScriptConsensusWithAmount(byte[] scriptPubKey, uint scriptPubKeyLen,
			long amount, byte[] txTo, uint txToLen, uint nIn, ScriptVerify flags, ref BitcoinConsensusError err);

		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, ScriptVerify flags)
		{
			var err = BitcoinConsensusError.ERR_OK;
			return VerifyScriptConsensus(scriptPubKey, tx, nIn, flags, out err);
		}

		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, Money amount,
			ScriptVerify flags)
		{
			var err = BitcoinConsensusError.ERR_OK;
			return VerifyScriptConsensus(scriptPubKey, tx, nIn, amount, flags, out err);
		}

		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, ScriptVerify flags,
			out BitcoinConsensusError err)
		{
			var scriptPubKeyBytes = scriptPubKey.ToBytes();
			var txToBytes = tx.ToBytes();
			err = BitcoinConsensusError.ERR_OK;
			var valid = VerifyScriptConsensus(scriptPubKeyBytes, (uint) scriptPubKeyBytes.Length, txToBytes,
				(uint) txToBytes.Length, nIn, flags, ref err);
			return valid == 1;
		}

		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, Money amount,
			ScriptVerify flags, out BitcoinConsensusError err)
		{
			var scriptPubKeyBytes = scriptPubKey.ToBytes();
			var txToBytes = tx.ToBytes();
			err = BitcoinConsensusError.ERR_OK;
			var valid = VerifyScriptConsensusWithAmount(scriptPubKeyBytes, (uint) scriptPubKeyBytes.Length,
				amount.Satoshi, txToBytes, (uint) txToBytes.Length, nIn, flags, ref err);
			return valid == 1;
		}
#endif
	}
}