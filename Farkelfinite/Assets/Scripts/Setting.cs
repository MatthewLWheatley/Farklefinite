using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class Setting : MonoBehaviour
{
    public static Setting _instance;

    [SerializeField] float _mainVolume = 0.2f;
    [SerializeField] float _sfxVolume = 1.0f;
    [SerializeField] float _ambientVolume = 1.0f;
    [SerializeField] float _musicVolume = 1.0f;

    public GameObject mainVolumeSlider;
    public GameObject sfxVolumeSlider;
    public GameObject ambientVolumeSlider;
    public GameObject musicVolumeSlider;

    [SerializeField] float _gameSpeed = 1.0f;
    [SerializeField] float _screenShake = 1.0f;

    public GameObject SpeedButton;
    public GameObject screenShakeSlider;


    public static Setting Instance { get { return _instance; } }

    private void Awake()
    {
        if (PlayerPrefs.HasKey("_mainVolume")) _mainVolume = PlayerPrefs.GetFloat("_mainVolume");
        if (PlayerPrefs.HasKey("_sfxVolume")) _mainVolume = PlayerPrefs.GetFloat("_sfxVolume");
        if (PlayerPrefs.HasKey("_ambientVolume")) _mainVolume = PlayerPrefs.GetFloat("_ambientVolume");
        if (PlayerPrefs.HasKey("_musicVolume")) _mainVolume = PlayerPrefs.GetFloat("_musicVolume");
        if (PlayerPrefs.HasKey("_gameSpeed")) _mainVolume = PlayerPrefs.GetFloat("_gameSpeed");
        if (PlayerPrefs.HasKey("_screenShake")) _mainVolume = PlayerPrefs.GetFloat("_screenShake");



        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        transform.GetChild(0).GetComponent<Canvas>().worldCamera = Camera.main;
        
        mainVolumeSlider.GetComponent<Slider>().value = _mainVolume; 
        mainVolumeSlider.GetComponent<Slider>().onValueChanged.AddListener((value) => MainVolumeChange());
        sfxVolumeSlider.GetComponent<Slider>().value = _sfxVolume;
        sfxVolumeSlider.GetComponent<Slider>().onValueChanged.AddListener((value) => SfxVolumeChange());
        ambientVolumeSlider.GetComponent<Slider>().value = _ambientVolume;
        ambientVolumeSlider.GetComponent<Slider>().onValueChanged.AddListener((value) => AmbientVolumeChange());
        musicVolumeSlider.GetComponent<Slider>().value = _musicVolume;
        musicVolumeSlider.GetComponent<Slider>().onValueChanged.AddListener((value) => MusicVolumeChange());

        SpeedButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Animation Speed: " + _gameSpeed.ToString();
        SpeedButton.GetComponent<Button>().onClick.AddListener(() => ChangeSpeed());

        screenShakeSlider.GetComponent<Slider>().value = _screenShake;
        screenShakeSlider.GetComponent<Slider>().onValueChanged.AddListener((value) => AnimationSpeedChange());
    }


    
    public void MainVolumeChange() 
    {
        _mainVolume = mainVolumeSlider.GetComponent<Slider>().value;
    }

    public void SfxVolumeChange()
    {
        _sfxVolume = sfxVolumeSlider.GetComponent<Slider>().value;
    }
    
    public void AmbientVolumeChange()
    {
        _ambientVolume = ambientVolumeSlider.GetComponent<Slider>().value;
    }

    public void MusicVolumeChange()
    {
        _musicVolume = musicVolumeSlider.GetComponent<Slider>().value;
    }

    public void ChangeSpeed()
    {
        _gameSpeed *= 2;
        if (_gameSpeed >= 16) _gameSpeed = 1f;

        SpeedButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Animation Speed: " + _gameSpeed.ToString();
    }

    public void AnimationSpeedChange()
    {
        _screenShake = screenShakeSlider.GetComponent<Slider>().value;
    }

    public void OnDestroy()
    {
        PlayerPrefs.SetFloat("_mainVolume", _mainVolume);
        PlayerPrefs.SetFloat("_sfxVolume", _sfxVolume);
        PlayerPrefs.SetFloat("_ambientVolume", _ambientVolume);
        PlayerPrefs.SetFloat("_musicVolume", _musicVolume);
        PlayerPrefs.SetFloat("_gameSpeed", _gameSpeed);
        PlayerPrefs.SetFloat("_screenShake", _screenShake);
    }

    private void OnDisable()
    {
        PlayerPrefs.SetFloat("_mainVolume", _mainVolume);
        PlayerPrefs.SetFloat("_sfxVolume", _sfxVolume);
        PlayerPrefs.SetFloat("_ambientVolume", _ambientVolume);
        PlayerPrefs.SetFloat("_musicVolume", _musicVolume);
        PlayerPrefs.SetFloat("_gameSpeed", _gameSpeed);
        PlayerPrefs.SetFloat("_screenShake", _screenShake);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("_mainVolume", _mainVolume);
        PlayerPrefs.SetFloat("_sfxVolume", _sfxVolume);
        PlayerPrefs.SetFloat("_ambientVolume", _ambientVolume);
        PlayerPrefs.SetFloat("_musicVolume", _musicVolume);
        PlayerPrefs.SetFloat("_gameSpeed", _gameSpeed);
        PlayerPrefs.SetFloat("_screenShake", _screenShake);
    }
}
