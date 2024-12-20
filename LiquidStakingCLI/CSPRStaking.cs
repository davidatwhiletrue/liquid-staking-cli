using Casper.Network.SDK;
using Casper.Network.SDK.Types;
using NetSdkValues;

namespace LiquidStakingCLI;

public class CSPRStaking
{
    public static async Task Install(
        NetCasperClient casperClient,
        KeyPair sender,
        PublicKey validator)
    {
        var runtimeArgs = new List<NamedArg>()
        {
            new ("odra_cfg_is_upgradable", CLValue.Bool(false)),
            new ("odra_cfg_allow_key_override", CLValue.Bool(true)),
            new ("odra_cfg_package_hash_key_name", CLValue.String("StakedCSPR_package_hash")),
            new ("validator_address", CLValue.PublicKey(validator)),
            new ("claim_time", CLValue.U64(7*60*60*1000))
        };

        var wasmBytes = File.ReadAllBytes("/Users/davidhernando/MAKE/LiquidStaking/liquid-staking-contracts/wasm/StakedCSPR.wasm");

        var transaction = new Transaction.SessionBuilder()
            .InstallOrUpgrade()
            .From(sender.PublicKey)
            .Wasm(wasmBytes)
            .RuntimeArgs(runtimeArgs)
            .ChainName(Values.ChainName)
            .Payment(PricingMode.PaymentLimited(500_000000000))
            .Build();
        transaction.Sign(sender);

        Console.WriteLine("TRANSACTION HASH: " + transaction.Hash);
        await casperClient.PutTransaction(transaction);

        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var txResponse = await casperClient.GetTransaction(transaction.Hash, false, tokenSource.Token);

        File.WriteAllText("tx.json", txResponse.Result.GetRawText());
        
        var result = txResponse.Parse();
        var execResult = result.ExecutionInfo.ExecutionResult;

        if (execResult.ErrorMessage != null)
            throw new Exception(execResult.ErrorMessage);

        var transform = execResult.Effect.FirstOrDefault(t => t.Kind is WriteTransformKind wtk 
                                                     && wtk.Value.Contract != null);
        if (transform != null)
            Console.WriteLine("Contract hash: " + transform.Key);
        else
            Console.WriteLine("Unexpectedly, could not find an entity-contract key in the execution result");
    }

    public static async Task Unstake(
        NetCasperClient casperClient,
        KeyPair sender,
        string contractHash,
        ulong amount
    )
    {
        var args = new List<NamedArg>()
        {
            new("amount", CLValue.U256(amount))
        };
        
        var transaction = new Transaction.ContractCallBuilder()
            .From(sender.PublicKey)
            .ByHash(contractHash)
            .EntryPoint("unstake")
            .RuntimeArgs(args)
            .Payment(PricingMode.PaymentLimited(3_000000000))
            .ChainName(Values.ChainName)
            .Build();
            
        transaction.Sign(sender);
    
        Console.WriteLine("TRANSACTION HASH: " + transaction.Hash);
        await casperClient.PutTransaction(transaction);
    
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var txResponse = await casperClient.GetTransaction(transaction.Hash, false, tokenSource.Token);
    }
}