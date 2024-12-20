using NetSdkValues;

namespace LiquidStakingCLI;

class Program
{
    static async Task Main(string[] args)
    {
        await CSPRStaking.Install(
            Values.GetCasperClient(false),
            Values.User1KeyPair,
            Values.Node1KeyPair.PublicKey
        );

        await CSPRStaking.Unstake(
            Values.GetCasperClient(false),
            Values.User1KeyPair,
            "0d3ed4da8ad7e0a4ff897176b7eba3005ac91e5a8bcdf3c27889a12c78f0917a",
            1000000000);
    }
}