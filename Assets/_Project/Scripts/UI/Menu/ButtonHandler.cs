using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    public Vector2 shadowPos;
    private MenuManager _menuManager;
    private SectionManager _sectionManager;
    private Shadow _shadow;
    private Tween _mouseEnter;
    private Tween _mouseExit;

    private void Awake()
    {
        _menuManager = FindObjectsByType<MenuManager>(FindObjectsSortMode.None)[0];
        _sectionManager = _menuManager.GetComponent<SectionManager>();
    }

    private void Start()
    {
        _shadow = this.GetComponent<Shadow>();
    }

    public void MouseEnter()
    {
        if (!_sectionManager.SectionChanging)
        {

        }
    }

    public void MouseExit()
    {
        if (!_sectionManager.SectionChanging)
        {

        }
    }

    public void MouseClick(int section)
    {
        if (!_sectionManager.SectionChanging)
        {

        }
    }
}