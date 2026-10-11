using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    [CreateAssetMenu(fileName = "NewChapter", menuName = "Farm Restoration/Quest Chapter")]
    public sealed class QuestChapterData : ScriptableObject
    {
        [SerializeField] private string chapterId = "chapter_01";
        [SerializeField] private string chapterTitle = "Chapter 1";
        [SerializeField] private List<QuestData> quests = new List<QuestData>();

        public string ChapterId => chapterId;
        public string ChapterTitle => chapterTitle;
        public IReadOnlyList<QuestData> Quests => quests;

        public void Configure(string id, string title, IEnumerable<QuestData> chapterQuests)
        {
            chapterId = id;
            chapterTitle = title;
            quests = new List<QuestData>(chapterQuests);
        }
    }
}
