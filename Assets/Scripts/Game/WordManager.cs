using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Impostor.Game
{
    /// <summary>
    /// Manages word selection, distribution, and word database.
    /// </summary>
    public class WordManager : MonoBehaviour
    {
        private static WordManager _instance;
        public static WordManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("WordManager");
                    _instance = go.AddComponent<WordManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private List<string> _wordDatabase = new List<string>();
        private HashSet<string> _usedWords = new HashSet<string>();
        private System.Random _random = new System.Random();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadWordDatabase();
        }

        private void LoadWordDatabase()
        {
            // Try to load from Resources first
            TextAsset wordFile = Resources.Load<TextAsset>("Words/wordlist");
            if (wordFile != null)
            {
                string[] words = wordFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                _wordDatabase.AddRange(words.Select(w => w.Trim()).Where(w => !string.IsNullOrEmpty(w)));
            }
            else
            {
                // Fallback to default words if file not found
                LoadDefaultWords();
            }

            Debug.Log($"Loaded {_wordDatabase.Count} words");
        }

        private void LoadDefaultWords()
        {
            _wordDatabase.AddRange(new[]
            {
                "Apple", "Banana", "Orange", "Grape", "Strawberry",
                "Dog", "Cat", "Bird", "Fish", "Rabbit",
                "Car", "Bicycle", "Airplane", "Train", "Boat",
                "House", "Tree", "Mountain", "Ocean", "River",
                "Book", "Computer", "Phone", "Camera", "Guitar",
                "Pizza", "Hamburger", "Ice Cream", "Cake", "Cookie",
                "Sun", "Moon", "Star", "Cloud", "Rainbow",
                "Doctor", "Teacher", "Chef", "Artist", "Musician",
                "Football", "Basketball", "Tennis", "Swimming", "Running"
            });
        }

        public string GetRandomWord()
        {
            if (_wordDatabase.Count == 0)
            {
                LoadDefaultWords();
            }

            // If we've used all words, reset
            if (_usedWords.Count >= _wordDatabase.Count)
            {
                _usedWords.Clear();
            }

            List<string> availableWords = _wordDatabase.Where(w => !_usedWords.Contains(w)).ToList();
            
            if (availableWords.Count == 0)
            {
                _usedWords.Clear();
                availableWords = new List<string>(_wordDatabase);
            }

            int index = _random.Next(availableWords.Count);
            string word = availableWords[index];
            _usedWords.Add(word);

            return word;
        }

        public void MarkWordAsUsed(string word)
        {
            _usedWords.Add(word);
        }

        public void ResetUsedWords()
        {
            _usedWords.Clear();
        }

        public void AddWord(string word)
        {
            if (!string.IsNullOrEmpty(word) && !_wordDatabase.Contains(word))
            {
                _wordDatabase.Add(word);
            }
        }

        public void AddWords(IEnumerable<string> words)
        {
            foreach (string word in words)
            {
                AddWord(word);
            }
        }
    }
}

