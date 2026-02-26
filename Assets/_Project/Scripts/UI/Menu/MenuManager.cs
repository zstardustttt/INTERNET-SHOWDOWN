using System.Collections;
using DG.Tweening;
using Game.Network;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Objects")]
    public GameObject mainPanel;
    public GameObject logo;
    public GameObject logoStartText;
    public GameObject logoContainer;
    public GameObject logoOutline;
    public GameObject star;
    public GameObject[] buttons;
    private RectTransform _mainPanelTransform;
    private RectTransform _logoContainerTransform;
    private RectTransform _starTransform;
    private RawImage _logoRawImage;
    private RawImage _logoContainerRawImage;
    private RawImage _logoOutlineRawImage;

    [Header("Settings")]
    public Slider sensitivity;

    private bool _init;
    private Tween logoScale;
    private Tween logoMove;

    [Header("Utils")]
    public TransitionManager transitionManager;
    public CustomNetworkManager customNetworkManager;
    public SectionManager sectionManager;
    private bool isGameWindowFocused = true;
    private void Awake()
    {
        logoContainer.SetActive(true);
        _mainPanelTransform = mainPanel.GetComponent<RectTransform>();
        _logoContainerTransform = logoContainer.GetComponent<RectTransform>();
        _starTransform = star.GetComponent<RectTransform>();

        _logoRawImage = logo.GetComponent<RawImage>();
        _logoContainerRawImage = logoContainer.GetComponent<RawImage>();
        _logoOutlineRawImage = logoOutline.GetComponent<RawImage>();

        if (PlayerPrefs.HasKey("sens"))
        {
            sensitivity.value = PlayerPrefs.GetFloat("sens");
            return;
        }

        PlayerPrefs.SetFloat("sens", 1f);
    }

    /*
    !!!!!!!!!!! КОСТЫЛИ !!!!!!!!!!!
    */
    public void ButtonPlayHost()
    {
        if (sectionManager.CurrentSubSection == 0)
            transitionManager.TransitionIn().OnComplete(customNetworkManager.StartHost);
    }

    public void ButtonPlayJoin()
    {
        if (sectionManager.CurrentSubSection == 0)
            sectionManager.SubsectionChange(1);
    }

    public void ButtonPlayBack()
    {
        if (sectionManager.CurrentSubSection == 0)
            sectionManager.Transition(0);
    }
    /*
    !!!!!!! конец костылей !!!!!!!
    */

    public void SetSens(float value)
    {
        PlayerPrefs.SetFloat("sens", value);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        transitionManager.TransitionOut();
        mainPanel.SetActive(false);
        _logoContainerTransform.localScale = new Vector3(0.7f, 0.7f, 1f);

        logoScale = _logoContainerTransform?.DOScale(1f, 0.4f);
        logoMove = _logoContainerTransform?.DOLocalMoveX(3f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutCubic);

    }

    private void Update()
    {
        _logoOutlineRawImage.uvRect = new Rect(
                    _logoOutlineRawImage.uvRect.x - 0.05f * Time.deltaTime,
                    _logoOutlineRawImage.uvRect.y + 0.05f * Time.deltaTime,
                    1f,
                    1f);

        _starTransform.localEulerAngles = new Vector3(
            _starTransform.localEulerAngles.x,
            35 + math.sin(Time.time / 2) * 10,
            _starTransform.localEulerAngles.z + 10f * Time.deltaTime
        );

        _mainPanelTransform.localEulerAngles = new Vector3(
            (Mathf.InverseLerp(0, Screen.height, Mouse.current.position.ReadValue().y) - 0.5f) * 2f,
            (-Mathf.InverseLerp(0, Screen.width, Mouse.current.position.ReadValue().x) + 0.5f) * 2f,
            0f);

        if (_init == false & isGameWindowFocused)
        {
            InputSystem.onAnyButtonPress
                .CallOnce(btn =>
                {
                    _logoContainerRawImage.DOKill();
                    _logoContainerTransform.DOKill();
                    _logoRawImage.DOKill();
                    MenuReveal();
                });
            return;
        }

        if (!isGameWindowFocused)
        {
            MenuUnreveal();
        }
    }

    private void OnDestroy()
    {
        DOTween.KillAll();
    }

    private void OnApplicationFocus(bool focus)
    {
        isGameWindowFocused = focus;
    }

    private void MenuReveal()
    {
        if (!mainPanel)
            return;

        mainPanel.SetActive(true);

        _logoContainerTransform?.DOScale(0.85f, 0.7f)
            .SetEase(Ease.OutExpo);
        _logoContainerRawImage.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        _logoContainerRawImage?.DOColor(new Color(0.11f, 0.11f, 0.11f, 0f), 0.7f)
            .SetEase(Ease.OutExpo)
            .OnComplete(() =>
            {
                logoContainer.SetActive(false);
            });

        _logoRawImage.DOColor(new Color(1f, 1f, 1f, 0f), 0.7f)
            .SetEase(Ease.OutExpo);

        _init = true;
    }

    private void MenuUnreveal()
    {
        if (!logoContainer)
            return;

        logoContainer.SetActive(true);

        _logoContainerTransform?.DOScale(1f, 0.7f)
            .SetEase(Ease.OutExpo);

        _logoContainerRawImage?.DOColor(new Color(0.11f, 0.11f, 0.11f, 1f), 0.7f)
            .SetEase(Ease.OutExpo);

        _logoRawImage.DOColor(new Color(1f, 1f, 1f, 1f), 0.7f)
            .SetEase(Ease.OutExpo)
            .OnComplete(() =>
            {
                mainPanel.SetActive(false);
            });

        _init = false;
    }
}