using System;
using UnityEngine;
using System.Collections.Generic;

namespace CGame
{
    public class GameManager : MonoSingleton<GameManager>
    {
        private static readonly LinkedList<IManager> s_managerList = new LinkedList<IManager>();
  

        public static IManager CreateManager(Type managerType)
        {
            // check Manager exsist
            foreach (IManager m in s_managerList)
            {
                if (m.GetType() == managerType)
                {
                    return m;
                }
            }
            
            IManager manager = (IManager)Activator.CreateInstance(managerType);
#if DEVELOPMENT_BUILD
            if (manager == null)
            {
                throw new Exception($"Can't Create {managerType}");
            }
#endif
            LinkedListNode<IManager> current = s_managerList.First;
            while (current != null)
            {
                if (manager.Priority > current.Value.Priority)
                {
                    break;
                }

                current = current.Next;
            }

            if (current != null)
            {
                s_managerList.AddBefore(current, manager);
            }
            else
            {
                s_managerList.AddLast(manager);
            }
            manager.Init();
            return manager;
        }

        public static T GetManager<T>() where T : class
        {
            Type interfaceType = typeof(T);
            return GetManager(interfaceType) as T;
        }

        public static void DestoryManager(Type managerType)
        {
            foreach (IManager manager in s_managerList)
            {
                if (manager.GetType() == managerType)
                {
                    s_managerList.Remove(manager);
                    manager.Shutdown();
                    return;
                }
            }
        }


        private static IManager GetManager(Type managerType)
        {
            foreach (IManager manager in s_managerList)
            {
                if (manager.GetType() == managerType)
                {
                    return manager;
                }
            }

            return CreateManager(managerType);
        }
        
        void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            foreach (var manager in s_managerList)
            {
                manager.FixedUpdate(deltaTime);
            }
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var manager in s_managerList)
            {
                manager.Update(deltaTime);
            }
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            foreach (var manager in s_managerList)
            {
                manager.LateUpdate(deltaTime);
            }
        }

        public void OnDestroy()
        {
            LinkedListNode<IManager> currentNode = s_managerList.Last; 
            while (currentNode != null)
            {
                currentNode.Value.Shutdown();
                currentNode = currentNode.Previous;       // 移动到前一个节点
            }
        }
    }
}
