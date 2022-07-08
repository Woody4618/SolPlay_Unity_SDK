using AllArt.Solana.Nft;
using Frictionless;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    public class NftItemListView : MonoBehaviour
    {
        public GameObject ItemRoot;
        public NftItemView itemPrefab;
        public string FilterSymbol;

        public void OnEnable()
        {
            UpdateContent();
        }

        public void Clear()
        {
            foreach (Transform trans in ItemRoot.transform)
            {
                Destroy(trans.gameObject);
            }
        }

        public void UpdateContent()
        {
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            if (nftService == null)
            {
                return;
            }

            Clear();

            foreach (Nft nft in nftService.MetaPlexNFts)
            {
                InstantiateListNftItem(nft);
            }
        }

        public void AddNFt(Nft newNft)
        {
            InstantiateListNftItem(newNft);
        }

        private void InstantiateListNftItem(Nft nft)
        {
            if (string.IsNullOrEmpty(nft.MetaplexData.mint))
            {
                return;
            }

            if (!string.IsNullOrEmpty(FilterSymbol) && nft.MetaplexData.data.symbol != FilterSymbol)
            {
                return;
            }

            NftItemView instance = Instantiate(itemPrefab, ItemRoot.transform);
            instance.SetData(nft, OnItemClicked);
        }

        private void OnItemClicked(NftItemView itemView)
        {
            Debug.Log("Item Clicked: " + itemView.CurrentNft.MetaplexData.data.name);
            ServiceFactory.Instance.Resolve<NftContextMenu>().Open(itemView);
        }
    }
}