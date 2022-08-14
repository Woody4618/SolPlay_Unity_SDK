using System.Collections;
using Frictionless;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    public class BlimpSystem : MonoBehaviour
    {
        public class ShowBlimpMessage
        {
            public string BlimpText;

            public ShowBlimpMessage(string blimpText)
            {
                BlimpText = blimpText;
            }
        }

        public TextBlimp TextBlimpPrefab;
        public GameObject BlimpRoot;

        // Start is called before the first frame update
        void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<ShowBlimpMessage>(OnShowBlimpMessage);
            Application.logMessageReceived += OnLogMessage;
        }

        private void OnLogMessage(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                SpawnBlimp(condition);
            }
        }

        private void OnShowBlimpMessage(ShowBlimpMessage message)
        {
            SpawnBlimp(message.BlimpText);
        }

        private void SpawnBlimp(string message)
        {
            // TODO: Pool for production
            var instance = Instantiate<TextBlimp>(TextBlimpPrefab, BlimpRoot.transform);
            instance.SetData(message);

            StartCoroutine(DestroyDelayed(instance.gameObject));
        }

        private IEnumerator DestroyDelayed(GameObject go)
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(go);
        }
    }
}