using System;
using System.Collections.Generic;
using Frictionless;
using SolPlay.CustomSmartContractExample;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    public class NftItemListView : MonoBehaviour
    {
        public GameObject ItemRoot;
        public NftItemView itemPrefab;
        public string FilterSymbol;
        public string BlackList;

        private List<NftItemView> allNftItemViews = new List<NftItemView>();

        public void OnEnable()
        {
            UpdateContent();
        }

        public void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftSelectedMessage>(OnNFtSelectedMessage);
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NewHighScoreLoadedMessage>(OnHighscoreLoadedMessage);
        }

        private void OnHighscoreLoadedMessage(NewHighScoreLoadedMessage message)
        {
            foreach (var itemView in allNftItemViews)
            {
                if (itemView.CurrentNft.MetaplexData.mint.Contains(message.HighscoreEntry.Seed))
                {
                    itemView.PowerLevel.text = $"Score: {message.HighscoreEntry.Highscore}";
                }   
            }
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

            NftItemView nftItemView = Instantiate(itemPrefab, ItemRoot.transform);
            nftItemView.SetData(solPlayNft, OnItemClicked);
            allNftItemViews.Add(nftItemView);
        }

        private void OnItemClicked(NftItemView itemView)
        {
            Debug.Log("Item Clicked: " + itemView.currentSolPlayNft.MetaplexData.data.name);
            ServiceFactory.Instance.Resolve<NftContextMenu>().Open(itemView);
        }
    }
}