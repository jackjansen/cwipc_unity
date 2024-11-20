using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Cwipc
{
    /// <summary>
    /// Play a sequence of prerecorded pointclouds (think: volumetric video)
    /// </summary>
    public class PointCloudPlayback_VFX : MonoBehaviour
    {
        [Tooltip("Point cloud reader prefab")]
        public AbstractPointCloudSource reader_prefab;
        [Tooltip("Point cloud renderer prefab")]
        public PointCloudRenderer_VFX renderer_prefab;
        [Tooltip("If true start playback on Start")]
        public bool playOnStart = false;
        [Tooltip("Number of times point cloud stream is looped (zero: forever)")]
        public int loopCount = 0;
        [Tooltip("Directory with point cloud files")]
        public string dirName = "";
        [Tooltip("Invoked when playback starts")]
        public UnityEvent started;
        [Tooltip("Invoked when playback finishes")]
        public UnityEvent finished;
        [Tooltip("(introspection) point cloud reader")]
        public AbstractPointCloudSource cur_reader;
        [Tooltip("(introspection) point cloud renderer")]
        public PointCloudRenderer_VFX cur_renderer;

        public string Name()
        {
            return $"{GetType().Name}";
        }


        // Start is called before the first frame update
        void Start()
        {
            if (playOnStart)
            {
                Play(dirName);
            }
        }

        public void Play(string _dirName)
        {
            if (cur_reader != null || cur_renderer != null)
            {
                Debug.LogError($"{Name()}: Play() called while playing");
                return;
            }
            cur_reader = Instantiate(reader_prefab, transform);
            cur_renderer = Instantiate(renderer_prefab, transform);
            cur_renderer.pointcloudSource = cur_reader;
            Debug.Log($"{Name()}: Play({dirName})");
            dirName = _dirName;
            StartCoroutine(startPlay());
        }

        private IEnumerator startPlay()
        {
            yield return null;
            PrerecordedPointCloudReader rdr = cur_reader as PrerecordedPointCloudReader;
            if (rdr != null) {
                rdr.dirName = dirName;
                rdr.loopCount = loopCount;
            }
            cur_reader.gameObject.SetActive(true);
            cur_renderer.gameObject.SetActive(true);
        }

        private void preloadThread()
        {
            string[] filenames = System.IO.Directory.GetFileSystemEntries(dirName);
            foreach(var filename in filenames)
            {
                byte[] dummy = System.IO.File.ReadAllBytes(filename);
            }
        }

        private IEnumerator stopPlay()
        {
            yield return null;
            cur_reader.Stop();
            finished.Invoke(); // xxxjack or should this be done after the fade out?
            Destroy(cur_reader.gameObject);
            Destroy(cur_renderer.gameObject);
            cur_reader = null;
            cur_renderer = null;
        }

        public void RendererStarted()
        {
            Debug.Log($"{Name()}: Renderer started");
            started.Invoke();
        }

        public void RendererFinished()
        {
            Debug.Log($"{Name()}: Renderer finished");
            StartCoroutine(stopPlay());
        }

        public void Stop()
        {
            if (cur_reader != null || cur_renderer != null)
            {
                Debug.Log($"{Name()}: Stop");
                StartCoroutine(stopPlay());
            }
        }
    }
}
