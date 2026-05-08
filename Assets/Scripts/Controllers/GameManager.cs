using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private const float TIME_ATTACK_DURATION_SECONDS = 60f;

    public event Action<eStateGame> StateChangedAction = delegate { };

    [SerializeField]
    private Transform m_trayRoot;



    public enum eLevelMode
    {
        TIMER,
        MOVES
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }

    
    private GameSettings m_gameSettings;


    private BoardController m_boardController;
    private TrayController m_trayController;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;
    private Coroutine m_gameOverRoutine;

    private void Awake()
    {
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        ClearLevel();

        bool isTimeAttack =
            mode == eLevelMode.TIMER;

        TrayController tray =
            new GameObject("TrayController")
            .AddComponent<TrayController>();

        tray.Setup(
            m_trayRoot,
            isTimeAttack);
        m_trayController = tray;

        m_boardController =
            new GameObject("BoardController")
            .AddComponent<BoardController>();

        m_boardController.StartGame(
            this,
            m_gameSettings,
            tray,
            isTimeAttack);

        // if (mode == eLevelMode.MOVES)
        // {
        //     m_levelCondition =
        //         gameObject.AddComponent<LevelMoves>();

        //     m_levelCondition.Setup(
        //         m_gameSettings.LevelMoves,
        //         m_uiMenu.GetLevelConditionView(),
        //         m_boardController);
        // }
        // else if (mode == eLevelMode.TIMER)
        // {
        //     m_levelCondition =
        //         gameObject.AddComponent<LevelTime>();

        //     m_levelCondition.Setup(
        //         m_gameSettings.LevelMoves,
        //         m_uiMenu.GetLevelConditionView(),
        //         this);
        // }

        // m_levelCondition.ConditionCompleteEvent += GameOver;

        if (isTimeAttack)
        {
            m_levelCondition =
                gameObject.AddComponent<LevelTime>();

            m_levelCondition.Setup(
                TIME_ATTACK_DURATION_SECONDS,
                m_uiMenu.GetLevelConditionView(),
                this);

            m_levelCondition.ConditionCompleteEvent +=
                OnTimeAttackTimeout;

            m_uiMenu.SetLevelConditionVisible(true);
        }
        else
        {
            m_levelCondition = null;
            m_uiMenu.SetLevelConditionVisible(false);
        }

        State = eStateGame.GAME_STARTED;
    }

    private void OnTimeAttackTimeout()
    {
        if (m_boardController != null)
        {
            m_boardController.TriggerTimeAttackTimeout();
        }
    }

    public void GameOver()
    {
        if (m_gameOverRoutine != null)
        {
            StopCoroutine(m_gameOverRoutine);
        }

        m_gameOverRoutine =
            StartCoroutine(WaitBoardController());
    }

    internal void ClearLevel()
    {
        if (m_gameOverRoutine != null)
        {
            StopCoroutine(m_gameOverRoutine);
            m_gameOverRoutine = null;
        }

        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }

        if (m_trayController)
        {
            Destroy(m_trayController.gameObject);
            m_trayController = null;
        }

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
            m_levelCondition.ConditionCompleteEvent -=
                OnTimeAttackTimeout;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }

        m_uiMenu.SetLevelConditionVisible(false);
    }

    private IEnumerator WaitBoardController()
    {
        while (m_boardController != null &&
               m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        if (m_boardController == null)
        {
            m_gameOverRoutine = null;
            yield break;
        }

        State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
            m_levelCondition.ConditionCompleteEvent -=
                OnTimeAttackTimeout;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }

        m_gameOverRoutine = null;
    }

    public void StartAutoPlayWin()
    {
        LoadLevel(eLevelMode.MOVES);

        StartCoroutine(AutoPlayWinCoroutine());
    }

    public void StartAutoPlayLose()
    {
        LoadLevel(eLevelMode.MOVES);
        StartCoroutine(DelayAutoPlayLose());
    }

    private IEnumerator AutoPlayWinCoroutine()
    {
        yield return new WaitForSeconds(1f);

        while (State == eStateGame.GAME_STARTED)
        {
            if (!m_boardController.IsBusy)
            {
                List<Cell> group =  
                    m_boardController
                    .GetTripleMatchCells();

                if (group != null)
                {
                    foreach (Cell cell in group)
                    {
                        if (cell != null &&
                            !cell.IsEmpty)
                        {
                            m_boardController
                                .AutoSelect(cell);

                            yield return
                                new WaitForSeconds(0.5f);
                        }
                    }
                }
            }

            yield return null;
        }
    }

    private IEnumerator DelayAutoPlayLose()
    {
        yield return null;

        m_boardController.StartAutoPlayLose();
    }
}
