using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "Farm Restoration/Quest Database")]
    public sealed class QuestDatabase : ScriptableObject
    {
        [SerializeField] private List<QuestChapterData> chapters = new List<QuestChapterData>();

        public IReadOnlyList<QuestChapterData> Chapters => chapters;

        public void Configure(IEnumerable<QuestChapterData> chapterList)
        {
            chapters = new List<QuestChapterData>(chapterList);
        }
    }
}
