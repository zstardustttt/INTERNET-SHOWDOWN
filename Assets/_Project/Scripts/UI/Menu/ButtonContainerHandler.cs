using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ButtonContainerHandler : MonoBehaviour
{
    public GameObject[] buttons;
    private MenuManager _menuManager;
    private SectionManager _sectionManager;
    private void Awake()
    {
        _menuManager = FindObjectsByType<MenuManager>(FindObjectsSortMode.None)[0];
        _sectionManager = _menuManager.GetComponent<SectionManager>();
    }

    public void TransitionIn()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(
            buttons[2].transform.DOLocalRotate(new Vector3(0, 0, -75f), 0.6f)
        );

        sequence.Insert(0.1f,
            buttons[1].transform.DOLocalRotate(new Vector3(0, 0, -60f), 0.6f)
        );

        sequence.Insert(0.2f,
            buttons[0].transform.DOLocalRotate(new Vector3(0, 0, -45f), 0.6f)
        );

        sequence.OnComplete(() =>
        {
            buttons[0].transform.DOKill(false);
            buttons[1].transform.DOKill(false);
            buttons[2].transform.DOKill(false);
        });
    }

    public void TransitionOut()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(
            buttons[2].transform.DOLocalRotate(new Vector3(0, 0, 210f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.InOutSine)
        );

        sequence.Insert(0.1f,
            buttons[1].transform.DOLocalRotate(new Vector3(0, 0, 195f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.InOutSine)
        );

        sequence.Insert(0.2f,
            buttons[0].transform.DOLocalRotate(new Vector3(0, 0, 180f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.InOutSine)
        );

        sequence.OnComplete(() =>
        {
            foreach (GameObject btn in buttons)
            {
                btn.transform.DOKill(false);
                btn.transform.localEulerAngles = new Vector3(0, 0, 0);
            }
        });
    }
}