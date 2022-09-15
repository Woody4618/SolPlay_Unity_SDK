using Frictionless;
using UnityEngine;

namespace SolPlay.DeeplinksNftExample.Scripts
{
    public class MainInstaller : MonoBehaviour
    {
        private LoggingService _loggingService;
        
        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
            _loggingService = new LoggingService();
            ServiceFactory.Instance.RegisterSingleton(_loggingService);
        }
    }
}