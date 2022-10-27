using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    public class BasePopup : MonoBehaviour
    {
        public GameObject Root;
        public Button CloseButton;

        protected void Awake()
        {
            Root.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (CloseButton != null)
            {
                CloseButton.onClick.RemoveAllListeners();
                CloseButton.onClick.AddListener(OnCloseButtonClicked);
            }

            Root.gameObject.SetActive(true);
        }

        public void Close()
        {
            Root.gameObject.SetActive(false);
        }

        private void OnCloseButtonClicked()
        {
            Close();
        }
    }
}