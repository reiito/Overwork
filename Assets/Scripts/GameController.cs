using UnityEngine;
using UnityEngine.UI;

public enum DayState
{
    MORNING, AFTERNOON, NIGHT
}

public enum GameState
{
    WORKING, EAT, SLEEP, BREAK
}

enum EndState
{
    NONE, COMPLETE, REALISED, TIME_OUT, STARVED, EXHUASTED, DEPRIVED
}

public class GameController : MonoBehaviour
{
    public Image startScreen;

    public Image workingScreen;
    public Button actionButton;
    public Slider timeLeftSlider;
    public Text timeLeftText;

    public GameObject actionScreen;
    public GameObject blurScreen;
    public GameObject endScreen;

    public int MaxActionClicks;
    public int missedThreshold;
    public int buttonChance;
    public float secretTime;
    public float dueTime;
    public float resetTime;

    public Vector2 buttonAppearThreshold;

    Object[] workingImages;

    int currentImageIndex = 0;
    int actionClickCount = 0;
    int dayClickCount = 0;

    DayState dayState;
    GameState gameState;
    EndState endState;

    int[] workingLimits = { 50, 125, 175 };

    int buttonClickedCount = 0;

    int[] missedAction = { 0, 0, 0 };

    int daysPast = 0;

    bool gameOver = false;

    bool overButton = false;

    float timeLeft;

    float buttonPadding = 100;

    bool started = false;

    int currentButtonChance;

    float currentResetTime;

    float secretTimer;

    bool onActionScreen;

    void Start()
    {
        ResetGame();

        // load images into game array
        workingImages = Resources.LoadAll("Images", typeof(Sprite));
    }

    void Update()
    {
        // quit game
        if (Input.GetKeyDown("escape"))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        if (Input.GetMouseButtonDown(2))
        {
            ResetGame();
        }

        // game over state
        if (gameOver)
        {
            SetEndText();
            blurScreen.SetActive(false);
            endScreen.SetActive(true);

            currentResetTime -= Time.deltaTime;
            if (currentResetTime < 0)
            {
                ResetGame();
            }

            return;
        }

        if (started)
        {
            // time limit
            if (!onActionScreen)
            {
                timeLeft -= Time.deltaTime;
                timeLeftSlider.value = timeLeft / dueTime;
                timeLeftText.text = "Due: " + timeLeft.ToString("0.00");

                secretTimer += Time.deltaTime;
            }

            // handle time out loss
            if (timeLeft <= 0)
            {
                gameOver = true;
                endState = EndState.TIME_OUT;
            }

            // code for the secret ending counter
            if (secretTimer > secretTime)
            {
                gameOver = true;
                endState = EndState.REALISED;
                return;
            }

            // handle when the player is clicking through the game
            if (!overButton)
            {
                // clicking through screen shot state
                if (gameState == GameState.WORKING)
                {
                    // enable screen blur for only working
                    blurScreen.SetActive(true);

                    // handle mouse button event (both buttons)
                    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                    {
                        // secret time tracker
                        secretTimer = 0;

                        // check if under image limit
                        if (currentImageIndex < workingImages.Length)
                        {
                            workingScreen.sprite = workingImages[currentImageIndex] as Sprite;
                            currentImageIndex += buttonClickedCount + 1;
                        }
                        // win game state: got work done
                        else
                        {
                            gameOver = true;
                            endState = EndState.COMPLETE;
                        }

                        // chance of showing the action button (varies depending on how much the player has missed, increasing if at a higher count)
                        if (dayClickCount >= (workingLimits[(int)dayState] - Random.Range(buttonAppearThreshold.x, buttonAppearThreshold.y)) && dayClickCount <= workingLimits[(int)dayState])
                        {
                            if (Random.Range(0, currentButtonChance) == 0)
                            {
                                actionButton.gameObject.SetActive(true);
                                actionButton.GetComponent<RectTransform>().position = new Vector3(Random.Range(buttonPadding, Screen.width - buttonPadding), Random.Range(buttonPadding, Screen.height - buttonPadding), 1);
                                SetButtonText();
                            }
                            else
                            {
                                actionButton.gameObject.SetActive(false);
                            }
                        }

                        // check if day state has ended
                        if (dayClickCount >= workingLimits[(int)dayState])
                        {
                            missedAction[(int)dayState]++;
                            actionButton.gameObject.SetActive(false);

                            int highestMissed = 0;
                            for (int i = 0; i < missedAction.Length; i++)
                            {
                                if (highestMissed < missedAction[i])
                                {
                                    highestMissed = missedAction[i];
                                }
                            }

                            blurScreen.GetComponent<Image>().material.SetFloat("_Size", ((float)highestMissed / (float)(missedThreshold + 1)));

                            if (CheckMissedLimitReached())
                            {
                                gameOver = true;
                                return;
                            }

                            if (dayState == DayState.NIGHT)
                            {
                                actionScreen.GetComponentInChildren<Text>().text = "Sleeping.";
                                actionScreen.GetComponentInChildren<Image>().color = Color.red;
                                gameState = GameState.SLEEP;
                                actionScreen.SetActive(true);
                            }
                            else
                            {
                                dayState++;
                            }
                        }

                        dayClickCount++;
                    }
                }
                // clicking through activity screen
                else
                {
                    blurScreen.SetActive(false);

                    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) && !overButton)
                    {
                        actionScreen.GetComponentInChildren<Text>().text += ".";
                        actionClickCount++;
                        if (actionClickCount >= MaxActionClicks)
                        {
                            actionClickCount = 0;
                            actionScreen.SetActive(false);

                            switch (dayState)
                            {
                                case DayState.MORNING:
                                    dayState = DayState.AFTERNOON;
                                    dayClickCount = workingLimits[0];
                                    break;
                                case DayState.AFTERNOON:
                                    dayState = DayState.NIGHT;
                                    dayClickCount = workingLimits[1];
                                    break;
                                case DayState.NIGHT:
                                    ResetDay();
                                    break;
                            }

                            gameState = GameState.WORKING;
                            actionButton.gameObject.SetActive(false);

                            onActionScreen = false;
                        }
                    }
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // start screen
                if (!started && startScreen.gameObject.activeInHierarchy)
                {
                    startScreen.gameObject.SetActive(false);
                    started = true;
                    return;
                }
            }
        }
    }


    /// <summary>
    /// Switch button text depending on the current state of day
    /// </summary>
    void SetButtonText()
    {
        switch (dayState)
        {
            case DayState.MORNING:
                actionButton.GetComponentInChildren<Text>().text = "Eat";
                break;
            case DayState.AFTERNOON:
                actionButton.GetComponentInChildren<Text>().text = "Break";
                break;
            case DayState.NIGHT:
                actionButton.GetComponentInChildren<Text>().text = "Sleep";
                break;
        }

    }


    /// <summary>
    /// Handle when the action button is pressed
    /// </summary>
    public void ButtonClick()
    {
        switch (dayState)
        {
            case DayState.MORNING:
                actionScreen.GetComponentInChildren<Text>().text = "Eating.";
                gameState = GameState.EAT;
                break;
            case DayState.AFTERNOON:
                actionScreen.GetComponentInChildren<Text>().text = "Break.";
                gameState = GameState.BREAK;
                break;
            case DayState.NIGHT:
                actionScreen.GetComponentInChildren<Text>().text = "Sleeping.";
                gameState = GameState.SLEEP;
                break;
        }

        currentButtonChance++;
        buttonClickedCount++;
        overButton = false;

        actionScreen.SetActive(true);
        actionButton.gameObject.SetActive(false);

        onActionScreen = true;
    }


    /// <summary>
    /// Set if over button
    /// </summary>
    public void OverButton()
    {
        overButton = true;
    }
    /// <summary>
    /// Set if not over button
    /// </summary>
    public void NotOverButton()
    {
        overButton = false;
    }


    /// <summary>
    /// Resets the day including counters and enums
    /// </summary>
    void ResetDay()
    {
        if (CheckMissedLimitReached())
        {
            gameOver = true;
            return;
        }
        dayClickCount = 0;
        daysPast++;
        if (currentButtonChance > 0)
            currentButtonChance--;
        dayState = DayState.MORNING;

        actionScreen.GetComponentInChildren<Image>().color = Color.green;
    }


    /// <summary>
    /// End text handler
    /// </summary>
    void SetEndText()
    {
        switch (endState)
        {
            case EndState.COMPLETE:
                endScreen.GetComponentInChildren<Text>().text = "Done!";
                endScreen.GetComponentInChildren<Image>().color = Color.black;
                break;
            case EndState.REALISED:
                endScreen.GetComponentInChildren<Text>().text = "Enlightened!";
                endScreen.GetComponentInChildren<Image>().color = Color.green;
                break;
            case EndState.TIME_OUT:
                endScreen.GetComponentInChildren<Text>().text = "Over due.";
                endScreen.GetComponentInChildren<Image>().color = Color.red;
                break;
            case EndState.STARVED:
                endScreen.GetComponentInChildren<Text>().text = "Starving.";
                endScreen.GetComponentInChildren<Image>().color = Color.red;
                break;
            case EndState.EXHUASTED:
                endScreen.GetComponentInChildren<Text>().text = "Burnt out.";
                endScreen.GetComponentInChildren<Image>().color = Color.red;
                break;
            case EndState.DEPRIVED:
                endScreen.GetComponentInChildren<Text>().text = "Sleep deprived.";
                endScreen.GetComponentInChildren<Image>().color = Color.red;
                break;
        }
    }


    /// <summary>
    /// Checks if the player has missed a button in this stage of the day
    /// </summary>
    /// <returns> missed button </returns>
    bool CheckMissedLimitReached()
    {
        if (missedAction[0] >= missedThreshold)
        {
            endState = EndState.STARVED;
            return true;
        }
        else if (missedAction[1] >= missedThreshold)
        {
            endState = EndState.EXHUASTED;
            return true;
        }
        else if (missedAction[2] >= missedThreshold)
        {
            endState = EndState.DEPRIVED;
            return true;
        }

        return false;
    }

    void ResetGame()
    {
        gameOver = false;

        currentButtonChance = buttonChance;
        timeLeft = dueTime;
        currentResetTime = resetTime;

        // set up start screen
        started = false;
        startScreen.gameObject.SetActive(true);

        // init counters
        currentImageIndex = 0;
        actionClickCount = 0;
        dayClickCount = 0;
        for (int i = 0; i < missedAction.Length; i++)
        {
            missedAction[i] = 0;
        }
        buttonClickedCount = 0;
        daysPast = 0;
        secretTimer = 0;

        // init game states
        dayState = DayState.MORNING;
        gameState = GameState.WORKING;
        endState = EndState.NONE;

        // set-up UI
        actionScreen.SetActive(false);
        blurScreen.SetActive(false);
        endScreen.SetActive(false);
        actionButton.gameObject.SetActive(false);

        // init shader value
        blurScreen.GetComponent<Image>().material.SetFloat("_Size", 0);

        actionScreen.GetComponentInChildren<Image>().color = Color.green;
        endScreen.GetComponentInChildren<Image>().color = Color.black;
    }
}