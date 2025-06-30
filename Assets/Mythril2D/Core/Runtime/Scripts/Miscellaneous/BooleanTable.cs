using System;
using UnityEngine;

namespace Gyvr.Mythril2D
{
    [Serializable]
    public struct BooleanEntry
    {
        public string id;
        public bool value;
    }

    [Serializable]
    public struct BooleanTable
    {
        public BooleanEntry[] data;

        public bool Get(string id)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i].id == id)
                {
                    return data[i].value;
                }
            }

            Debug.LogErrorFormat("[GET] Boolean {0} not found in table", id);

            return false;
        }

        public void Set(string id, bool value)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i].id == id)
                {
                    data[i].value = value;
                    // GameManager.NotificationSystem.booleanChanged.Invoke(id, value);
                    return;
                }
            }

            Debug.LogErrorFormat("[SET] Boolean {0} not found in table", id);
        }

        public bool Has(string id)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i].id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
