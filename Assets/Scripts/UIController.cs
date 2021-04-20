using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class UIController : ControllerBase<UIController>
{
    bool started = false;
    public void setStartUpUI(bool iamhost = false)
    {
        started = true;
        titleScreen.SetActive(false);
        playerListObject.SetActive(true);
        StartGameButton.SetActive(false);
        waitingText.SetActive(true);
        if (iamhost)
        {
            StartGameButton.SetActive(true);
            PlayAgainButton.SetActive(true);
            waitingText.SetActive(false);
            RestartGameButton.SetActive(true);

            settingsResetButton.interactable = true;

            for(int i = 0; i < settingsList.Count; i++)
            {
                settingsList[i].interactable = true;
            }

            for (int i = 0; i < numberFrequencies.Count; i++)
            {
                numberFrequencies[i].interactable = true;
            }
            
        }
        //settingsChangeEvent();
    }

    public void setGameUI(bool ishost = false)
    {
        Sceneobjects.current.WildColourIndicator.transform.parent.gameObject.SetActive(true);
        WinScreen.SetActive(false);
        playerListObject.SetActive(false);
        GameScreen.SetActive(true);
        //StartGameButton.SetActive(false);
    }

    public void updatePlayerList()
    {
        for(int i = 0; i < 4; i++)
        {
            if (i < Gameplay.current.players.Count) {
                playerList[i].text = "ID " + i.ToString() + " | " + Gameplay.current.players[i].playerName;
            }
            else
            {
                playerList[i].text = "[Empty]";
            }
        }
    }

    public InputField playerNameField;
    public InputField addressField;

    public GameObject StartGameButton;
    public GameObject waitingText;
    public GameObject titleScreen;

    public GameObject GameScreen;

    public GameObject RestartGameButton;

    public GameObject playerListObject;

    public GameObject ConnectingBox;

    public GameObject SelectColourBox;

    public GameObject WinScreen;

    public List<Text> playerWinStatList;

    public GameObject PlayAgainButton;

    public GameObject DisconnectedBox;

    public Text winText;

    public List<Text> playerList = new List<Text>();

    public List<Toggle> settingsList = new List<Toggle>();

    public Button settingsResetButton;

    public List<Slider> numberFrequencies = new List<Slider>();

    public GameObject passButton;

    public void StartGame()
    {
        SaveSettings();
        Debug.Log("Reached UI StartGame");
        Gameplay.current.StartGame();
        setGameUI(true);
    }

    public void settingsChangeEvent(int settingchanged)
    {
        if (settingchanged != -1 && !started)
        {
            return;
        }
        /*for (int i = 0; i < 4; i++)
        {
            if (i == settingchanged)
            {
                settingsList[i].isOn = true;
            }
            else
            {
                settingsList[i].isOn = false;
            }
        }*/
        
        if (settingchanged == 0)
        {
            for (int i = 0; i < settingsList.Count; i++)
            {
                if(i != settingchanged)
                settingsList[i].isOn = Sceneobjects.current.recommended_ruless[i];

                settingsList[settingchanged].isOn = false;
            }
        }
        else if (settingchanged == 1)
        {
            for (int i = 0; i < settingsList.Count; i++)
            {
                if (i != settingchanged)
                    settingsList[i].isOn = Sceneobjects.current.uno_unofficial_ruless[i];
                settingsList[settingchanged].isOn = false;
            }
        }
        else if (settingchanged == 2)
        {
            for (int i = 0; i < settingsList.Count; i++)
            {
                if (i != settingchanged)
                    settingsList[i].isOn = Sceneobjects.current.uno_official_ruless[i];
                settingsList[settingchanged].isOn = false;
            }
        }
        else if (settingchanged == 3)
        {
            for (int i = 0; i < settingsList.Count; i++)
            {
                if (i != settingchanged)
                    settingsList[i].isOn = Sceneobjects.current.uno_house_ruless[i];
                settingsList[settingchanged].isOn = false;
            }
        }
        else
        {
            if (settingchanged == 4)
            {
                settingsList[4].isOn = false;
            }
            else
            {
                settingsList[4].isOn = true;
            }

            if (settingchanged == 5 && settingsList[5].isOn)
            {
                settingsList[6].isOn = false;
            }


            if (settingchanged >= 18 && settingchanged <= 20)
            {
                for (int i = 18; i < 21; i++)
                {

                        settingsList[i].isOn = false;
                }
                
            }


            for (int i = 0; i < 4; i++)
            {

                    settingsList[i].isOn = false;
                
            }
        }
        Gameplay.current.CmdSettingsChangeEvent();

        //StartCoroutine(settingsChanger(settingchanged));
        
    }

    public void settingsChangeConsequence(List<bool> _settings, List<int> _cardProbabilities)
    {
        if (settingsList[0].interactable)
        {
            return;
        }
        for (int i = 0; i < settingsList.Count; i++)
        {
            settingsList[i].isOn = _settings[i];
        }
        for (int i = 0; i < numberFrequencies.Count; i++)
        {
            numberFrequencies[i].value = _cardProbabilities[i];
        }
    }

    public void ResetSettingsToDefault()
    {
        ResetSavedValues();

        for (int i = 0; i < Sceneobjects.current.recommended_ruless.Length; i++)
        {
            settingsList[i].isOn = Sceneobjects.current.recommended_ruless[i];
            if (settingsList[i].isOn) {
                PlayerPrefs.SetInt("setting" + i.ToString(), 1);
            }
            else
            {
                PlayerPrefs.SetInt("setting" + i.ToString(), 0);
            }
        }

        for (int i = 0; i < Sceneobjects.current.defaultCardProbabilities.Length; i++)
        {
            numberFrequencies[i].value = Sceneobjects.current.defaultCardProbabilities[i];
            PlayerPrefs.SetInt("cardFreq" + i.ToString(), (int)numberFrequencies[i].value);
        }


    }

    public void LeaveGame()
    {
        if (!Gameplay.current.isServer)
        {
            Sceneobjects.current.netManager.StopClient();
        }
        else
        {
            Sceneobjects.current.netManager.StopHost();
        }

        SceneManager.LoadScene("SampleScene");
    }

    public void CreateGame()
    {
        SaveSavedValues();
        Sceneobjects.current.netManager.StartHost();
        ConnectingBox.SetActive(true);
    }

    public void JoinGame()
    {
        SaveSavedValues();
        PlayerPrefs.SetString("playerName", playerNameField.text);
        Sceneobjects.current.netManager.StartClient();
        ConnectingBox.SetActive(true);
    }

    void SaveSavedValues()
    {
        PlayerPrefs.SetString("playerName", playerNameField.text);
        PlayerPrefs.SetString("connectAddress", addressField.text);

 
    }

    void SaveSettings()
    {
        for (int i = 0; i < Sceneobjects.current.recommended_ruless.Length; i++)
        {
            if (settingsList[i].isOn)
            {
                PlayerPrefs.SetInt("setting" + i.ToString(), 1);
            }
            else
            {
                PlayerPrefs.SetInt("setting" + i.ToString(), 0);
            }
        }

        for (int i = 0; i < Sceneobjects.current.defaultCardProbabilities.Length; i++)
        {
            PlayerPrefs.SetInt("cardFreq" + i.ToString(), (int)numberFrequencies[i].value);
        }
    }

    void loadSavedValues()
    {
        playerNameField.text = PlayerPrefs.GetString("playerName", "RandomName");
        addressField.text = PlayerPrefs.GetString("connectAddress", "localhost");

        Debug.Log(settingsList.Count);
        Debug.Log("recommended rules length = " + Sceneobjects.current.recommended_ruless.Length);

        for (int i = 0; i < settingsList.Count; i++)
        {
            int def = 0;
            if (Sceneobjects.current.recommended_ruless[i])
            {
                def = 1;
            }
            int temp = PlayerPrefs.GetInt("setting" + i.ToString(), def);
            
            if (temp != 0)
            {
                settingsList[i].isOn = true;
            }
            else
            {
                settingsList[i].isOn = false;
            }
        }

        for (int i = 0; i < numberFrequencies.Count; i++)
        {
            int temp = PlayerPrefs.GetInt("cardFreq" + i.ToString(), Sceneobjects.current.defaultCardProbabilities[i]);
            numberFrequencies[i].value = temp;
        }
    }

    void ResetSavedValues()
    {
        PlayerPrefs.SetString("playerName", "RandomName");
        PlayerPrefs.SetString("connectAddress", "localhost");
    }

    public void updateNetAddress()
    {
        Sceneobjects.current.netManager.setAddress(addressField.text);
    }

    // Start is called before the first frame update
    void Start()
    {
       // PlayerPrefs.DeleteAll();
        loadSavedValues();
    }


    // Update is called once per frame
    void Update()
    {
        if (ConnectingBox.activeSelf)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                ConnectingBox.SetActive(false);
                setStartUpUI(true);

            }
            // client-only
            else if (NetworkClient.isConnected)
            {
                ConnectingBox.SetActive(false);
                setStartUpUI();
            }
            // server-only
            else if (NetworkServer.active)
            {

            }
        }
    }
}
