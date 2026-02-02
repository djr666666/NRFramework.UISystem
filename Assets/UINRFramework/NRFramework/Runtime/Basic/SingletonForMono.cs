
using UnityEngine;



namespace NRFramework
{
    public class SingletonForMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T mInstance;

        private static object mLock = new object();

        public static T Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                    {
                        mInstance = FindObjectOfType(typeof(T)) as T;

                        if (mInstance == null)
                        {
                            GameObject singleton = new GameObject();
                            mInstance = singleton.AddComponent<T>();
                            singleton.name = string.Format("{0}(SingletonForMono)", typeof(T).ToString());
                            //不能editor 跟 编辑器语法用
                            DontDestroyOnLoad(singleton);
                        }
                        else
                        {
                            Debug.Log("[SingletonForMono] Using instance already created: " + mInstance.gameObject.name);
                        }
                    }

                    return mInstance;
                }
            }
        }

        public void DeleteInstance()
        {
            GameObject.DestroyImmediate(SingletonForMono<T>.mInstance);
            SingletonForMono<T>.mInstance = default(T);
        }

        public virtual void OnDestory()
        {
            if (mInstance == this)
            {
                mInstance = null;
            }
        }

        public virtual void Startup() { }
        public virtual void Init() { }

        public GameObject GetInstantiate(string name)
        {
            GameObject go = Resources.Load<GameObject>(name);
            return GameObject.Instantiate<GameObject>(go);
        }
        //Instantiate(block, new Vector3(x, y,0), Quaternion.identity);

        public void DestroyAll(GameObject go)
        {
            Destroy(go);
        }
    }


}


