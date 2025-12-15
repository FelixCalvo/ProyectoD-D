
using TMPro;
using UnityEngine;
using System.Text;
using UnityEngine.UI;
using System.Collections;

public class JournalUI : MonoBehaviour
{
    public TextMeshProUGUI journalText;
    [SerializeField] private RectTransform contentRectTransform; // Arrastra aquí el "Content" del ScrollView

    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string closeTrigger = "Close";

    [SerializeField] private bool activarPanel = true;

    public void Refresh()
    {
        if (journalText == null || AdventureJournal.Instance == null) return;

        var sb = new StringBuilder();
        foreach (var e in AdventureJournal.Instance.entries)
        {
            sb.AppendLine($"<b>{e.speaker}</b>");
            sb.AppendLine(e.text);
            sb.AppendLine();
        }

        journalText.text = sb.ToString();
        
        // Forzar actualización del TextMeshPro y el layout
        StartCoroutine(RefreshLayoutNextFrame());
    }

    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null; // Esperar un frame para que TMP calcule el texto
        
        if (journalText != null)
        {
            journalText.ForceMeshUpdate();
            
            // Ajustar manualmente la altura del Content basándose en el texto
            if (contentRectTransform != null)
            {
                float textHeight = journalText.preferredHeight;
                contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, textHeight);
            }
        }
        
        if (contentRectTransform != null)
        {
            Canvas.ForceUpdateCanvases();
        }
    }

    public void OpenJournal()
    {
        if (activarPanel)
        {
            gameObject.SetActive(true);
            animator.ResetTrigger(closeTrigger);
            animator.SetTrigger(openTrigger);
            activarPanel = false;
        }
        else
        {
            CloseJournal();
            activarPanel = true;
        }
        
        // Siempre refrescar cuando se abre el panel
        Refresh();
    }

    public void CloseJournal()
    {
        animator.ResetTrigger(openTrigger);
        animator.SetTrigger(closeTrigger);
    }

    // Vincula este método al final del clip "Close" como Animation Event
    public void OnCloseAnimationComplete()
       {
        gameObject.SetActive(false);
    }
}