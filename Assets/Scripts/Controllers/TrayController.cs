using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrayController : MonoBehaviour
{
    private const float SLOT_SPACING = 1.2f;
    private const float SLOT_TAP_RADIUS = 0.55f;

    private class TrayEntry
    {
        public Item Item;
        public Cell OriginCell;
    }

    public event Action OnTrayOverflow =
        delegate { };

    [SerializeField]
    private int m_maxSlots = 5;

    private List<TrayEntry> m_entries =
        new List<TrayEntry>();

    private Transform m_trayRoot;

    private bool m_ignoreOverflow;

    public void Setup(
        Transform trayRoot,
        bool ignoreOverflow = false)
    {
        m_trayRoot = trayRoot;
        m_ignoreOverflow = ignoreOverflow;
    }

    public void AddItem(
        Item item,
        Cell originCell,
        Action callback)
    {
        TrayEntry entry = new TrayEntry
        {
            Item = item,
            OriginCell = originCell
        };

        m_entries.Add(entry);

        MoveItemToTray(entry, () =>
        {
            CheckMatches(); 

            CheckOverflow();

            callback?.Invoke();
        });
    }

    private void MoveItemToTray(
        TrayEntry entry,
        TweenCallback callback)
    {
        int index = m_entries.Count - 1;

        entry.Item.View.DOMove(
            GetSlotPosition(index),
            0.25f)
            .SetEase(Ease.OutBack)
            .OnComplete(callback);

        entry.Item.View.DOScale(0.9f, 0.25f);
    }

    private Vector3 GetSlotPosition(int index)
    {
        return m_trayRoot.position +
               Vector3.right * index * SLOT_SPACING;
    }

    private void CheckMatches()
    {
        var groups =
            m_entries.GroupBy(x => x.Item.ItemID);

        foreach (var group in groups)
        {
            if (group.Count() >= 3)
            {
                StartCoroutine(
                    ClearMatch(
                        group.Take(3).ToList()));

                return;
            }
        }
    }

    private IEnumerator ClearMatch(
        List<TrayEntry> matched)
    {
        yield return new WaitForSeconds(0.1f);

        foreach (TrayEntry entry in matched)
        {
            m_entries.Remove(entry);

            entry.Item.ExplodeView();
        }

        yield return new WaitForSeconds(0.2f);

        RearrangeTray();
    }

    private void RearrangeTray()
    {
        for (int i = 0; i < m_entries.Count; i++)
        {
            m_entries[i]
                .Item
                .View
                .DOMove(
                    GetSlotPosition(i),
                    0.2f);
        }
    }

    private void CheckOverflow()
    {
        if (!m_ignoreOverflow &&
            m_entries.Count > m_maxSlots)
        {
            OnTrayOverflow();
        }
    }

    public int CountItem(int itemID)
    {
        int count = 0;

        foreach (TrayEntry entry in m_entries)
        {
            if (entry.Item.ItemID == itemID)
            {
                count++;
            }
        }

        return count;
    }

    public bool TryReturnItemAtPosition(
        Vector2 worldPosition,
        TweenCallback callback)
    {
        if (m_entries.Count <= 0)
        {
            return false;
        }

        int index =
            Mathf.RoundToInt(
                (worldPosition.x - m_trayRoot.position.x) /
                SLOT_SPACING);

        if (index < 0 || index >= m_entries.Count)
        {
            return false;
        }

        Vector2 slotPos = GetSlotPosition(index);
        if (Vector2.Distance(slotPos, worldPosition) >
            SLOT_TAP_RADIUS)
        {
            return false;
        }

        TrayEntry entry = m_entries[index];
        if (entry.Item == null ||
            entry.Item.View == null ||
            entry.OriginCell == null ||
            !entry.OriginCell.IsEmpty)
        {
            return false;
        }

        m_entries.RemoveAt(index);

        entry.Item.View
            .DOMove(entry.OriginCell.transform.position, 0.2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                entry.OriginCell.Assign(entry.Item);
                entry.Item.View.DOScale(1f, 0.2f);
                RearrangeTray();
                callback?.Invoke();
            });

        return true;
    }
}