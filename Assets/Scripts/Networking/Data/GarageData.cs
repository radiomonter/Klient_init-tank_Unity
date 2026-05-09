using System;
using System.Collections.Generic;

namespace Tanki.Networking.Data
{
    [Serializable]
    public class GarageItemData
    {
        public string id;
        public string name;
        public string description;
        public int rank;
        public int index;
        public int price;
        public string category;
        public string type;
        public string previewResourceId;
        public string modificationID;
        public string baseItemId;
        public int count;
        public PropertyData[] properts;
        public string object3ds;
        public string coloring;
        public string animatedColoring;
        public int remainingTimeInSec;
    }

    [Serializable]
    public class PropertyData
    {
        public string property;
        public float value;
        public SubPropertyData[] subproperties;
    }

    [Serializable]
    public class SubPropertyData
    {
        public string property;
        public float value;
    }

    [Serializable]
    public class GarageResponse
    {
        public GarageItemData[] items;
    }
}
