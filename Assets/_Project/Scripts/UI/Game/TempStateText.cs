using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Gameplay;
using TMPro;
using UnityEngine;

public class TempStateText : MonoBehaviour
{
    GameState state;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus<OnGameStateChange>.Listen((data) => state = data.state);
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<TMP_Text>().text = state.ToString();
    }
}
