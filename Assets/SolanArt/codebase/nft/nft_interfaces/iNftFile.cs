
namespace AllArt.Solana.Nft { 
    public interface iNftFile<T> {
        string Name { get; set; }
        string Extension { get; set; }
        string ExternalUrl { get; set; }
        T File { get; set; }
    }
}