using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class PointCloudOutput : MonoBehaviour
    {
        QueueThreadSafe ReaderToPreparerQueue;
        public AsyncPointCloudReader PCreader;
        public AsyncPointCloudPreparer PCpreparer;
        public PointCloudRenderer PCrenderer;

        public float Preparer_DefaultCellSize = 1.0f;
        public float Preparer_CellSizeFactor = 1.0f;
        public float Synthetic_Framerate = 15;
        public int Synthetic_NPoints = 8000;
        // Start is called before the first frame update
        void Start()
        {
            InitializePipeline(); 
        }

        void InitializePipeline()
        {
            ReaderToPreparerQueue = new QueueThreadSafe("ReaderToPreparer", 2, true);
            PCreader = new AsyncSyntheticReader(Synthetic_Framerate, Synthetic_NPoints, ReaderToPreparerQueue);
            PCpreparer = new AsyncPointCloudPreparer(ReaderToPreparerQueue, Preparer_DefaultCellSize, Preparer_CellSizeFactor);
            PCrenderer.SetPreparer(PCpreparer);
        }

        private void OnDestroy()
        {
            PCreader.StopAndWait();
            PCpreparer.StopAndWait();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
