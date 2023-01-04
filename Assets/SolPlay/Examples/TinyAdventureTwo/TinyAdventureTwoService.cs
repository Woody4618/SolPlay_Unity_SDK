using System.Collections;
using System.Collections.Generic;
using System.Text;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Services;
using TinyAdventureTwo.Program;
using UnityEngine;

public class TinyAdventureTwoService : MonoBehaviour
{

    private string ProgramId = "FuMji6fC3YRe4YcqoYFctyhggyR7cDMAen1ut75PeGie";

    public void Start()
    {
        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("chestVault")
            },
            new PublicKey(ProgramId), out PublicKey chestVault, out var bump);
        
        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("level1")
            },
            new PublicKey(ProgramId), out PublicKey gameDataAccount, out var bump2);

        
        var baseWallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;
        MoveRightAccounts accounts = new MoveRightAccounts();
        accounts.Signer = baseWallet.Account.PublicKey;
        accounts.ChestVault = chestVault;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.GameDataAccount = gameDataAccount;
        
        TinyAdventureTwoProgram.MoveRight(accounts, new PublicKey(ProgramId));
    }
    
    
    
}
