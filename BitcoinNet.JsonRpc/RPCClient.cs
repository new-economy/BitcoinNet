﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using BitcoinNet.DataEncoders;
using BitcoinNet.Protocol;
using BitcoinNet.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitcoinNet.JsonRpc
{
	/*
		Category			Name						Implemented 
		------------------ --------------------------- -----------------------
		------------------ Overall control/query calls 
		control			getinfo
		control			help
		control			stop

		------------------ P2P networking
		network			getnetworkinfo
		network			addnode					  Yes
		network			disconnectnode
		network			getaddednodeinfo			 Yes
		network			getconnectioncount
		network			getnettotals
		network			getpeerinfo				  Yes
		network			ping
		network			setban
		network			listbanned
		network			clearbanned

		------------------ Block chain and UTXO
		blockchain		 getblockchaininfo			Yes
		blockchain		 getbestblockhash			 Yes
		blockchain		 getblockcount				Yes
		blockchain		 getblock					 Yes
		blockchain		 getblockhash				 Yes
		blockchain		 getchaintips
		blockchain		 getdifficulty
		blockchain		 getmempoolinfo
		blockchain		 getrawmempool				Yes
		blockchain		 gettxout					Yes
		blockchain		 gettxoutproof
		blockchain		 verifytxoutproof
		blockchain		 gettxoutsetinfo
		blockchain		 verifychain

		------------------ Mining
		mining			 getblocktemplate
		mining			 getmininginfo
		mining			 getnetworkhashps
		mining			 prioritisetransaction
		mining			 submitblock

		------------------ Coin generation
		generating		 getgenerate
		generating		 setgenerate
		generating		 generate

		------------------ Raw transactions
		rawtransactions	createrawtransaction
		rawtransactions	decoderawtransaction
		rawtransactions	decodescript
		rawtransactions	getrawtransaction
		rawtransactions	sendrawtransaction
		rawtransactions	signrawtransaction
		rawtransactions	fundrawtransaction

		------------------ Utility functions
		util			createmultisig
		util			validateaddress
		util			verifymessage
		util			estimatefee				  Yes
		util			estimatesmartfee			  Yes
		------------------ Not shown in help
		hidden			invalidateblock				Yes
		hidden			reconsiderblock
		hidden			setmocktime
		hidden			resendwallettransactions
	*/
	public class RPCClient : IBlockRepository
	{
		private static readonly ConcurrentDictionary<Network, string> DefaultPaths =
			new ConcurrentDictionary<Network, string>();

		private ConcurrentQueue<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>> _batchedRequests;


		static RPCClient()
		{
			var home = Environment.GetEnvironmentVariable("HOME");
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");

			if (string.IsNullOrEmpty(home) && string.IsNullOrEmpty(localAppData))
			{
				return;
			}

			if (!string.IsNullOrEmpty(home))
			{
				var bitcoinFolder = Path.Combine(home, ".bitcoin");

				var mainnet = Path.Combine(bitcoinFolder, ".cookie");
				RegisterDefaultCookiePath(Network.Main, mainnet);

				var testnet = Path.Combine(bitcoinFolder, "testnet3", ".cookie");
				RegisterDefaultCookiePath(Network.TestNet, testnet);

				var regtest = Path.Combine(bitcoinFolder, "regtest", ".cookie");
				RegisterDefaultCookiePath(Network.RegTest, regtest);
			}
			else if (!string.IsNullOrEmpty(localAppData))
			{
				var bitcoinFolder = Path.Combine(localAppData, "Bitcoin");

				var mainnet = Path.Combine(bitcoinFolder, ".cookie");
				RegisterDefaultCookiePath(Network.Main, mainnet);

				var testnet = Path.Combine(bitcoinFolder, "testnet3", ".cookie");
				RegisterDefaultCookiePath(Network.TestNet, testnet);

				var regtest = Path.Combine(bitcoinFolder, "regtest", ".cookie");
				RegisterDefaultCookiePath(Network.RegTest, regtest);
			}
		}

		/// <summary>
		///     Use default bitcoin parameters to configure a RPCClient.
		/// </summary>
		/// <param name="network">The network used by the node. Must not be null.</param>
		public RPCClient(Network network) : this(null as string, BuildUri(null, null, network.RPCPort), network)
		{
		}

		public RPCClient(RPCCredentialString credentials, Network network)
			: this(credentials, null as string, network)
		{
		}

		public RPCClient(RPCCredentialString credentials, string host, Network network)
			: this(credentials, BuildUri(host, credentials.ToString(), network.RPCPort), network)
		{
		}

		public RPCClient(RPCCredentialString credentials, Uri address, Network network)
		{
			credentials = credentials ?? new RPCCredentialString();

			if (address != null && network == null)
			{
				network = Network.GetNetworks().FirstOrDefault(n => n.RPCPort == address.Port);
				if (network == null)
				{
					throw new ArgumentNullException(nameof(network));
				}
			}

			if (credentials.UseDefault && network == null)
			{
				throw new ArgumentException("network parameter is required if you use default credentials");
			}

			if (address == null && network == null)
			{
				throw new ArgumentException("network parameter is required if you use default uri");
			}

			if (address == null)
			{
				address = new Uri("http://127.0.0.1:" + network.RPCPort + "/");
			}


			if (credentials.UseDefault)
			{
				//will throw impossible to get the cookie path
				GetDefaultCookieFilePath(network);
			}

			CredentialString = credentials;
			Address = address;
			Network = network;

			if (credentials.UserPassword != null)
			{
				Authentication = $"{credentials.UserPassword.UserName}:{credentials.UserPassword.Password}";
			}

			if (Authentication == null)
			{
				TryRenewCookie(null);
			}
		}

		/// <summary>
		///     Create a new RPCClient instance
		/// </summary>
		/// <param name="authenticationString">username:password, the content of the .cookie file, or cookiefile=pathToCookieFile</param>
		/// <param name="hostOrUri"></param>
		/// <param name="network"></param>
		public RPCClient(string authenticationString, string hostOrUri, Network network)
			: this(authenticationString, BuildUri(hostOrUri, authenticationString, network.RPCPort), network)
		{
		}

		public RPCClient(NetworkCredential credentials, Uri address, Network network = null)
			: this(credentials == null ? null : credentials.UserName + ":" + credentials.Password, address, network)
		{
		}

		/// <summary>
		///     Create a new RPCClient instance
		/// </summary>
		/// <param name="authenticationString">username:password or the content of the .cookie file or null to auto configure</param>
		/// <param name="address"></param>
		/// <param name="network"></param>
		public RPCClient(string authenticationString, Uri address, Network network = null)
			: this(authenticationString == null ? null : RPCCredentialString.Parse(authenticationString), address,
				network)
		{
		}

		public Uri Address { get; }

		public RPCCredentialString CredentialString { get; }

		public Network Network { get; }

		public string Authentication { get; private set; }

		/// <summary>
		///     Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public async Task<Block> GetBlockAsync(uint256 blockId)
		{
			var resp = await SendCommandAsync(RPCOperations.getblock, blockId, false).ConfigureAwait(false);
			return Block.Parse(resp.Result.ToString(), Network);
		}

		public static void RegisterDefaultCookiePath(Network network, string path)
		{
			DefaultPaths.TryAdd(network, path);
		}


		private string GetCookiePath()
		{
			if (CredentialString.UseDefault && Network == null)
			{
				throw new InvalidOperationException("BitcoinNet bug, report to the developers.");
			}

			if (CredentialString.UseDefault)
			{
				return GetDefaultCookieFilePath(Network);
			}

			if (CredentialString.CookieFile != null)
			{
				return CredentialString.CookieFile;
			}

			return null;
		}

		public static string GetDefaultCookieFilePath(Network network)
		{
			string path = null;
			if (!DefaultPaths.TryGetValue(network, out path))
			{
				throw new ArgumentException(
					"This network has no default cookie file path registered, use RPCClient.RegisterDefaultCookiePath to register",
					nameof(network));
			}

			return path;
		}

		public static string TryGetDefaultCookieFilePath(Network network)
		{
			string path = null;
			if (!DefaultPaths.TryGetValue(network, out path))
			{
				return null;
			}

			return path;
		}

		private static Uri BuildUri(string hostOrUri, string connectionString, int port)
		{
			RPCCredentialString connString;
			if (connectionString != null && RPCCredentialString.TryParse(connectionString, out connString))
			{
				if (connString.Server != null)
				{
					hostOrUri = connString.Server;
				}
			}

			if (hostOrUri != null)
			{
				hostOrUri = hostOrUri.Trim();
				try
				{
					if (hostOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
					    hostOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
					{
						return new Uri(hostOrUri, UriKind.Absolute);
					}
				}
				catch
				{
				}
			}

			hostOrUri = hostOrUri ?? "127.0.0.1";
			var indexOfPort = hostOrUri.IndexOf(":");
			if (indexOfPort != -1)
			{
				port = int.Parse(hostOrUri.Substring(indexOfPort + 1));
				hostOrUri = hostOrUri.Substring(0, indexOfPort);
			}

			var builder = new UriBuilder {Host = hostOrUri, Scheme = "http", Port = port};
			return builder.Uri;
		}

		public RPCClient PrepareBatch()
		{
			return new RPCClient(CredentialString, Address, Network)
			{
				_batchedRequests = new ConcurrentQueue<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>>()
			};
		}

		public RPCResponse SendCommand(RPCOperations commandName, params object[] parameters)
		{
			return SendCommand(commandName.ToString(), parameters);
		}

		public Task<RPCResponse> SendCommandAsync(RPCOperations commandName, params object[] parameters)
		{
			return SendCommandAsync(commandName.ToString(), parameters);
		}

		/// <summary>
		///     Send a command
		/// </summary>
		/// <param name="commandName">https://en.bitcoin.it/wiki/Original_Bitcoin_client/API_calls_list</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public RPCResponse SendCommand(string commandName, params object[] parameters)
		{
			return SendCommand(new RPCRequest(commandName, parameters));
		}

		public Task<RPCResponse> SendCommandAsync(string commandName, params object[] parameters)
		{
			return SendCommandAsync(new RPCRequest(commandName, parameters));
		}

		public RPCResponse SendCommand(RPCRequest request, bool throwIfRPCError = true)
		{
			return SendCommandAsync(request, throwIfRPCError).GetAwaiter().GetResult();
		}

		/// <summary>
		///     Send all commands in one batch
		/// </summary>
		public void SendBatch()
		{
			SendBatchAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		///     Cancel all commands
		/// </summary>
		public void CancelBatch()
		{
			var batches = _batchedRequests;
			if (batches == null)
			{
				throw new InvalidOperationException("This RPCClient instance is not a batch, use PrepareBatch");
			}

			_batchedRequests = null;
			while (batches.TryDequeue(out var req))
			{
				req.Item2.TrySetCanceled();
			}
		}

		/// <summary>
		///     Send all commands in one batch
		/// </summary>
		public async Task SendBatchAsync()
		{
			var requests = new List<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>>();
			var batches = _batchedRequests;
			if (batches == null)
			{
				throw new InvalidOperationException("This RPCClient instance is not a batch, use PrepareBatch");
			}

			_batchedRequests = null;
			while (batches.TryDequeue(out var req))
			{
				requests.Add(req);
			}

			if (requests.Count == 0)
			{
				return;
			}

			try
			{
				await SendBatchAsyncCore(requests).ConfigureAwait(false);
			}
			catch (WebException ex)
			{
				if (!IsUnauthorized(ex))
				{
					throw;
				}

				if (GetCookiePath() == null)
				{
					throw;
				}

				TryRenewCookie(ex);
				await SendBatchAsyncCore(requests).ConfigureAwait(false);
			}
		}

		private async Task SendBatchAsyncCore(List<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>> requests)
		{
			var writer = new StringWriter();
			writer.Write("[");
			var first = true;
			foreach (var item in requests)
			{
				if (!first)
				{
					writer.Write(",");
				}

				first = false;
				item.Item1.WriteJSON(writer);
			}

			writer.Write("]");
			writer.Flush();

			var json = writer.ToString();
			var bytes = Encoding.UTF8.GetBytes(json);

			var webRequest = CreateWebRequest();

			var responseIndex = 0;
			var dataStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false);
			await dataStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
			await dataStream.FlushAsync().ConfigureAwait(false);
			dataStream.Dispose();
			JArray response;
			WebResponse webResponse = null;
			WebResponse errorResponse = null;
			try
			{
				webResponse = await webRequest.GetResponseAsync().ConfigureAwait(false);
				response = JArray.Load(new JsonTextReader(
					new StreamReader(
						await ToMemoryStreamAsync(webResponse.GetResponseStream()).ConfigureAwait(false),
						Encoding.UTF8)));
				foreach (var jobj in response.OfType<JObject>())
				{
					try
					{
						var rpcResponse = new RPCResponse(jobj);
						requests[responseIndex].Item2.TrySetResult(rpcResponse);
					}
					catch (Exception ex)
					{
						requests[responseIndex].Item2.TrySetException(ex);
					}

					responseIndex++;
				}
			}
			catch (WebException ex)
			{
				if (IsUnauthorized(ex))
				{
					throw;
				}

				if (ex.Response == null || ex.Response.ContentLength == 0
				                        || !ex.Response.ContentType.Equals("application/json",
					                        StringComparison.Ordinal))
				{
					foreach (var item in requests)
					{
						item.Item2.TrySetException(ex);
					}
				}
				else
				{
					errorResponse = ex.Response;
					try
					{
						var rpcResponse = RPCResponse.Load(await ToMemoryStreamAsync(errorResponse.GetResponseStream())
							.ConfigureAwait(false));
						foreach (var item in requests)
						{
							item.Item2.TrySetResult(rpcResponse);
						}
					}
					catch (Exception)
					{
						foreach (var item in requests)
						{
							item.Item2.TrySetException(ex);
						}
					}
				}
			}
			catch (Exception ex)
			{
				foreach (var item in requests)
				{
					item.Item2.TrySetException(ex);
				}
			}
			finally
			{
				if (errorResponse != null)
				{
					errorResponse.Dispose();
					errorResponse = null;
				}

				if (webResponse != null)
				{
					webResponse.Dispose();
					webResponse = null;
				}
			}
		}

		private static bool IsUnauthorized(WebException ex)
		{
			var httpResp = ex.Response as HttpWebResponse;
			var isUnauthorized = httpResp != null && httpResp.StatusCode == HttpStatusCode.Unauthorized;
			return isUnauthorized;
		}

		public async Task<RPCResponse> SendCommandAsync(RPCRequest request, bool throwIfRPCError = true)
		{
			try
			{
				return await SendCommandAsyncCore(request, throwIfRPCError).ConfigureAwait(false);
			}
			catch (WebException ex)
			{
				if (!IsUnauthorized(ex))
				{
					throw;
				}

				if (GetCookiePath() == null)
				{
					throw;
				}

				TryRenewCookie(ex);
				return await SendCommandAsyncCore(request, throwIfRPCError).ConfigureAwait(false);
			}
		}

		private void TryRenewCookie(WebException ex)
		{
			if (GetCookiePath() == null)
			{
				throw new InvalidOperationException("Bug in BitcoinNet notify the developers.");
			}

			try
			{
				Authentication = File.ReadAllText(GetCookiePath());
			}
			//We are only interested into the previous exception
			catch
			{
				if (ex == null)
				{
					return;
				}

				ExceptionDispatchInfo.Capture(ex).Throw();
			}
		}

		private async Task<RPCResponse> SendCommandAsyncCore(RPCRequest request, bool throwIfRPCError)
		{
			RPCResponse response = null;
			var batches = _batchedRequests;
			if (batches != null)
			{
				var source = new TaskCompletionSource<RPCResponse>();
				batches.Enqueue(Tuple.Create(request, source));
				response = await source.Task.ConfigureAwait(false);
			}

			var webRequest = response == null ? CreateWebRequest() : null;
			if (response == null)
			{
				var writer = new StringWriter();
				request.WriteJSON(writer);
				writer.Flush();
				var json = writer.ToString();
				var bytes = Encoding.UTF8.GetBytes(json);

				var dataStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false);
				await dataStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
				await dataStream.FlushAsync().ConfigureAwait(false);
				dataStream.Dispose();
			}

			WebResponse webResponse = null;
			WebResponse errorResponse = null;
			try
			{
				webResponse = response == null ? await webRequest.GetResponseAsync().ConfigureAwait(false) : null;
				response = response ?? RPCResponse.Load(await ToMemoryStreamAsync(webResponse.GetResponseStream())
					           .ConfigureAwait(false));

				if (throwIfRPCError)
				{
					response.ThrowIfError();
				}
			}
			catch (WebException ex)
			{
				if (ex.Response == null || ex.Response.ContentLength == 0 ||
				    !ex.Response.ContentType.Equals("application/json", StringComparison.Ordinal))
				{
					throw;
				}

				errorResponse = ex.Response;
				response = RPCResponse.Load(await ToMemoryStreamAsync(errorResponse.GetResponseStream())
					.ConfigureAwait(false));
				if (throwIfRPCError)
				{
					response.ThrowIfError();
				}
			}
			finally
			{
				if (errorResponse != null)
				{
					errorResponse.Dispose();
					errorResponse = null;
				}

				if (webResponse != null)
				{
					webResponse.Dispose();
					webResponse = null;
				}
			}

			return response;
		}

		private HttpWebRequest CreateWebRequest()
		{
			var address = Address.AbsoluteUri;
			var webRequest = (HttpWebRequest) WebRequest.Create(address);
			webRequest.Headers[HttpRequestHeader.Authorization] =
				"Basic " + Encoders.Base64.EncodeData(Encoders.ASCII.DecodeData(Authentication));
			webRequest.ContentType = "application/json-rpc";
			webRequest.Method = "POST";
			return webRequest;
		}

		private async Task<Stream> ToMemoryStreamAsync(Stream stream)
		{
			var ms = new MemoryStream();
			await stream.CopyToAsync(ms).ConfigureAwait(false);
			ms.Position = 0;
			return ms;
		}

		// P2P Networking

		public PeerInfo[] GetPeersInfo()
		{
			PeerInfo[] peers = null;

			peers = GetPeersInfoAsync().GetAwaiter().GetResult();
			return peers;
		}

		public async Task<PeerInfo[]> GetPeersInfoAsync()
		{
			var resp = await SendCommandAsync(RPCOperations.getpeerinfo).ConfigureAwait(false);
			var peers = resp.Result as JArray;
			var result = new PeerInfo[peers.Count];
			var i = 0;
			foreach (var peer in peers)
			{
				var localAddr = (string) peer["addrlocal"];
				var pingWait = peer["pingwait"] != null ? (double) peer["pingwait"] : 0;

				localAddr = string.IsNullOrEmpty(localAddr) ? "127.0.0.1:8333" : localAddr;

				ulong services;
				if (!ulong.TryParse((string) peer["services"], out services))
				{
					services = Utils.ToUInt64(Encoders.Hex.DecodeData((string) peer["services"]), false);
				}

				result[i++] = new PeerInfo
				{
					Id = (int) peer["id"],
					Address = Utils.ParseIpEndpoint((string) peer["addr"], Network.DefaultPort),
					LocalAddress = Utils.ParseIpEndpoint(localAddr, Network.DefaultPort),
					Services = (NodeServices) services,
					LastSend = Utils.UnixTimeToDateTime((uint) peer["lastsend"]),
					LastReceive = Utils.UnixTimeToDateTime((uint) peer["lastrecv"]),
					BytesSent = (long) peer["bytessent"],
					BytesReceived = (long) peer["bytesrecv"],
					ConnectionTime = Utils.UnixTimeToDateTime((uint) peer["conntime"]),
					TimeOffset = TimeSpan.FromSeconds(Math.Min(int.MaxValue, (long) peer["timeoffset"])),
					PingTime = peer["pingtime"] == null
						? (TimeSpan?) null
						: TimeSpan.FromSeconds((double) peer["pingtime"]),
					PingWait = TimeSpan.FromSeconds(pingWait),
					Blocks = peer["blocks"] != null ? (int) peer["blocks"] : -1,
					Version = (int) peer["version"],
					SubVersion = (string) peer["subver"],
					Inbound = (bool) peer["inbound"],
					StartingHeight = (int) peer["startingheight"],
					SynchronizedBlocks = (int) peer["synced_blocks"],
					SynchronizedHeaders = (int) peer["synced_headers"],
					IsWhiteListed = (bool) peer["whitelisted"],
					BanScore = peer["banscore"] == null ? 0 : (int) peer["banscore"],
					Inflight = peer["inflight"].Select(x => uint.Parse((string) x)).ToArray()
				};
			}

			return result;
		}

		public void AddNode(EndPoint nodeEndPoint, bool onetry = false)
		{
			if (nodeEndPoint == null)
			{
				throw new ArgumentNullException(nameof(nodeEndPoint));
			}

			SendCommand("addnode", nodeEndPoint.ToString(), onetry ? "onetry" : "add");
		}

		public async Task AddNodeAsync(EndPoint nodeEndPoint, bool onetry = false)
		{
			if (nodeEndPoint == null)
			{
				throw new ArgumentNullException(nameof(nodeEndPoint));
			}

			await SendCommandAsync(RPCOperations.addnode, nodeEndPoint.ToString(), onetry ? "onetry" : "add")
				.ConfigureAwait(false);
		}

		public void RemoveNode(EndPoint nodeEndPoint)
		{
			if (nodeEndPoint == null)
			{
				throw new ArgumentNullException(nameof(nodeEndPoint));
			}

			SendCommandAsync(RPCOperations.addnode, nodeEndPoint.ToString(), "remove");
		}

		public async Task RemoveNodeAsync(EndPoint nodeEndPoint)
		{
			if (nodeEndPoint == null)
			{
				throw new ArgumentNullException(nameof(nodeEndPoint));
			}

			await SendCommandAsync(RPCOperations.addnode, nodeEndPoint.ToString(), "remove").ConfigureAwait(false);
		}

		public async Task<AddedNodeInfo[]> GetAddedNodeInfoAsync(bool detailed)
		{
			var result = await SendCommandAsync(RPCOperations.getaddednodeinfo, detailed).ConfigureAwait(false);
			var obj = result.Result;
			return obj.Select(entry => new AddedNodeInfo
			{
				AddedNode = Utils.ParseIpEndpoint((string) entry["addednode"], 8333),
				Connected = (bool) entry["connected"],
				Addresses = entry["addresses"].Select(x => new NodeAddressInfo
				{
					Address = Utils.ParseIpEndpoint((string) x["address"], 8333),
					Connected = (bool) x["connected"]
				})
			}).ToArray();
		}

		public AddedNodeInfo[] GetAddedNodeInfo(bool detailed)
		{
			AddedNodeInfo[] addedNodesInfo = null;

			addedNodesInfo = GetAddedNodeInfoAsync(detailed).GetAwaiter().GetResult();
			return addedNodesInfo;
		}

		public AddedNodeInfo GetAddedNodeInfo(bool detailed, EndPoint nodeEndPoint)
		{
			AddedNodeInfo addedNodeInfo = null;

			addedNodeInfo = GetAddedNodeInfoAync(detailed, nodeEndPoint).GetAwaiter().GetResult();
			return addedNodeInfo;
		}

		public async Task<AddedNodeInfo> GetAddedNodeInfoAync(bool detailed, EndPoint nodeEndPoint)
		{
			if (nodeEndPoint == null)
			{
				throw new ArgumentNullException(nameof(nodeEndPoint));
			}

			try
			{
				var result = await SendCommandAsync(RPCOperations.getaddednodeinfo, detailed, nodeEndPoint.ToString())
					.ConfigureAwait(false);
				var e = result.Result;
				return e.Select(entry => new AddedNodeInfo
				{
					AddedNode = Utils.ParseIpEndpoint((string) entry["addednode"], 8333),
					Connected = (bool) entry["connected"],
					Addresses = entry["addresses"].Select(x => new NodeAddressInfo
					{
						Address = Utils.ParseIpEndpoint((string) x["address"], 8333),
						Connected = (bool) x["connected"]
					})
				}).FirstOrDefault();
			}
			catch (RPCException ex)
			{
				if (ex.RPCCode == RPCErrorCode.RPC_CLIENT_NODE_NOT_ADDED)
				{
					return null;
				}

				throw;
			}
		}

		// Block chain and UTXO

		public async Task<BlockchainInfo> GetBlockchainInfoAsync()
		{
			var response = await SendCommandAsync(RPCOperations.getblockchaininfo).ConfigureAwait(false);
			var result = response.Result;

			var epochToDtateTimeOffset = new Func<long, DateTimeOffset>(epoch =>
			{
				try
				{
					return Utils.UnixTimeToDateTime(epoch);
				}
				catch (OverflowException)
				{
					return DateTimeOffset.MaxValue;
				}
			});

			var blockchainInfo = new BlockchainInfo
			{
				Chain = Network.GetNetwork(result.Value<string>("chain")),
				Blocks = result.Value<ulong>("blocks"),
				Headers = result.Value<ulong>("headers"),
				BestBlockHash = new uint256(result.Value<string>("bestblockhash")), // the block hash
				Difficulty = result.Value<ulong>("difficulty"),
				MedianTime = result.Value<ulong>("mediantime"),
				VerificationProgress = result.Value<float>("verificationprogress"),
				ChainWork = new uint256(result.Value<string>("chainwork")),
				SizeOnDisk = result.Value<ulong>("size_on_disk"),
				Pruned = result.Value<bool>("pruned"),
				SoftForks = result["softforks"].Select(x =>
					new BlockchainInfo.SoftFork
					{
						Bip = (string) x["id"],
						Version = (int) x["version"],
						RejectStatus = bool.Parse((string) x["reject"]["status"])
					}).ToList()
			};

			return blockchainInfo;
		}

		public uint256 GetBestBlockHash()
		{
			return uint256.Parse((string) SendCommand(RPCOperations.getbestblockhash).Result);
		}

		public async Task<uint256> GetBestBlockHashAsync()
		{
			return uint256.Parse((string) (await SendCommandAsync(RPCOperations.getbestblockhash).ConfigureAwait(false))
				.Result);
		}

		public BlockHeader GetBlockHeader(int height)
		{
			var hash = GetBlockHash(height);
			return GetBlockHeader(hash);
		}

		public async Task<BlockHeader> GetBlockHeaderAsync(int height)
		{
			var hash = await GetBlockHashAsync(height).ConfigureAwait(false);
			return await GetBlockHeaderAsync(hash).ConfigureAwait(false);
		}

		/// <summary>
		///     Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public Block GetBlock(uint256 blockId)
		{
			return GetBlockAsync(blockId).GetAwaiter().GetResult();
		}

		public Block GetBlock(int height)
		{
			return GetBlockAsync(height).GetAwaiter().GetResult();
		}

		public async Task<Block> GetBlockAsync(int height)
		{
			var hash = await GetBlockHashAsync(height).ConfigureAwait(false);
			return await GetBlockAsync(hash).ConfigureAwait(false);
		}

		public BlockHeader GetBlockHeader(uint256 blockHash)
		{
			var resp = SendCommand("getblockheader", blockHash);
			return ParseBlockHeader(resp);
		}

		public async Task<BlockHeader> GetBlockHeaderAsync(uint256 blockHash)
		{
			var resp = await SendCommandAsync("getblockheader", blockHash).ConfigureAwait(false);
			return ParseBlockHeader(resp);
		}

		private BlockHeader ParseBlockHeader(RPCResponse resp)
		{
			var header = Network.Consensus.ConsensusFactory.CreateBlockHeader();
			header.Version = (int) resp.Result["version"];
			header.Nonce = (uint) resp.Result["nonce"];
			header.Bits = new Target(Encoders.Hex.DecodeData((string) resp.Result["bits"]));
			if (resp.Result["previousblockhash"] != null)
			{
				header.HashPrevBlock = uint256.Parse((string) resp.Result["previousblockhash"]);
			}

			if (resp.Result["time"] != null)
			{
				header.BlockTime = Utils.UnixTimeToDateTime((uint) resp.Result["time"]);
			}

			if (resp.Result["merkleroot"] != null)
			{
				header.HashMerkleRoot = uint256.Parse((string) resp.Result["merkleroot"]);
			}

			return header;
		}

		public uint256 GetBlockHash(int height)
		{
			var resp = SendCommand(RPCOperations.getblockhash, height);
			return uint256.Parse(resp.Result.ToString());
		}

		public async Task<uint256> GetBlockHashAsync(int height)
		{
			var resp = await SendCommandAsync(RPCOperations.getblockhash, height).ConfigureAwait(false);
			return uint256.Parse(resp.Result.ToString());
		}

		public int GetBlockCount()
		{
			return (int) SendCommand(RPCOperations.getblockcount).Result;
		}

		public async Task<int> GetBlockCountAsync()
		{
			return (int) (await SendCommandAsync(RPCOperations.getblockcount).ConfigureAwait(false)).Result;
		}

		public uint256[] GetRawMempool()
		{
			var result = SendCommand(RPCOperations.getrawmempool);
			var array = (JArray) result.Result;
			return array.Select(o => (string) o).Select(uint256.Parse).ToArray();
		}

		public async Task<uint256[]> GetRawMempoolAsync()
		{
			var result = await SendCommandAsync(RPCOperations.getrawmempool).ConfigureAwait(false);
			var array = (JArray) result.Result;
			return array.Select(o => (string) o).Select(uint256.Parse).ToArray();
		}

		/// <summary>
		///     Returns details about an unspent transaction output.
		/// </summary>
		/// <param name="txid">The transaction id</param>
		/// <param name="index">vout number</param>
		/// <param name="includeMempool">
		///     Whether to include the mempool. Note that an unspent output that is spent in the mempool
		///     won't appear.
		/// </param>
		/// <returns>null if spent or never existed</returns>
		public GetTxOutResponse GetTxOut(uint256 txid, int index, bool includeMempool = true)
		{
			return GetTxOutAsync(txid, index, includeMempool).GetAwaiter().GetResult();
		}

		/// <summary>
		///     Returns details about an unspent transaction output.
		/// </summary>
		/// <param name="txid">The transaction id</param>
		/// <param name="index">vout number</param>
		/// <param name="includeMempool">
		///     Whether to include the mempool. Note that an unspent output that is spent in the mempool
		///     won't appear.
		/// </param>
		/// <returns>null if spent or never existed</returns>
		public async Task<GetTxOutResponse> GetTxOutAsync(uint256 txid, int index, bool includeMempool = true)
		{
			var response = await SendCommandAsync(RPCOperations.gettxout, txid, index, includeMempool)
				.ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(response?.ResultString))
			{
				return null;
			}

			var result = response.Result;
			var value = result.Value<decimal>("value"); // The transaction value in BTC
			var txOut = new TxOut(Money.Coins(value), new Script(result["scriptPubKey"].Value<string>("asm")));

			return new GetTxOutResponse
			{
				BestBlock = new uint256(result.Value<string>("bestblock")), // the block hash
				Confirmations = result.Value<int>("confirmations"), // The number of confirmations
				IsCoinBase = result.Value<bool>("coinbase"), // Coinbase or not
				ScriptPubKeyType = result["scriptPubKey"].Value<string>("type"), // The type, eg pubkeyhash
				TxOut = txOut
			};
		}

		/// <summary>
		///     GetTransactions only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="blockHash"></param>
		/// <returns></returns>
		public IEnumerable<Transaction> GetTransactions(uint256 blockHash)
		{
			if (blockHash == null)
			{
				throw new ArgumentNullException(nameof(blockHash));
			}

			var resp = SendCommand(RPCOperations.getblock, blockHash);

			var tx = resp.Result["tx"] as JArray;
			if (tx != null)
			{
				foreach (var item in tx)
				{
					var result = GetRawTransaction(uint256.Parse(item.ToString()), false);
					if (result != null)
					{
						yield return result;
					}
				}
			}
		}

		public IEnumerable<Transaction> GetTransactions(int height)
		{
			return GetTransactions(GetBlockHash(height));
		}

		// Raw Transaction

		public Transaction DecodeRawTransaction(string rawHex)
		{
			var response = SendCommand(RPCOperations.decoderawtransaction, rawHex);
			return Network.GetFormatter(RawFormat.Satoshi).ParseJson(response.Result.ToString());
		}

		public Transaction DecodeRawTransaction(byte[] raw)
		{
			return DecodeRawTransaction(Encoders.Hex.EncodeData(raw));
		}

		public async Task<Transaction> DecodeRawTransactionAsync(string rawHex)
		{
			var response = await SendCommandAsync(RPCOperations.decoderawtransaction, rawHex).ConfigureAwait(false);
			return Network.GetFormatter(RawFormat.Satoshi).ParseJson(response.Result.ToString());
		}

		public Task<Transaction> DecodeRawTransactionAsync(byte[] raw)
		{
			return DecodeRawTransactionAsync(Encoders.Hex.EncodeData(raw));
		}

		/// <summary>
		///     getrawtransaction only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="txid"></param>
		/// <returns></returns>
		public Transaction GetRawTransaction(uint256 txid, bool throwIfNotFound = true)
		{
			return GetRawTransactionAsync(txid, throwIfNotFound).GetAwaiter().GetResult();
		}

		public async Task<Transaction> GetRawTransactionAsync(uint256 txid, bool throwIfNotFound = true)
		{
			var response =
				await SendCommandAsync(new RPCRequest(RPCOperations.getrawtransaction, new[] {txid}), throwIfNotFound)
					.ConfigureAwait(false);
			if (throwIfNotFound)
			{
				response.ThrowIfError();
			}

			if (response.Error != null && response.Error.Code == RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY)
			{
				return null;
			}

			response.ThrowIfError();
			var tx = new Transaction();
			tx.ReadWrite(Encoders.Hex.DecodeData(response.Result.ToString()));
			return tx;
		}

		public RawTransactionInfo GetRawTransactionInfo(uint256 txid)
		{
			return GetRawTransactionInfoAsync(txid).GetAwaiter().GetResult();
		}

		public async Task<RawTransactionInfo> GetRawTransactionInfoAsync(uint256 txId)
		{
			var request = new RPCRequest(RPCOperations.getrawtransaction, new object[] {txId, true});
			var response = await SendCommandAsync(request);
			var json = response.Result;
			return new RawTransactionInfo
			{
				Transaction = Transaction.Parse(json.Value<string>("hex"), Network),
				TransactionId = uint256.Parse(json.Value<string>("txid")),
				TransactionTime = json["time"] != null
					? Utils.UnixTimeToDateTime(json.Value<long>("time"))
					: (DateTimeOffset?) null,
				Hash = uint256.Parse(json.Value<string>("hash")),
				Size = json.Value<uint>("size"),
				Version = json.Value<uint>("version"),
				LockTime = new LockTime(json.Value<uint>("locktime")),
				BlockHash = json["blockhash"] != null ? uint256.Parse(json.Value<string>("blockhash")) : null,
				Confirmations = json.Value<uint>("confirmations"),
				BlockTime = json["blocktime"] != null
					? Utils.UnixTimeToDateTime(json.Value<long>("blocktime"))
					: (DateTimeOffset?) null
			};
		}

		public void SendRawTransaction(Transaction tx)
		{
			SendRawTransaction(tx.ToBytes());
		}

		public void SendRawTransaction(byte[] bytes)
		{
			SendCommand(RPCOperations.sendrawtransaction, Encoders.Hex.EncodeData(bytes));
		}

		public Task SendRawTransactionAsync(Transaction tx)
		{
			return SendRawTransactionAsync(tx.ToBytes());
		}

		public Task SendRawTransactionAsync(byte[] bytes)
		{
			return SendCommandAsync(RPCOperations.sendrawtransaction, Encoders.Hex.EncodeData(bytes));
		}

		/// <summary>
		///     Sign a transaction
		/// </summary>
		/// <param name="tx">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public Transaction SignRawTransaction(Transaction tx)
		{
			if (tx == null)
			{
				throw new ArgumentNullException(nameof(tx));
			}

			return SignRawTransactionAsync(tx).GetAwaiter().GetResult();
		}

		/// <summary>
		///     Sign a transaction
		/// </summary>
		/// <param name="tx">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public async Task<Transaction> SignRawTransactionAsync(Transaction tx)
		{
			var result = await SendCommandAsync(RPCOperations.signrawtransaction, tx.ToString()).ConfigureAwait(false);
			return new Transaction(result.Result["hex"].Value<string>());
		}

		// Utility functions

		// Estimates the approximate fee per kilobyte needed for a transaction to begin
		// confirmation within conf_target blocks if possible and return the number of blocks
		// for which the estimate is valid.Uses virtual transaction size as defined
		// in BIP 141 (witness data is discounted).

		// Fee Estimation

		/// <summary>
		///     (>= Bitcoin Core v0.14) Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">
		///     Whether to return a more conservative estimate which also satisfies a longer history. A
		///     conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired
		///     target, but is not as responsive to short term drops in the prevailing fee market.
		/// </param>
		/// <returns>The estimated fee rate, block number where estimate was found</returns>
		/// <exception cref="NoEstimationException">
		///     The Fee rate couldn't be estimated because of insufficient data from Bitcoin
		///     Core
		/// </exception>
		public EstimateSmartFeeResponse EstimateSmartFee(int confirmationTarget,
			EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
		{
			return EstimateSmartFeeAsync(confirmationTarget, estimateMode).GetAwaiter().GetResult();
		}

		/// <summary>
		///     (>= Bitcoin Core v0.14) Tries to get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">
		///     Whether to return a more conservative estimate which also satisfies a longer history. A
		///     conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired
		///     target, but is not as responsive to short term drops in the prevailing fee market.
		/// </param>
		/// <returns>The estimated fee rate, block number where estimate was found or null</returns>
		public async Task<EstimateSmartFeeResponse> TryEstimateSmartFeeAsync(int confirmationTarget,
			EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
		{
			return await EstimateSmartFeeImplAsync(confirmationTarget, estimateMode).ConfigureAwait(false);
		}

		/// <summary>
		///     (>= Bitcoin Core v0.14) Tries to get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">
		///     Whether to return a more conservative estimate which also satisfies a longer history. A
		///     conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired
		///     target, but is not as responsive to short term drops in the prevailing fee market.
		/// </param>
		/// <returns>The estimated fee rate, block number where estimate was found or null</returns>
		public EstimateSmartFeeResponse TryEstimateSmartFee(int confirmationTarget,
			EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
		{
			return TryEstimateSmartFeeAsync(confirmationTarget, estimateMode).GetAwaiter().GetResult();
		}

		/// <summary>
		///     (>= Bitcoin Core v0.14) Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">
		///     Whether to return a more conservative estimate which also satisfies a longer history. A
		///     conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired
		///     target, but is not as responsive to short term drops in the prevailing fee market.
		/// </param>
		/// <returns>The estimated fee rate, block number where estimate was found</returns>
		/// <exception cref="NoEstimationException">when fee couldn't be estimated</exception>
		public async Task<EstimateSmartFeeResponse> EstimateSmartFeeAsync(int confirmationTarget,
			EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
		{
			var feeRate = await EstimateSmartFeeImplAsync(confirmationTarget, estimateMode);
			if (feeRate == null)
			{
				throw new NoEstimationException(confirmationTarget);
			}

			return feeRate;
		}

		/// <summary>
		///     (>= Bitcoin Core v0.14)
		/// </summary>
		private async Task<EstimateSmartFeeResponse> EstimateSmartFeeImplAsync(int confirmationTarget,
			EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
		{
			var request = new RPCRequest(RPCOperations.estimatesmartfee.ToString(),
				new object[] {confirmationTarget, estimateMode.ToString().ToUpperInvariant()});

			var response = await SendCommandAsync(request, false).ConfigureAwait(false);

			if (response?.Error != null)
			{
				return null;
			}

			var resultJToken = response.Result;
			var feeRateDecimal = resultJToken.Value<decimal>("feerate"); // estimate fee-per-kilobyte (in BTC)
			var blocks = resultJToken.Value<int>("blocks"); // block number where estimate was found
			var money = Money.Coins(feeRateDecimal);
			if (money.Satoshi <= 0)
			{
				return null;
			}

			return new EstimateSmartFeeResponse
			{
				FeeRate = new FeeRate(money),
				Blocks = blocks
			};
		}

		public async Task<uint256[]> GenerateAsync(int nBlocks)
		{
			if (nBlocks < 0)
			{
				throw new ArgumentOutOfRangeException("nBlocks");
			}

			var result = (JArray) (await SendCommandAsync(RPCOperations.generate, nBlocks).ConfigureAwait(false))
				.Result;
			return result.Select(r => new uint256(r.Value<string>())).ToArray();
		}

		public uint256[] Generate(int nBlocks)
		{
			return GenerateAsync(nBlocks).GetAwaiter().GetResult();
		}

		/// <summary>
		///     Permanently marks a block as invalid, as if it violated a consensus rule.
		/// </summary>
		/// <param name="blockhash">the hash of the block to mark as invalid</param>
		public void InvalidateBlock(uint256 blockhash)
		{
			SendCommand(RPCOperations.invalidateblock, blockhash);
		}

		/// <summary>
		///     Permanently marks a block as invalid, as if it violated a consensus rule.
		/// </summary>
		/// <param name="blockhash">the hash of the block to mark as invalid</param>
		public async Task InvalidateBlockAsync(uint256 blockhash)
		{
			await SendCommandAsync(RPCOperations.invalidateblock, blockhash).ConfigureAwait(false);
		}
	}

	public class PeerInfo
	{
		public int Id { get; internal set; }

		public IPEndPoint Address { get; internal set; }

		public IPEndPoint LocalAddress { get; internal set; }

		public NodeServices Services { get; internal set; }

		public DateTimeOffset LastSend { get; internal set; }

		public DateTimeOffset LastReceive { get; internal set; }

		public long BytesSent { get; internal set; }

		public long BytesReceived { get; internal set; }

		public DateTimeOffset ConnectionTime { get; internal set; }

		public TimeSpan? PingTime { get; internal set; }

		public int Version { get; internal set; }

		public string SubVersion { get; internal set; }

		public bool Inbound { get; internal set; }

		public int StartingHeight { get; internal set; }

		public int BanScore { get; internal set; }

		public int SynchronizedHeaders { get; internal set; }

		public int SynchronizedBlocks { get; internal set; }

		public uint[] Inflight { get; internal set; }

		public bool IsWhiteListed { get; internal set; }

		public TimeSpan PingWait { get; internal set; }

		public int Blocks { get; internal set; }

		public TimeSpan TimeOffset { get; internal set; }
	}

	public class AddedNodeInfo
	{
		public EndPoint AddedNode { get; internal set; }

		public bool Connected { get; internal set; }

		public IEnumerable<NodeAddressInfo> Addresses { get; internal set; }
	}

	public class NodeAddressInfo
	{
		public IPEndPoint Address { get; internal set; }

		public bool Connected { get; internal set; }
	}

	public class BlockchainInfo
	{
		public Network Chain { get; set; }
		public ulong Blocks { get; set; }
		public ulong Headers { get; set; }
		public uint256 BestBlockHash { get; set; }
		public ulong Difficulty { get; set; }
		public ulong MedianTime { get; set; }

		public float VerificationProgress { get; set; }
		public uint256 ChainWork { get; set; }
		public ulong SizeOnDisk { get; set; }
		public bool Pruned { get; set; }

		public List<SoftFork> SoftForks { get; set; }

		public class SoftFork
		{
			public string Bip { get; set; }
			public int Version { get; set; }
			public bool RejectStatus { get; set; }
		}
	}

	public class RawTransactionInfo
	{
		public Transaction Transaction { get; internal set; }
		public uint256 TransactionId { get; internal set; }
		public uint256 Hash { get; internal set; }
		public uint Size { get; internal set; }
		public uint Version { get; internal set; }
		public LockTime LockTime { get; internal set; }
		public uint256 BlockHash { get; internal set; }
		public uint Confirmations { get; internal set; }
		public DateTimeOffset? TransactionTime { get; internal set; }
		public DateTimeOffset? BlockTime { get; internal set; }
	}

	public class NoEstimationException : Exception
	{
		public NoEstimationException(int nblock)
			: base(
				"The FeeRate couldn't be estimated because of insufficient data from Bitcoin Core. Try to use smaller nBlock, or wait Bitcoin Core to gather more data.")
		{
		}
	}
}