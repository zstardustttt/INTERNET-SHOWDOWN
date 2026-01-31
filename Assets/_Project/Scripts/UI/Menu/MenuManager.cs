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
    private void Awake()
    {
        logoContainer.SetActive(true);
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
    private void OnEnable()
    {
        TransitionManager.OnTransitionInComplete += customNetworkManager.StartHost;
    }

    private void OnDisable()
    {
        TransitionManager.OnTransitionInComplete -= customNetworkManager.StartHost;

    }

    public void ButtonHost()
    {
        transitionManager.TransitionIn();
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

        if (_init == false)
        {
            InputSystem.onAnyButtonPress
                .CallOnce(btn =>
                {
                    MenuReveal();
                });
            return;
        }
    }
    private void MenuReveal()
    {
        logoMove?.Kill(false);
        logoScale?.Kill(false);

        mainPanel.SetActive(true);

        _logoContainerTransform?.DORewind();
        _logoContainerTransform?.DOScale(0.85f, 0.7f)
            .SetEase(Ease.OutExpo);
        _logoContainerRawImage.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        _logoContainerRawImage.DORewind();
        _logoContainerRawImage?.DOColor(new Color(0.11f, 0.11f, 0.11f, 0f), 0.8f)
            .SetEase(Ease.OutCirc)
            .OnComplete(() =>
            {
                logoContainer.SetActive(false);
            });

        _logoRawImage.DORewind();
        _logoRawImage.DOColor(new Color(1f, 1f, 1f, 0f), 0.8f)
            .SetEase(Ease.OutCirc);

        _init = true;
    }
}