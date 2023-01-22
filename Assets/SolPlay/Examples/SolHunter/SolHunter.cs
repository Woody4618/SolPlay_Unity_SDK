using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using SolHunter;
using SolHunter.Program;
using SolHunter.Errors;
using SolHunter.Accounts;
using SolHunter.Types;

namespace SolHunter
{
    namespace Accounts
    {
        public partial class GameDataAccount
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 2830422829680616787UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{83, 229, 68, 63, 145, 174, 71, 39};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "F2tkBUcrpHt";
            public Tile[][] Board { get; set; }

            public static GameDataAccount Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                GameDataAccount result = new GameDataAccount();
                result.Board = new Tile[4][];
                for (uint resultBoardIdx = 0; resultBoardIdx < 4; resultBoardIdx++)
                {
                    result.Board[resultBoardIdx] = new Tile[4];
                    for (uint resultBoardresultBoardIdxIdx = 0; resultBoardresultBoardIdxIdx < 4; resultBoardresultBoardIdxIdx++)
                    {
                        offset += Tile.Deserialize(_data, offset, out var resultBoardresultBoardIdxresultBoardresultBoardIdxIdx);
                        result.Board[resultBoardIdx][resultBoardresultBoardIdxIdx] = resultBoardresultBoardIdxresultBoardresultBoardIdxIdx;
                    }
                }

                return result;
            }
        }

        public partial class ChestVaultAccount
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 9406927803919968769UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{1, 42, 101, 100, 255, 30, 140, 130};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "CJrgsQPtJV";
            public static ChestVaultAccount Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                ChestVaultAccount result = new ChestVaultAccount();
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum SolHunterErrorKind : uint
        {
            TileOutOfBounds = 6000U,
            BoardIsFull = 6001U,
            PlayerAlreadyExists = 6002U,
            TriedToMovePlayerThatWasNotOnTheBoard = 6003U,
            WrongDirectionInput = 6004U
        }
    }

    namespace Types
    {
        public partial class Tile
        {
            public PublicKey Player { get; set; }

            public byte State { get; set; }

            public byte Health { get; set; }

            public ulong CollectReward { get; set; }

            public PublicKey Avatar { get; set; }

            public byte Kills { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WritePubKey(Player, offset);
                offset += 32;
                _data.WriteU8(State, offset);
                offset += 1;
                _data.WriteU8(Health, offset);
                offset += 1;
                _data.WriteU64(CollectReward, offset);
                offset += 8;
                _data.WritePubKey(Avatar, offset);
                offset += 32;
                _data.WriteU8(Kills, offset);
                offset += 1;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out Tile result)
            {
                int offset = initialOffset;
                result = new Tile();
                result.Player = _data.GetPubKey(offset);
                offset += 32;
                result.State = _data.GetU8(offset);
                offset += 1;
                result.Health = _data.GetU8(offset);
                offset += 1;
                result.CollectReward = _data.GetU64(offset);
                offset += 8;
                result.Avatar = _data.GetPubKey(offset);
                offset += 32;
                result.Kills = _data.GetU8(offset);
                offset += 1;
                return offset - initialOffset;
            }
        }
    }

    public partial class SolHunterClient : TransactionalBaseClient<SolHunterErrorKind>
    {
        public SolHunterClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameDataAccount>>> GetGameDataAccountsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = GameDataAccount.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameDataAccount>>(res);
            List<GameDataAccount> resultingAccounts = new List<GameDataAccount>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => GameDataAccount.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameDataAccount>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<ChestVaultAccount>>> GetChestVaultAccountsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = ChestVaultAccount.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<ChestVaultAccount>>(res);
            List<ChestVaultAccount> resultingAccounts = new List<ChestVaultAccount>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => ChestVaultAccount.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<ChestVaultAccount>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<GameDataAccount>> GetGameDataAccountAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<GameDataAccount>(res);
            var resultingAccount = GameDataAccount.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<GameDataAccount>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<ChestVaultAccount>> GetChestVaultAccountAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<ChestVaultAccount>(res);
            var resultingAccount = ChestVaultAccount.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<ChestVaultAccount>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeGameDataAccountAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, GameDataAccount> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                GameDataAccount parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = GameDataAccount.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeChestVaultAccountAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, ChestVaultAccount> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                ChestVaultAccount parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = ChestVaultAccount.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializeAsync(InitializeAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SolHunterProgram.Initialize(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendSpawnPlayerAsync(SpawnPlayerAccounts accounts, PublicKey avatar, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SolHunterProgram.SpawnPlayer(accounts, avatar, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendMovePlayerAsync(MovePlayerAccounts accounts, byte direction, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SolHunterProgram.MovePlayer(accounts, direction, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendMovePlayerV2Async(MovePlayerV2Accounts accounts, byte direction, byte blockBump, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SolHunterProgram.MovePlayerV2(accounts, direction, blockBump, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<SolHunterErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<SolHunterErrorKind>>{};
        }
    }

    namespace Program
    {
        public class InitializeAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey NewGameDataAccount { get; set; }

            public PublicKey ChestVault { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class SpawnPlayerAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey ChestVault { get; set; }

            public PublicKey GameDataAccount { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class MovePlayerAccounts
        {
            public PublicKey ChestVault { get; set; }

            public PublicKey GameDataAccount { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class MovePlayerV2Accounts
        {
            public PublicKey ChestVault { get; set; }

            public PublicKey GameDataAccount { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class SolHunterProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.NewGameDataAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.ChestVault, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction SpawnPlayer(SpawnPlayerAccounts accounts, PublicKey avatar, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.ChestVault, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameDataAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(3695486382544324736UL, offset);
                offset += 8;
                _data.WritePubKey(avatar, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction MovePlayer(MovePlayerAccounts accounts, byte direction, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.ChestVault, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameDataAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16684840164937447953UL, offset);
                offset += 8;
                _data.WriteU8(direction, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction MovePlayerV2(MovePlayerV2Accounts accounts, byte direction, byte blockBump, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.ChestVault, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameDataAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15560957635555335921UL, offset);
                offset += 8;
                _data.WriteU8(direction, offset);
                offset += 1;
                _data.WriteU8(blockBump, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}