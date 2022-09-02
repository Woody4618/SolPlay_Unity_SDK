using System;
using Frictionless;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    public class NftItemListView : MonoBehaviour
    {
        public GameObject ItemRoot;
        public NftItemView itemPrefab;
        public string FilterSymbol;
        public string BlackList;

        public void OnEnable()
        {
            UpdateContent();
        }

        public void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftSelectedMessage>(OnNFtSelectedMessage);
        }

        private void OnNFtSelectedMessage(NftSelectedMessage message)
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

            foreach (SolPlayNft nft in nftService.MetaPlexNFts)
            {
                InstantiateListNftItem(nft);
            }
        }

        public void AddNFt(SolPlayNft newSolPlayNft)
        {
            InstantiateListNftItem(newSolPlayNft);
        }

        private void InstantiateListNftItem(SolPlayNft solPlayNft)
        {
            if (string.IsNullOrEmpty(solPlayNft.MetaplexData.mint))
            {
                return;
            }

            if (!string.IsNullOrEmpty(FilterSymbol) && solPlayNft.MetaplexData.data.symbol != FilterSymbol)
            {
                return;
            }

            if (!string.IsNullOrEmpty(BlackList) && solPlayNft.MetaplexData.data.symbol == BlackList)
            {
                return;
            }

            NftItemView instance = Instantiate(itemPrefab, ItemRoot.transform);
            instance.SetData(solPlayNft, OnItemClicked);
        }

        private void OnItemClicked(NftItemView itemView)
        {
            Debug.Log("Item Clicked: " + itemView.currentSolPlayNft.MetaplexData.data.name);
            ServiceFactory.Instance.Resolve<NftContextMenu>().Open(itemView);
        }
    }
}