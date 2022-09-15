using Frictionless;
using SolPlay.Deeplinks;
using UnityEngine;

namespace SolPlay.DeeplinksNftExample.Scripts
{
    public class LoggingService
    {
        public void Log(string message, bool showBlimpOnScreen)
        {
            Debug.Log(message);
            if (showBlimpOnScreen)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage(message));
            }
        }
        public void LogWarning(string message, bool showBlimpOnScreen)
        {
            Debug.LogWarning(message);
            if (showBlimpOnScreen)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage(message));
            }
        }

    }
}