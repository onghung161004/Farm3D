using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageResidentNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private string residentName = "Villager";
        [SerializeField] private string greeting = "Hello! It is nice to see you.";
        [SerializeField] private string firstTopic = "How are you?";
        [SerializeField] private string[] firstReplies = { "I am doing well, thank you!", "It is a lovely day in the village." };
        [SerializeField] private string secondTopic = "Tell me about the village";
        [SerializeField] private string[] secondReplies = { "The well is our favorite meeting place.", "Come by whenever you like." };

        public string ResidentName => residentName;
        public string Greeting => greeting;

        public void Configure(string name, string hello, string topicOne, string[] repliesOne,
            string topicTwo, string[] repliesTwo)
        {
            residentName = string.IsNullOrWhiteSpace(name) ? "Villager" : name;
            greeting = string.IsNullOrWhiteSpace(hello) ? "Hello!" : hello;
            firstTopic = topicOne;
            firstReplies = repliesOne == null ? new string[0] : (string[])repliesOne.Clone();
            secondTopic = topicTwo;
            secondReplies = repliesTwo == null ? new string[0] : (string[])repliesTwo.Clone();
        }

        public string TopicLabel(int topic) => topic == 0 ? firstTopic : topic == 1 ? secondTopic : "";
        public int ReplyCount(int topic) => topic == 0 ? firstReplies.Length : topic == 1 ? secondReplies.Length : 0;
        public string Reply(int topic, int index)
        {
            string[] replies = topic == 0 ? firstReplies : topic == 1 ? secondReplies : null;
            return replies != null && index >= 0 && index < replies.Length ? replies[index] : "";
        }

        public bool TryInteract(FarmTool tool) => VillageResidentDialogue.Show(this);
        public string GetInteractionPrompt(FarmTool tool) => "Talk to " + residentName + "  [E]";
    }
}
