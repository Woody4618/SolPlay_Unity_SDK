using Frictionless;
using SolPlay.Scripts.Ui;
using UnityEngine;

namespace SolPlay.Scripts.Services
{
    public class LoggingService
    {
        public void Log(string message, bool showBlimpOnScreen)
        {
            Debug.Log(message);
            if (showBlimpOnScreen)
            {
                MessageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage(message));
            }
        }

        public void LogWarning(string message, bool showBlimpOnScreen)
        {
            Debug.LogWarning(message);
            if (showBlimpOnScreen)
            {
                MessageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage(message));
            }
        }
    }
}