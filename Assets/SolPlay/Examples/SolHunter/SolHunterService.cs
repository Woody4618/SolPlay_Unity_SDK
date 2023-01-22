using System;
using System.Text;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using SolHunter.Program;
using SolPlay.DeeplinksNftExample.Utils;
using SolPlay.Scripts.Services;
using UnityEngine;

public class SolHunterService : MonoBehaviour
{
    public static PublicKey ProgramId = new PublicKey("huntegMDH7NicWeJ7ezxiV4PsTrvMRkswNL4Uamm44h");

    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }

    public static byte STATE_EMPTY = 0;
    public static byte STATE_PLAYER = 1;
    public static byte STATE_CHEST = 2;

    public static int TILE_COUNT_X = 4;
    public static int TILE_COUNT_Y = 4;

    public SolHunter.Accounts.GameDataAccount CurrentGameData;

    private PublicKey levelAccount;

    private PublicKey chestVaultAccount;

    // This is a little trick to not get the same transaction hash when there are multiple of the same transactions in the 
    // with the same block hash. Since the would result in the same hash one would be dropped. So we just increase this byte 
    // and send it the with instruction. 
    private int InstructionBump;

    public class SolHunterGameDataChangedMessage
    {
        public SolHunter.Accounts.GameDataAccount GameDataAccount;
    }

    public void OnEnable()
    {
        ServiceFactory.RegisterSingleton(this);
        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("level105")
            },
            ProgramId, out levelAccount, out var bump);

        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("chestVault105")
            },
            ProgramId, out chestVaultAccount, out var bumpChest);
    }

    private void Start()
    {
        MessageRouter.AddHandler<SocketServerConnectedMessage>(OnSocketConnected);
    }

    private void OnSocketConnected(SocketServerConnectedMessage message)
    {
        GetGameData();
        ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(levelAccount, result =>
        {
            SolHunter.Accounts.GameDataAccount gameDataAccount =
                SolHunter.Accounts.GameDataAccount.Deserialize(Convert.FromBase64String(result.result.value.data[0]));
            SetCachedGameData(gameDataAccount);
            MessageRouter.RaiseMessage(new SolHunterGameDataChangedMessage()
            {
                GameDataAccount = gameDataAccount
            });
        });
    }

    public async Task<SolHunter.Accounts.GameDataAccount> GetGameData()
    {
        var gameData = await ServiceFactory.Resolve<WalletHolderService>().InGameWallet.ActiveRpcClient
            .GetAccountInfoAsync(this.levelAccount, Commitment.Confirmed, BinaryEncoding.JsonParsed);
        SolHunter.Accounts.GameDataAccount gameDataAccount =
            SolHunter.Accounts.GameDataAccount.Deserialize(Convert.FromBase64String(gameData.Result.Value.Data[0]));
        SetCachedGameData(gameDataAccount);
        MessageRouter.RaiseMessage(new SolHunterGameDataChangedMessage()
        {
            GameDataAccount = gameDataAccount
        });
        return gameDataAccount;
    }

    private void SetCachedGameData(SolHunter.Accounts.GameDataAccount gameDataAccount)
    {
        bool playerWasAlive = false;
        if (CurrentGameData != null)
        {
            playerWasAlive = IsPlayerSpawned();
        }

        CurrentGameData = gameDataAccount;
        bool playerAliveAfterData = IsPlayerSpawned();

        if (playerWasAlive && !playerAliveAfterData)
        {
            LoggingService.Log("You died :( ", true);
            ServiceFactory.Resolve<NftService>().ResetSelectedNft();
        }
        else
        {
            var avatar = TryGetSpawnedPlayerAvatar();
            if (avatar != null)
            {
                var avatarNft = ServiceFactory.Resolve<NftService>().GetNftByMintAddress(avatar);
                ServiceFactory.Resolve<NftService>().SelectNft(avatarNft);
            }
        }
    }

    public void Initialize()
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, 300000000))
        {
            return;
        }

        TransactionInstruction initializeInstruction = InitializeInstruction();
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock("Initialize", initializeInstruction,
            walletHolderService.InGameWallet);
    }

    public void Move(Direction direction)
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, 10000))
        {
            return;
        }

        TransactionInstruction initializeInstruction = GetMovePlayerInstruction((byte) direction);
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock($"Move{direction}",
            initializeInstruction, walletHolderService.InGameWallet);
    }

    public void SpawnPlayerAndChest()
    {
        long costForSpawn = (long) (0.11f * SolanaUtils.SolToLamports);
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, costForSpawn))
        {
            return;
        }

        LoggingService.Log("Spawn player and chest", true);

        TransactionInstruction initializeInstruction = GetSpawnPlayerAndChestInstruction();
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock("Spawn player", initializeInstruction,
            walletHolderService.InGameWallet,
            s =>
            {
                //GetGameData(); not needed since we rely on the socket connection
            });
    }

    private TransactionInstruction GetMovePlayerInstruction(byte direction)
    {
        MovePlayerV2Accounts accounts = new MovePlayerV2Accounts();
        accounts.GameDataAccount = levelAccount;
        accounts.ChestVault = chestVaultAccount;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.Player = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey;

        TransactionInstruction initializeInstruction =
            SolHunterProgram.MovePlayerV2(accounts, direction, GetInstructionBump(), ProgramId);
        return initializeInstruction;
    }

    private byte GetInstructionBump()
    {
        return (byte) (InstructionBump++ % 127);
    }

    private TransactionInstruction InitializeInstruction()
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        var wallet = walletHolderService.InGameWallet;

        var accounts = new SolHunter.Program.InitializeAccounts();
        accounts.Signer = wallet.Account.PublicKey;
        accounts.NewGameDataAccount = levelAccount;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.ChestVault = chestVaultAccount;

        TransactionInstruction initializeInstruction = SolHunterProgram.Initialize(accounts, ProgramId);
        return initializeInstruction;
    }

    public TransactionInstruction GetSpawnPlayerAndChestInstruction()
    {
        var selectedNft = ServiceFactory.Resolve<NftService>().SelectedNft;
        if (selectedNft == null)
        {
            LoggingService.Log("Nft is still loading...", true);
            return null;
        }

        SpawnPlayerAccounts accounts = new SpawnPlayerAccounts();
        accounts.GameDataAccount = levelAccount;
        accounts.ChestVault = chestVaultAccount;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.Payer = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey;

        TransactionInstruction initializeInstruction =
            SolHunterProgram.SpawnPlayer(accounts, new PublicKey(selectedNft.MetaplexData.mint), ProgramId);
        return initializeInstruction;
    }

    public PublicKey TryGetSpawnedPlayerAvatar()
    {
        if (CurrentGameData == null)
        {
            return null;
        }

        var localWallet = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey.Key;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                var tile = CurrentGameData.Board[y][x];
                if (tile.Player == localWallet && tile.State == STATE_PLAYER)
                {
                    return tile.Avatar;
                }
            }
        }

        return null;
    }

    public bool IsPlayerSpawned()
    {
        if (CurrentGameData == null)
        {
            return false;
        }

        var localWallet = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey.Key;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                var tile = CurrentGameData.Board[y][x];
                if (tile.Player == localWallet && tile.State == STATE_PLAYER)
                {
                    return true;
                }
            }
        }

        return false;
    }
}