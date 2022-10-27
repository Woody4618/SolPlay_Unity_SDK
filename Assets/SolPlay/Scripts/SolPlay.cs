using Frictionless;
using SolPlay.DeeplinksNftExample.Scripts;
using SolPlay.Scripts.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SolPlay.Deeplinks
{
    public class SolPlay : MonoBehaviour
    {
        public static SolPlay Instance;

        private LoggingService _loggingService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceFactory.RegisterSingleton(this);
            _loggingService = new LoggingService();
            ServiceFactory.RegisterSingleton(_loggingService);
        }

        public void LoadScene(string newSceneName)
        {
            MessageRouter.Reset();
            ServiceFactory.Reset();

            SceneManager.LoadScene(newSceneName);
        }

        public void LoadSceneAsync(string newSceneName)
        {
            MessageRouter.Reset();
            ServiceFactory.Reset();

            SceneManager.LoadSceneAsync(newSceneName);
        }
    }
}