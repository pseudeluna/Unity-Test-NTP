using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent =
        delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private Camera m_cam;

    private TrayController m_tray;

    private GameManager m_gameManager;

    private bool m_gameOver;

    private bool m_isTimeAttack;
    
    private Coroutine m_autoPlayRoutine;

    public void StartGame(
        GameManager manager,
        GameSettings settings,
        TrayController tray,
        bool isTimeAttack)
    {
        m_cam = Camera.main;

        m_gameManager = manager;

        m_tray = tray;

        m_isTimeAttack = isTimeAttack;


        m_board = new Board(
            transform,
            settings);

        m_board.Fill();

        if (!m_isTimeAttack)
        {
            m_tray.OnTrayOverflow += GameOver;
        }
    }

    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos =
                m_cam.ScreenToWorldPoint(
                    Input.mousePosition);

            if (m_isTimeAttack)
            {
                bool tappedTrayItem =
                    m_tray.TryReturnItemAtPosition(
                        pos,
                        () => { IsBusy = false; });

                if (tappedTrayItem)
                {
                    IsBusy = true;
                    return;
                }
            }

            RaycastHit2D hit =
                Physics2D.Raycast(
                    pos,
                    Vector2.zero);

            if (hit.collider != null)
            {
                Cell cell =
                    hit.collider.GetComponent<Cell>();

                if (cell != null &&
                    !cell.IsEmpty)
                {
                    SelectCell(cell);
                }
            }
        }
    }

    public void SelectCell(Cell cell)
    {
        IsBusy = true;

        Item item = cell.Item;

        cell.Free();

        OnMoveEvent();

        m_tray.AddItem(item, cell, () =>
        {
            CheckWin();

            IsBusy = false;
        });
    }

    private void CheckWin()
    {
        if (m_board.IsBoardEmpty())
        {
            FindObjectOfType<UIMainManager>()
                .ShowWinPanel();
        }
    }

    private void GameOver()
    {
        if (m_gameOver) return;

        m_gameOver = true;

        Debug.Log("LOSE");
        FindObjectOfType<UIMainManager>()
            .ShowLosePanel();

        if (m_gameManager != null)
        {
            m_gameManager.GameOver();
        }
    }

    public void TriggerTimeAttackTimeout()
    {
        if (!m_isTimeAttack) return;

        GameOver();
    }

    public void Clear()
    {
        m_board.Clear();
    }

    public void StartAutoPlayWin()
    {
        if (m_autoPlayRoutine != null)
        {
            StopCoroutine(m_autoPlayRoutine);
        }

        m_autoPlayRoutine =
            StartCoroutine(AutoPlayWinCoroutine());
    }

    public void StartAutoPlayLose()
    {
        if (m_autoPlayRoutine != null)
        {
            StopCoroutine(m_autoPlayRoutine);
        }

        m_autoPlayRoutine =
            StartCoroutine(AutoPlayLoseCoroutine());
    }

    public List<Cell> GetTripleMatchCells()
    {
        List<Cell> cells =
            m_board.GetAllFilledCells();

        Dictionary<int, List<Cell>> groups =
            new Dictionary<int, List<Cell>>();

        foreach (Cell cell in cells)
        {
            int id = cell.Item.ItemID;

            if (!groups.ContainsKey(id))
            {
                groups[id] = new List<Cell>();
            }

            groups[id].Add(cell);
        }

        foreach (var pair in groups)
        {
            if (pair.Value.Count >= 3)
            {
                return pair.Value
                    .Take(3)
                    .ToList();
            }
        }

        return null;
    }

    public void AutoSelect(Cell cell)
    {
        if (cell == null) return;

        SelectCell(cell);
    }
    private IEnumerator AutoPlayWinCoroutine()
    {
        while (!m_gameOver)
        {
            if (IsBusy)
            {
                yield return null;
                continue;
            }

            List<Cell> cells =
                m_board.GetAllNonEmptyCells();

            if (cells.Count <= 0)
            {
                yield break;
            }

            Dictionary<int, List<Cell>> groups =
                new Dictionary<int, List<Cell>>();

            foreach (Cell cell in cells)
            {
                int id = cell.Item.ItemID;

                if (!groups.ContainsKey(id))
                {
                    groups[id] =
                        new List<Cell>();
                }

                groups[id].Add(cell);
            }

            List<Cell> target = null;

            foreach (var pair in groups)
            {
                if (pair.Value.Count >= 3)
                {
                    target =
                        pair.Value
                        .Take(3)
                        .ToList();

                    break;
                }
            }

            if (target == null)
            {
                yield break;
            }

            foreach (Cell cell in target)
            {
                if (cell != null &&
                    !cell.IsEmpty)
                {
                    SelectCell(cell);

                    yield return
                        new WaitForSeconds(0.5f);
                }
            }

            yield return
                new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator AutoPlayLoseCoroutine()
    {
        while (!m_gameOver)
        {
            yield return new WaitForSeconds(0.5f);

            if (IsBusy)
            {
                continue;
            }

            List<Cell> cells =
                m_board.GetAllNonEmptyCells();

            if (cells.Count <= 0)
            {
                yield break;
            }

            Cell chosen = null;

            foreach (Cell cell in cells)
            {
                int id = cell.Item.ItemID;

                int count =
                    m_tray.CountItem(id);

                if (count < 2)
                {
                    chosen = cell;
                    break;
                }
            }

            if (chosen == null)
            {
                chosen =
                    cells[UnityEngine.Random.Range(
                        0,
                        cells.Count)];
            }

            SelectCell(chosen);
        }
    }

}