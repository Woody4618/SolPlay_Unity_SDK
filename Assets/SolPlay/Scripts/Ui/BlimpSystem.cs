using System;
using System.Collections;
using Frictionless;
using SolPlay.Deeplinks;
using UnityEngine;

namespace SolPlay.Scripts.Ui
{
    public class BlimpSystem : MonoBehaviour
    {
        public enum BlimpType
        {
            TextWithBackground,
            Boost,
            Score
        }

        public class ShowBlimpMessage
        {
            public string BlimpText;
            public BlimpType BlimpType;

            public ShowBlimpMessage(string blimpText, BlimpType blimpType = BlimpType.TextWithBackground)
            {
                BlimpText = blimpText;
                BlimpType = blimpType;
            }
        }

        public TextBlimp TextBlimpPrefab;
        public TextBlimp BoostBlimpPrefab;
        public TextBlimp ScoreBlimpPrefab;
        public GameObject BlimpRoot;

        // Start is called before the first frame update
        void Start()
        {
            MessageRouter.AddHandler<ShowBlimpMessage>(OnShowBlimpMessage);
            Application.logMessageReceived += OnLogMessage;
        }

        private void OnDestroy()
        {
            MessageRouter.RemoveHandler<ShowBlimpMessage>(OnShowBlimpMessage);
        }

        private void OnLogMessage(string condition, string stacktrace, LogType type)
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (type == LogType.Error || type == LogType.Exception)
            {
                SpawnBlimp(condition, BlimpType.TextWithBackground);
            }
        }

        private void OnShowBlimpMessage(ShowBlimpMessage message)
        {
            SpawnBlimp(message.BlimpText, message.BlimpType);
        }

        private void SpawnBlimp(string message, BlimpType blimpType)
        {
            // TODO: Pool for production
            TextBlimp blimpPrefab = null;
            string animationId = null;
            switch (blimpType)
            {
                case BlimpType.TextWithBackground:
                    blimpPrefab = TextBlimpPrefab;
                    break;
                case BlimpType.Boost:
                    blimpPrefab = BoostBlimpPrefab;
                    animationId = "BoostBlimpAppear";
                    break;
                case BlimpType.Score:
                    blimpPrefab = ScoreBlimpPrefab;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(blimpType), blimpType, null);
            }

            var instance = Instantiate(blimpPrefab, BlimpRoot.transform);
            instance.SetData(message);
            if (!string.IsNullOrEmpty(animationId))
            {
                instance.GetComponent<Animator>().Play(animationId);
            }

            StartCoroutine(DestroyDelayed(instance.gameObject));
        }

        private IEnumerator DestroyDelayed(GameObject go)
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(go);
        }
    }
}