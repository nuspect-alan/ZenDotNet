using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZenObjectDetection.DataStructures
{
    public class ImageNetPrediction
    {
        [ColumnName("grid")]
        public float[] PredictedLabels;
    }
}
