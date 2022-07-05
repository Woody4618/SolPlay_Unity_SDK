using System.Collections;
using Frictionless;
using UnityEngine;

namespace Solplay.Deeplinks
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
        }

        private void OnShowBlimpMessage(ShowBlimpMessage message)
        {
            // TODO: Pool for production
            var instance = Instantiate<TextBlimp>(TextBlimpPrefab, BlimpRoot.transform);
            instance.SetData(message.BlimpText);

            StartCoroutine(DestroyDelayed(instance.gameObject));
        }

        private IEnumerator DestroyDelayed(GameObject go)
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(go);
        }
    }
}