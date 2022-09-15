using System.Collections.Generic;
using Frictionless;
using Solana.Unity.Rpc.Models;
using UnityEngine;

[ExecuteAlways]
public class PlayerFollow : MonoBehaviour
{
    [SerializeField] float _xOffset;
    [SerializeField] Transform _objectToFollow;

    public List<GameObject> Backgrounds;
    
    private void Start() 
    {
        if(_objectToFollow == null)
            enabled = false;

        ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<ScoreChangedMessage>(OnScoreChangedMessage);
    }

    private void OnScoreChangedMessage(ScoreChangedMessage message)
    {
        Backgrounds[0].gameObject.SetActive(false);
        Backgrounds[1].gameObject.SetActive(false);
        Backgrounds[2].gameObject.SetActive(false);
        Backgrounds[3].gameObject.SetActive(false);
        Backgrounds[4].gameObject.SetActive(false);
        
        if (message.NewScore <= 10)
        {
            Backgrounds[0].gameObject.SetActive(true);
        }
        else if (message.NewScore > 10 && message.NewScore < 25)
        {
            Backgrounds[1].gameObject.SetActive(true);
        } else if (message.NewScore > 25 && message.NewScore < 50)
        {
            Backgrounds[2].gameObject.SetActive(true);
        } else if (message.NewScore > 50 && message.NewScore < 100)
        {
            Backgrounds[3].gameObject.SetActive(true);
        }else if (message.NewScore > 100)
        {
            Backgrounds[4].gameObject.SetActive(true);
        }
    }

    void LateUpdate()
    {
        Vector3 target = transform.position;
        target.x = _objectToFollow.position.x + _xOffset;
        transform.position = target;
    }
}
