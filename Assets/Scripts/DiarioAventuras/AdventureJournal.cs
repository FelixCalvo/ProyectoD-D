using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class AdventureJournal : MonoBehaviour
{
    public static AdventureJournal Instance { get; private set; }

    public List<JournalEntry> entries = new List<JournalEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddEntry(string speaker, string text)
    {
        entries.Add(new JournalEntry
        {
            speaker = speaker,
            text = text,
            scene = SceneManager.GetActiveScene().name
        });
    }
}
