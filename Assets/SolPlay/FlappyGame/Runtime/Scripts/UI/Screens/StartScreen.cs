using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using Frictionless;
using SolPlay.Deeplinks;
using UnityEngine;

public class StartScreen : MonoBehaviour
{
    [SerializeField] GameMode _gameMode;

    public GameObject NftSelectedRoot;
    public GameObject NftUnselectedRoot;
    
    public void StartGame() => _gameMode.StartWaiting();

    private void Start()
    {
        UpdateContent();
        ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftSelectedMessage>(OnNftSelectedMessage);
        ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftLoadingFinishedMessage>(OnNftLoadingFinishedMessage);
    }

    private void OnNftLoadingFinishedMessage(NftLoadingFinishedMessage message)
    {
        UpdateContent();
    }

    private void OnNftSelectedMessage(NftSelectedMessage message)
    {
        UpdateContent();
    }
    
    private void UpdateContent()
    {
        var nftService = ServiceFactory.Instance.Resolve<NftService>();
        NftSelectedRoot.gameObject.SetActive(nftService.SelectedNft != null);
        NftUnselectedRoot.gameObject.SetActive(nftService.SelectedNft == null);
    }
}
