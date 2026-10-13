using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class TreeFallAudio : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip ownedClip;

        public static TreeFallAudio Ensure(ChoppableTree tree)
        {
            if (tree == null) return null;
            TreeFallAudio audio = tree.GetComponent<TreeFallAudio>();
            if (audio != null) return audio;
            audio = tree.gameObject.AddComponent<TreeFallAudio>();
            audio.source = tree.gameObject.AddComponent<AudioSource>();
            audio.source.playOnAwake = false;
            audio.source.spatialBlend = 1f;
            audio.source.rolloffMode = AudioRolloffMode.Linear;
            audio.source.minDistance = 3f;
            audio.source.maxDistance = 28f;
            FarmAudioChannel.Bind(audio.source, FarmAudioGroup.Effects, 0.75f);
            audio.source.priority = 70;
            audio.source.clip = Resources.Load<AudioClip>("FarmAudio/tree_fall");
            if (audio.source.clip == null) audio.source.clip = audio.ownedClip = FarmSoundGenerator.CreateTreeFall();
            else audio.source.pitch = 0.75f; // Slower, heavier trunk fall; recorded impact aligns with 1.65 s animation.
            return audio;
        }

        public static void PlayFor(ChoppableTree tree)
        {
            TreeFallAudio audio = Ensure(tree);
            if (audio != null) audio.source.Play();
        }

        private void OnDestroy()
        {
            if (ownedClip == null) return;
            if (Application.isPlaying) Destroy(ownedClip);
            else DestroyImmediate(ownedClip);
        }
    }
}
